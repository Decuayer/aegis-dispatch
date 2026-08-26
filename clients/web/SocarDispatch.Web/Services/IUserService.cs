using SocarDispatch.Web.Models.Auth;
using SocarDispatch.Web.Models.Common;

namespace SocarDispatch.Web.Services;

public interface IUserService
{
    event Action<UserDto>? OnUserProfileUpdated;
    Task<ApiResponse<List<UserDto>>?> GetUsersAsync(string? search = null, RoleType? role = null, CancellationToken cancellationToken = default);
    Task<ApiResponse<UserDto>?> GetCurrentUserAsync(CancellationToken cancellationToken = default);
    Task<ApiResponse<UserDto>?> UpdateCurrentUserProfileAsync(UpdateUserProfileRequestDto request, CancellationToken cancellationToken = default);
    Task<ApiResponse<UserDto>?> UpdateUserRoleAsync(Guid userId, UpdateUserRoleRequestDto request, CancellationToken cancellationToken = default);
}

