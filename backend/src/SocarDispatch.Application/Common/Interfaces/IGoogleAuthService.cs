namespace SocarDispatch.Application.Common.Interfaces;

public record GoogleUserInfo(
    string Email,
    string FirstName,
    string LastName,
    string? PictureUrl);

public interface IGoogleAuthService
{
    Task<GoogleUserInfo> VerifyIdTokenAsync(string idToken, CancellationToken cancellationToken = default);
}