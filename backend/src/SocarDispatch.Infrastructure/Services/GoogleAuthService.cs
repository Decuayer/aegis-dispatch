using Google.Apis.Auth;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SocarDispatch.Application.Common.Interfaces;
using SocarDispatch.Domain.Exceptions;

namespace SocarDispatch.Infrastructure.Services;

public class GoogleAuthService : IGoogleAuthService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<GoogleAuthService> _logger;

    public GoogleAuthService(IConfiguration configuration, ILogger<GoogleAuthService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<GoogleUserInfo> VerifyIdTokenAsync(string idToken, CancellationToken cancellationToken = default)
    {
        try
        {
            var clientId = _configuration["GOOGLE_CLIENT_ID"] ?? _configuration["GoogleAuth:ClientId"];

            var settings = new GoogleJsonWebSignature.ValidationSettings();
            if (!string.IsNullOrEmpty(clientId))
            {
                settings.Audience = new[] { clientId };
            }

            var payload = await GoogleJsonWebSignature.ValidateAsync(idToken, settings);

            return new GoogleUserInfo(
                Email: payload.Email,
                FirstName: payload.GivenName ?? "GoogleUser",
                LastName: payload.FamilyName ?? string.Empty,
                PictureUrl: payload.Picture
            );
        }
        catch (InvalidJwtException ex)
        {
            _logger.LogWarning(ex, "Invalid Google ID Token received.");
            throw new DomainException("Invalid or expired Google ID token.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while validating Google ID Token.");
            throw new DomainException("An error occurred during the Google authentication process.");
        }
    }
}