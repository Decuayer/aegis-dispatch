using System.Net.Http.Json;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Authorization;
using SocarDispatch.Web.Models.Auth;
using SocarDispatch.Web.Models.Common;

namespace SocarDispatch.Web.Auth;

public class AuthService : IAuthService
{
    private readonly HttpClient _httpClient;
    private readonly ILocalStorageService _localStorage;
    private readonly AuthenticationStateProvider _authStateProvider;
    private const string AuthTokenKey = "authToken";

    public AuthService(
        HttpClient httpClient,
        ILocalStorageService localStorage,
        AuthenticationStateProvider authStateProvider)
    {
        _httpClient = httpClient;
        _localStorage = localStorage;
        _authStateProvider = authStateProvider;
    }

    public async Task<ApiResponse<AuthResponseDto>> LoginAsync(LoginModel model)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/v1/auth/login", model);
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<AuthResponseDto>>();

            if (result == null || !result.Success || result.Data == null)
            {
                return ApiResponse<AuthResponseDto>.FailureResult(
                    result?.Message ?? "Login failed. Please verify your credentials.",
                    result?.Errors);
            }

            // Enforce Operator role requirement
            if (result.Data.User.RoleType != RoleType.Operator)
            {
                return ApiResponse<AuthResponseDto>.FailureResult(
                    "Unauthorized role: Only dispatch operators can access this panel.");
            }

            await _localStorage.SetItemAsync(AuthTokenKey, result.Data.AccessToken);

            if (_authStateProvider is CustomAuthStateProvider customAuthStateProvider)
            {
                customAuthStateProvider.NotifyUserAuthentication(result.Data.AccessToken);
            }

            return result;
        }
        catch (Exception ex)
        {
            return ApiResponse<AuthResponseDto>.FailureResult(
                $"An error occurred while communicating with the server: {ex.Message}");
        }
    }

    public async Task<ApiResponse<AuthResponseDto>> GoogleLoginAsync(string idToken)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/v1/auth/google-login", new { IdToken = idToken });
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<AuthResponseDto>>();

            if (result == null || !result.Success || result.Data == null)
            {
                return ApiResponse<AuthResponseDto>.FailureResult(
                    result?.Message ?? "Google login failed.");
            }

            if (result.Data.User.RoleType != RoleType.Operator)
            {
                return ApiResponse<AuthResponseDto>.FailureResult(
                    "Unauthorized role: Only dispatch operators can access this panel.");
            }

            await _localStorage.SetItemAsync(AuthTokenKey, result.Data.AccessToken);

            if (_authStateProvider is CustomAuthStateProvider customAuthStateProvider)
            {
                customAuthStateProvider.NotifyUserAuthentication(result.Data.AccessToken);
            }

            return result;
        }
        catch (Exception ex)
        {
            return ApiResponse<AuthResponseDto>.FailureResult($"Google sign-in error: {ex.Message}");
        }
    }

    public async Task LogoutAsync()
    {
        await _localStorage.RemoveItemAsync(AuthTokenKey);

        if (_authStateProvider is CustomAuthStateProvider customAuthStateProvider)
        {
            customAuthStateProvider.NotifyUserLogout();
        }
    }

    public async Task HandleSessionExpiredAsync(string? returnUrl = null)
    {
        await _localStorage.RemoveItemAsync(AuthTokenKey);

        if (_authStateProvider is CustomAuthStateProvider customAuthStateProvider)
        {
            customAuthStateProvider.NotifyUserLogout();
        }
    }

    public async Task<string?> GetTokenAsync()
    {
        return await _localStorage.GetItemAsync<string>(AuthTokenKey);
    }
}
