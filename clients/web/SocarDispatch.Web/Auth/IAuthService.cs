using SocarDispatch.Web.Models.Auth;
using SocarDispatch.Web.Models.Common;

namespace SocarDispatch.Web.Auth;

public interface IAuthService
{
    Task<ApiResponse<AuthResponseDto>> LoginAsync(LoginModel model);
    Task<ApiResponse<AuthResponseDto>> GoogleLoginAsync(string idToken);
    Task LogoutAsync();
    Task HandleSessionExpiredAsync(string? returnUrl = null);
    Task<string?> GetTokenAsync();
}
