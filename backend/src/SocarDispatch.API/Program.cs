using System.Text;
using dotenv.net;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Microsoft.EntityFrameworkCore;
using SocarDispatch.API.Middlewares;
using SocarDispatch.Application;
using SocarDispatch.Infrastructure;
using SocarDispatch.Infrastructure.Hubs;
using Swashbuckle.AspNetCore.Filters;

// Installing .env
DotEnv.Load(options: new DotEnvOptions(probeForEnv: true, probeLevelsToSearch: 5));

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddEnvironmentVariables();

// Layer Services
builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);

// 1. JWT Authentication & Authorization
var jwtSecretKey = builder.Configuration["JWT_SECRET_KEY"]
    ?? builder.Configuration["JwtSettings:SecretKey"]
    ?? "SOCAR_Super_Secret_Key_For_Emergency_Dispatch_System_2026";

var jwtIssuer = builder.Configuration["JWT_ISSUER"]
    ?? builder.Configuration["JwtSettings:Issuer"]
    ?? "socar-dispatch-api";

var jwtAudience = builder.Configuration["JWT_AUDIENCE"]
    ?? builder.Configuration["JwtSettings:Audience"]
    ?? "socar-dispatch-clients";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecretKey)),
        ValidateIssuer = true,
        ValidIssuer = jwtIssuer,
        ValidateAudience = true,
        ValidAudience = jwtAudience,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };

    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = ctx =>
        {
            var token = ctx.Request.Query["access_token"];
            var path = ctx.HttpContext.Request.Path;
            if (!string.IsNullOrEmpty(token) &&
                (path.StartsWithSegments("/hubs/incidents") ||
                 path.StartsWithSegments("/hubs/location")))
            {
                ctx.Token = token;
            }
            return Task.CompletedTask;
        }
    };
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.SetIsOriginAllowed(_ => true)
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

builder.Services.AddAuthorization();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddHealthChecks();

// 2. Swagger & Bearer Auth Configuration (Filter-based)
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Aegis Dispatch API",
        Version = "v1",
        Description = "Real-Time Emergency Dispatch and Response System API"
    });

    options.AddSecurityDefinition("oauth2", new OpenApiSecurityScheme
    {
        Description = "Standard Authorization header: 'Bearer {token}'",
        In = ParameterLocation.Header,
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey
    });

    options.CustomSchemaIds(type => type.FullName ?? type.Name);
    options.OperationFilter<SecurityRequirementsOperationFilter>();
});

var app = builder.Build();

// Global Exception Middleware
app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Aegis Dispatch API v1");
    });
}

// app.UseHttpsRedirection();

app.UseCors("AllowAll");

// Sorting: Authentication -> Authorization
app.UseAuthentication();
app.UseAuthorization();


// Database Migration, PostGIS Extension Checks & Object Storage Bucket Provisioning Lifecycle
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();
    var context = services.GetRequiredService<SocarDispatch.Infrastructure.Persistence.ApplicationDbContext>();

    try
    {
        // 1. PostGIS and UUID extension verification
        logger.LogInformation("Verifying PostgreSQL extensions (postgis, uuid-ossp)...");
        await context.Database.ExecuteSqlRawAsync("CREATE EXTENSION IF NOT EXISTS postgis;");
        await context.Database.ExecuteSqlRawAsync("CREATE EXTENSION IF NOT EXISTS \"uuid-ossp\";");
        logger.LogInformation("PostgreSQL extensions verified successfully.");

        // 2. Automated EF Core Database Migrations
        logger.LogInformation("Applying EF Core migrations...");
        await context.Database.MigrateAsync();
        logger.LogInformation("EF Core migrations applied successfully.");
    }
    catch (Exception ex)
    {
        logger.LogCritical(ex, "Database migration/extension initialization failed. Halting application startup.");
        throw;
    }

    // 3. Automated MinIO Bucket & Storage Policy Provisioning
    try
    {
        var storageInitializer = services.GetRequiredService<SocarDispatch.Application.Common.Interfaces.IStorageInitializer>();
        logger.LogInformation("Initializing object storage buckets...");
        await storageInitializer.InitializeStorageAsync();
        logger.LogInformation("Object storage initialized successfully.");
    }
    catch (Exception ex)
    {
        logger.LogWarning(ex, "Object storage initialization encountered an issue. Startup will continue.");
    }

    // 4. Default Operator Seed Data (ensure operator@aegisdispatch.internal exists on startup)
    const string defaultOperatorEmail = "operator@aegisdispatch.internal";
    var existingOperator = await context.Users.FirstOrDefaultAsync(u => u.Email == defaultOperatorEmail || u.Email == "operator@socar.az");
    var passwordHasher = services.GetRequiredService<SocarDispatch.Application.Common.Interfaces.IPasswordHasher>();

    if (existingOperator == null)
    {
        var defaultOperator = new SocarDispatch.Domain.Entities.User
        {
            Id = Guid.NewGuid(),
            FirstName = "Aegis",
            LastName = "Operator",
            Email = defaultOperatorEmail,
            Phone = "+15551234567",
            PasswordHash = passwordHasher.HashPassword("Operator123!"),
            Department = "Emergency Operations",
            RoleType = SocarDispatch.Domain.Enums.RoleType.Operator,
            SubRole = "Head Dispatcher",
            CreatedAt = DateTime.UtcNow
        };

        context.Users.Add(defaultOperator);
        await context.SaveChangesAsync();
        logger.LogInformation("Default operator account successfully created ({Email}).", defaultOperator.Email);
    }
    else
    {
        if (existingOperator.Email == "operator@socar.az")
        {
            existingOperator.Email = defaultOperatorEmail;
            existingOperator.FirstName = "Aegis";
        }
        if (existingOperator.RoleType != SocarDispatch.Domain.Enums.RoleType.Operator)
        {
            existingOperator.RoleType = SocarDispatch.Domain.Enums.RoleType.Operator;
        }
        await context.SaveChangesAsync();
        logger.LogInformation("Updated operator account ({Email}).", existingOperator.Email);
    }
}


app.MapHub<IncidentsHub>("/hubs/incidents");
app.MapHub<LocationHub>("/hubs/location");

app.MapControllers();
app.MapHealthChecks("/healthz");

app.Run();

// Expose Program class for WebApplicationFactory integration tests
public partial class Program { }
