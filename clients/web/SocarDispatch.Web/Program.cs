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

// 1. Blazored LocalStorage Servisi
builder.Services.AddBlazoredLocalStorage();

// 2. HTTP Interceptor (AuthorizationHeaderHandler)
builder.Services.AddTransient<AuthorizationHeaderHandler>();

// 3. Backend API için HttpClient Yapılandırması (Interceptor ile birlikte)
var backendApiUrl = builder.Configuration["ApiBaseUrl"] ?? "http://localhost:5233";

builder.Services.AddHttpClient("SocarDispatchAPI", client =>
{
    client.BaseAddress = new Uri(backendApiUrl);
}).AddHttpMessageHandler<AuthorizationHeaderHandler>();

// varsayılan HttpClient olarak yapılandırılan client'ı enjekte et
builder.Services.AddScoped(sp => 
    sp.GetRequiredService<IHttpClientFactory>().CreateClient("SocarDispatchAPI"));

// 4. Custom AuthenticationStateProvider ve Auth Servis Kayıtları
builder.Services.AddScoped<CustomAuthStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(sp => sp.GetRequiredService<CustomAuthStateProvider>());
builder.Services.AddScoped<IAuthService, AuthService>();

// 5. Blazor Yetkilendirme Çekirdeği (Authorization Core)
builder.Services.AddAuthorizationCore();

// 6. Harita Servisi (Leaflet)
builder.Services.AddScoped<SocarDispatch.Web.Services.IMapService, SocarDispatch.Web.Services.MapService>();

// 7. SignalR Hub Client Servisleri
builder.Services.AddScoped<IIncidentHubClient, IncidentHubClient>();
builder.Services.AddScoped<ILocationHubClient, LocationHubClient>();

// Toast Servisi
builder.Services.AddScoped<IToastService, ToastService>();


await builder.Build().RunAsync();
