using System.Security.Claims;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Authorization;

namespace SocarDispatch.Web.Auth;

public class CustomAuthStateProvider : AuthenticationStateProvider
{
    private readonly ILocalStorageService _localStorage;
    private const string AuthTokenKey = "authToken";
    private readonly ClaimsPrincipal _anonymous = new(new ClaimsIdentity());

    public CustomAuthStateProvider(ILocalStorageService localStorage)
    {
        _localStorage = localStorage;
    }

    /// <summary>
    /// Blazor bileşenleri (AuthorizeView vb.) kullanıcı oturum durumunu sorguladığında çalışır.
    /// </summary>
    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        try
        {
            var token = await _localStorage.GetItemAsync<string>(AuthTokenKey);

            // Token yoksa veya süresi dolmuşsa anonim durum döndür
            if (string.IsNullOrWhiteSpace(token) || JwtTokenParser.IsTokenExpired(token))
            {
                return new AuthenticationState(_anonymous);
            }

            // Token'dan claim'leri çöz
            var claims = JwtTokenParser.ParseClaimsFromJwt(token);

            // Identity ve Principal oluştur (Name claim ve Role claim eşleşmeleri ile)
            var identity = new ClaimsIdentity(claims, "jwt", ClaimTypes.Name, ClaimTypes.Role);
            var user = new ClaimsPrincipal(identity);

            return new AuthenticationState(user);
        }
        catch
        {
            // JS Interop veya LocalStorage okuma hatasında güvenli bir şekilde anonim döndür
            return new AuthenticationState(_anonymous);
        }
    }

    /// <summary>
    /// Başarılı giriş sonrasında çağrılarak Blazor cascading state'ini günceller.
    /// </summary>
    public void NotifyUserAuthentication(string token)
    {
        var claims = JwtTokenParser.ParseClaimsFromJwt(token);
        var identity = new ClaimsIdentity(claims, "jwt", ClaimTypes.Name, ClaimTypes.Role);
        var user = new ClaimsPrincipal(identity);

        var authState = Task.FromResult(new AuthenticationState(user));
        NotifyAuthenticationStateChanged(authState);
    }

    /// <summary>
    /// Çıkış yapıldığında çağrılarak Blazor cascading state'ini anonim duruma getirir.
    /// </summary>
    public void NotifyUserLogout()
    {
        var authState = Task.FromResult(new AuthenticationState(_anonymous));
        NotifyAuthenticationStateChanged(authState);
    }
}
