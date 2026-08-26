using SocarDispatch.Web.Models.Common;
using SocarDispatch.Web.Models.Dispatch;

namespace SocarDispatch.Web.Services;

public interface ITeamService
{
    /// Retrieves all response teams, their active members, statuses, and GPS locations.
    Task<ApiResponse<List<TeamDto>>?> GetTeamsAsync(CancellationToken cancellationToken = default);

    /// Retrieves details of a single team by unique ID.
    Task<ApiResponse<TeamDto>?> GetTeamByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// Creates a new response team.
    Task<ApiResponse<TeamDto>?> CreateTeamAsync(CreateTeamRequestDto request, CancellationToken cancellationToken = default);

    /// Updates team name and/or designated leader.
    Task<ApiResponse<TeamDto>?> UpdateTeamAsync(Guid id, UpdateTeamRequestDto request, CancellationToken cancellationToken = default);

    /// Updates the operational status of a team.
    Task<ApiResponse<TeamDto>?> UpdateTeamStatusAsync(Guid id, string status, CancellationToken cancellationToken = default);

    /// Adds a personnel to the team roster.
    Task<ApiResponse<TeamDto>?> AddMemberAsync(Guid teamId, Guid userId, CancellationToken cancellationToken = default);

    /// Removes a personnel from the team roster.
    Task<ApiResponse<TeamDto>?> RemoveMemberAsync(Guid teamId, Guid userId, CancellationToken cancellationToken = default);

    /// Updates the operational status of an individual team member.
    Task<ApiResponse<TeamMemberDto>?> UpdateMemberStatusAsync(Guid teamId, Guid userId, string status, CancellationToken cancellationToken = default);
}
