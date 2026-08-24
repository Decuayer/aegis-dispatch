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

    /// <summary>
    /// Kullanıcı e-posta ve şifre ile giriş talebini işler.
    /// Yalnızca 'Operator' rolüne izin verir.
    /// </summary>
    public async Task<ApiResponse<AuthResponseDto>> LoginAsync(LoginModel model)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/v1/auth/login", model);
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<AuthResponseDto>>();

            if (result == null || !result.Success || result.Data == null)
            {
                return ApiResponse<AuthResponseDto>.FailureResult(
                    result?.Message ?? "Giriş başarısız. Lütfen bilgilerinizi kontrol ediniz.",
                    result?.Errors);
            }

            // Operatör Rolü Güvenlik Kontrolü
            var userRole = result.Data.User.RoleType;
            if (userRole != RoleType.Operator)
            {
                return ApiResponse<AuthResponseDto>.FailureResult(
                    "Unauthorized role: Only dispatch operators can access this panel.");
            }

            // Operatör rolü onaylandı: Token'ı LocalStorage'a kaydet
            await _localStorage.SetItemAsync(AuthTokenKey, result.Data.AccessToken);

            // AuthenticationStateProvider'ı bilgilendir (Phase 3'te tamamlanacak)
            if (_authStateProvider is CustomAuthStateProvider customAuthStateProvider)
            {
                customAuthStateProvider.NotifyUserAuthentication(result.Data.AccessToken);
            }

            return result;
        }
        catch (Exception ex)
        {
            return ApiResponse<AuthResponseDto>.FailureResult(
                $"Sunucu ile iletişim kurulurken bir hata oluştu: {ex.Message}");
        }
    }

    /// <summary>
    /// Google OAuth 2.0 idToken ile giriş yapma işlemi.
    /// Yalnızca 'Operator' rolüne izin verir.
    /// </summary>
    public async Task<ApiResponse<AuthResponseDto>> GoogleLoginAsync(string idToken)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/v1/auth/google-login", new { IdToken = idToken });
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<AuthResponseDto>>();

            if (result == null || !result.Success || result.Data == null)
            {
                return ApiResponse<AuthResponseDto>.FailureResult(
                    result?.Message ?? "Google ile giriş başarısız.");
            }

            // Operatör Rolü Güvenlik Kontrolü
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
            return ApiResponse<AuthResponseDto>.FailureResult($"Google giriş hatası: {ex.Message}");
        }
    }

    /// <summary>
    /// Kullanıcının oturumunu kapatır ve yerel token'ı temizler.
    /// </summary>
    public async Task LogoutAsync()
    {
        await _localStorage.RemoveItemAsync(AuthTokenKey);

        if (_authStateProvider is CustomAuthStateProvider customAuthStateProvider)
        {
            customAuthStateProvider.NotifyUserLogout();
        }
    }

    /// <summary>
    /// Kayıtlı token'ı getirir.
    /// </summary>
    public async Task<string?> GetTokenAsync()
    {
        return await _localStorage.GetItemAsync<string>(AuthTokenKey);
    }
}
