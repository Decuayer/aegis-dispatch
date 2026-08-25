using SocarDispatch.Web.Models.Auth;
using SocarDispatch.Web.Models.Common;

namespace SocarDispatch.Web.Services;

public interface IUserService
{
    /// Retrieves a list of users filtered by search query and/or role.
    Task<ApiResponse<List<UserDto>>?> GetUsersAsync(string? search = null, RoleType? role = null, CancellationToken cancellationToken = default);
}
