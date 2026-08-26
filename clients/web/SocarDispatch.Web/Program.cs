using System.Net.Http;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using SocarDispatch.Web;
using SocarDispatch.Web.Auth;
using SocarDispatch.Web.Handlers;
using SocarDispatch.Web.Services;
using SocarDispatch.Web.Services.SignalR;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// 1. Blazored LocalStorage Service
builder.Services.AddBlazoredLocalStorage();

// 2. Auth Event Bus Service
builder.Services.AddScoped<IAuthEventService, AuthEventService>();

// 3. HTTP Interceptor (AuthorizationHeaderHandler)
builder.Services.AddTransient<AuthorizationHeaderHandler>();

// 4. Backend API HttpClient Configuration with Interceptor
var backendApiUrl = builder.Configuration["ApiBaseUrl"] ?? "http://localhost:5233";

builder.Services.AddHttpClient("SocarDispatchAPI", client =>
{
    client.BaseAddress = new Uri(backendApiUrl);
}).AddHttpMessageHandler<AuthorizationHeaderHandler>();

// Default HttpClient injection
builder.Services.AddScoped(sp => 
    sp.GetRequiredService<IHttpClientFactory>().CreateClient("SocarDispatchAPI"));

// 5. Custom AuthenticationStateProvider and Auth Services
builder.Services.AddScoped<CustomAuthStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(sp => sp.GetRequiredService<CustomAuthStateProvider>());
builder.Services.AddScoped<IAuthService, AuthService>();

// 6. Blazor Authorization Core
builder.Services.AddAuthorizationCore();

// 7. Map Service (Leaflet)
builder.Services.AddScoped<IMapService, MapService>();

// 8. SignalR Hub Client Services
builder.Services.AddScoped<IIncidentHubClient, IncidentHubClient>();
builder.Services.AddScoped<ILocationHubClient, LocationHubClient>();

// 9. Toast Notification Service
builder.Services.AddScoped<IToastService, ToastService>();

// 10. Incident & Dispatch Domain Services
builder.Services.AddScoped<IIncidentService, IncidentService>();
builder.Services.AddScoped<IAssignmentService, AssignmentService>();
builder.Services.AddScoped<ITeamService, TeamService>();
builder.Services.AddScoped<IUserService, UserService>();

// 11. Settings & Configuration Services
builder.Services.AddScoped<ISettingsService, SettingsService>();
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<IEmergencyCodeService, EmergencyCodeService>();

await builder.Build().RunAsync();
