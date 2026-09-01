using SocarDispatch.Web.Models.Common;
using SocarDispatch.Web.Models.Dispatch;

namespace SocarDispatch.Web.Services;

public interface ITeamService
{
    /// <summary>
    /// Retrieves paginated response teams matching query criteria.
    /// </summary>
    Task<ApiResponse<PagedResult<TeamDto>>?> GetTeamsAsync(
        int pageNumber = 1,
        int pageSize = 25,
        string? status = null,
        Guid? teamId = null,
        string? searchTerm = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all response teams for selector dropdowns and dashboard boards.
    /// </summary>
    Task<ApiResponse<List<TeamDto>>?> GetAllTeamsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves details of a single team by unique ID.
    /// </summary>
    Task<ApiResponse<TeamDto>?> GetTeamByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new response team.
    /// </summary>
    Task<ApiResponse<TeamDto>?> CreateTeamAsync(CreateTeamRequestDto request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates team name and/or designated leader.
    /// </summary>
    Task<ApiResponse<TeamDto>?> UpdateTeamAsync(Guid id, UpdateTeamRequestDto request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the operational status of a team.
    /// </summary>
    Task<ApiResponse<TeamDto>?> UpdateTeamStatusAsync(Guid id, string status, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a personnel to the team roster.
    /// </summary>
    Task<ApiResponse<TeamDto>?> AddMemberAsync(Guid teamId, Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a personnel from the team roster.
    /// </summary>
    Task<ApiResponse<TeamDto>?> RemoveMemberAsync(Guid teamId, Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the operational status of an individual team member.
    /// </summary>
    Task<ApiResponse<TeamMemberDto>?> UpdateMemberStatusAsync(Guid teamId, Guid userId, string status, CancellationToken cancellationToken = default);
}
