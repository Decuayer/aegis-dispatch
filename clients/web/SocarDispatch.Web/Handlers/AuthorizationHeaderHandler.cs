using System.Net.Http.Headers;
using Blazored.LocalStorage;

namespace SocarDispatch.Web.Handlers;

public class AuthorizationHeaderHandler : DelegatingHandler
{
    private readonly ILocalStorageService _localStorage;
    private const string AuthTokenKey = "authToken";

    public AuthorizationHeaderHandler(ILocalStorageService localStorage)
    {
        _localStorage = localStorage;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, 
        CancellationToken cancellationToken)
    {
        try
        {
            // LocalStorage'dan kayıtlı token'ı oku
            var token = await _localStorage.GetItemAsync<string>(AuthTokenKey);

            // Token varsa Authorization header'ı olarak ekle
            if (!string.IsNullOrWhiteSpace(token))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }
        }
        catch
        {
            // İlk yükleme veya JS Interop hazır olmama durumunda isteği kesme, başlık eklemeden devam et
        }

        return await base.SendAsync(request, cancellationToken);
    }
}
