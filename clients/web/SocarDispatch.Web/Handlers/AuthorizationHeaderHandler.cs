using System.Net;
using System.Net.Http.Headers;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components;
using SocarDispatch.Web.Auth;
using SocarDispatch.Web.Services;

namespace SocarDispatch.Web.Handlers;

public class AuthorizationHeaderHandler : DelegatingHandler
{
    private readonly ILocalStorageService _localStorage;
    private readonly NavigationManager _navigationManager;
    private readonly IToastService _toastService;
    private readonly CustomAuthStateProvider _authStateProvider;
    private const string AuthTokenKey = "authToken";
    private static bool _isHandlingExpiredSession = false;

    public AuthorizationHeaderHandler(
        ILocalStorageService localStorage,
        NavigationManager navigationManager,
        IToastService toastService,
        CustomAuthStateProvider authStateProvider)
    {
        _localStorage = localStorage;
        _navigationManager = navigationManager;
        _toastService = toastService;
        _authStateProvider = authStateProvider;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, 
        CancellationToken cancellationToken)
    {
        try
        {
            // 1. Read token from LocalStorage and append Bearer header
            var token = await _localStorage.GetItemAsync<string>(AuthTokenKey);
            if (!string.IsNullOrWhiteSpace(token))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }
        }
        catch
        {
            // Ignore if JS Interop is not ready during initial boot
        }

        // 2. Execute HTTP request
        var response = await base.SendAsync(request, cancellationToken);

        // 3. Catch 401 Unauthorized (exclude Login / Register endpoints)
        var requestPath = request.RequestUri?.AbsolutePath ?? string.Empty;
        var isAuthEndpoint = requestPath.Contains("/api/v1/auth/login", StringComparison.OrdinalIgnoreCase) ||
                             requestPath.Contains("/api/v1/auth/register", StringComparison.OrdinalIgnoreCase) ||
                             requestPath.Contains("/api/v1/auth/google-login", StringComparison.OrdinalIgnoreCase);

        if (response.StatusCode == HttpStatusCode.Unauthorized && !isAuthEndpoint)
        {
            if (!_isHandlingExpiredSession)
            {
                _isHandlingExpiredSession = true;
                try
                {
                    // Purge expired token
                    await _localStorage.RemoveItemAsync(AuthTokenKey);

                    // Notify Blazor authentication state
                    _authStateProvider.NotifyUserLogout();

                    // Display session expired warning toast
                    _toastService.ShowWarning("Your session has expired. Please sign in again.", "Session Expired");

                    // Capture current path and redirect to login
                    var currentUri = _navigationManager.ToBaseRelativePath(_navigationManager.Uri);
                    var redirectUrl = string.IsNullOrWhiteSpace(currentUri) || currentUri.StartsWith("login", StringComparison.OrdinalIgnoreCase)
                        ? "login"
                        : $"login?returnUrl={Uri.EscapeDataString(currentUri)}";

                    _navigationManager.NavigateTo(redirectUrl, forceLoad: false);
                }
                finally
                {
                    // Reset lock after a short delay
                    _ = Task.Delay(2000).ContinueWith(_ => _isHandlingExpiredSession = false);
                }
            }
        }

        return response;
    }
}
