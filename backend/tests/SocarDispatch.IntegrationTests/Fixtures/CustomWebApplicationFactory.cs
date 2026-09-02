using System.Data.Common;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Npgsql;
using Respawn;
using Respawn.Graph;
using SocarDispatch.Application.Common.Interfaces;
using SocarDispatch.Infrastructure.Persistence;
using Testcontainers.PostgreSql;

namespace SocarDispatch.IntegrationTests.Fixtures;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _dbContainer = new PostgreSqlBuilder()
        .WithImage("postgis/postgis:16-3.4")
        .WithDatabase("socar_dispatch_test")
        .WithUsername("postgres")
        .WithPassword("postgrespassword")
        .Build();

    private DbConnection _dbConnection = null!;
    private Respawner _respawner = null!;
    private string _connectionString = string.Empty;

    public string ConnectionString => _connectionString;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((context, config) =>
        {
            var overrides = new Dictionary<string, string?>
            {
                { "DB_CONNECTION_STRING", _connectionString },
                { "ConnectionStrings:DefaultConnection", _connectionString },
                { "JWT_SECRET_KEY", "SOCAR_Super_Secret_Key_For_Emergency_Dispatch_System_2026" },
                { "JWT_ISSUER", "socar-dispatch-api" },
                { "JWT_AUDIENCE", "socar-dispatch-clients" }
            };
            config.AddInMemoryCollection(overrides);
        });

        builder.ConfigureTestServices(services =>
        {
            // Replace DbContext with PostgreSQL test connection
            services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseNpgsql(_connectionString, o => o.UseNetTopologySuite()));

            // Replace external storage and push notification services with test stubs
            services.RemoveAll<IStorageInitializer>();
            services.AddScoped<IStorageInitializer, NoOpStorageInitializer>();

            services.RemoveAll<IPushNotificationService>();
            services.AddSingleton<IPushNotificationService, NoOpPushNotificationService>();
        });
    }

    public async Task InitializeAsync()
    {
        // Start PostGIS PostgreSQL container
        await _dbContainer.StartAsync();
        _connectionString = _dbContainer.GetConnectionString();

        // Ensure database extensions and schema migrations are applied
        using var scope = Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await context.Database.OpenConnectionAsync();
        await context.Database.ExecuteSqlRawAsync("CREATE EXTENSION IF NOT EXISTS postgis;");
        await context.Database.ExecuteSqlRawAsync("CREATE EXTENSION IF NOT EXISTS \"uuid-ossp\";");
        await context.Database.MigrateAsync();

        // Initialize Respawn for fast checkpoint-based database rollback
        _dbConnection = new NpgsqlConnection(_connectionString);
        await _dbConnection.OpenAsync();

        _respawner = await Respawner.CreateAsync(_dbConnection, new RespawnerOptions
        {
            DbAdapter = DbAdapter.Postgres,
            SchemasToInclude = new[] { "public" },
            TablesToIgnore = new Table[]
            {
                "__EFMigrationsHistory",
                "spatial_ref_sys"
            }
        });
    }

    public async Task ResetDatabaseAsync()
    {
        if (_dbConnection.State != System.Data.ConnectionState.Open)
        {
            await _dbConnection.OpenAsync();
        }

        await _respawner.ResetAsync(_dbConnection);
    }

    public new async Task DisposeAsync()
    {
        if (_dbConnection != null)
        {
            await _dbConnection.DisposeAsync();
        }

        await _dbContainer.DisposeAsync();
        await base.DisposeAsync();
    }

    private class NoOpStorageInitializer : IStorageInitializer
    {
        public Task InitializeStorageAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private class NoOpPushNotificationService : IPushNotificationService
    {
        public Task SendAsync(string deviceToken, string title, string body, Dictionary<string, string>? data = null, CancellationToken ct = default) => Task.CompletedTask;
        public Task SendMulticastAsync(IEnumerable<string> deviceTokens, string title, string body, Dictionary<string, string>? data = null, CancellationToken ct = default) => Task.CompletedTask;
    }

}
