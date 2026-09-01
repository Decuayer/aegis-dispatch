using SocarDispatch.Web.Models.Auth;
using SocarDispatch.Web.Models.Common;

namespace SocarDispatch.Web.Services;

public interface IUserService
{
    event Action<UserDto>? OnUserProfileUpdated;

    /// <summary>
    /// Retrieves paginated user directory matching query criteria.
    /// </summary>
    Task<ApiResponse<PagedResult<UserDto>>?> GetUsersAsync(
        int pageNumber = 1,
        int pageSize = 25,
        RoleType? role = null,
        string? department = null,
        Guid? userId = null,
        string? searchTerm = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves users matching criteria for selection dropdowns and team modals.
    /// </summary>
    Task<ApiResponse<List<UserDto>>?> GetAllUsersAsync(
        string? search = null, 
        RoleType? role = null, 
        CancellationToken cancellationToken = default);

    Task<ApiResponse<UserDto>?> GetCurrentUserAsync(CancellationToken cancellationToken = default);
    Task<ApiResponse<UserDto>?> UpdateCurrentUserProfileAsync(UpdateUserProfileRequestDto request, CancellationToken cancellationToken = default);
    Task<ApiResponse<UserDto>?> UpdateUserRoleAsync(Guid userId, UpdateUserRoleRequestDto request, CancellationToken cancellationToken = default);
}
