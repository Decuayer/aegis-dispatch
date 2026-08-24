using SocarDispatch.Web.Models.Common;
using SocarDispatch.Web.Models.Dispatch;

namespace SocarDispatch.Web.Services;

public interface ITeamService
{
    /// Retrieves all response teams, their active members, statuses, and GPS locations.
    Task<ApiResponse<List<TeamDto>>?> GetTeamsAsync(CancellationToken cancellationToken = default);

    /// Retrieves details of a single team by unique ID.
    Task<ApiResponse<TeamDto>?> GetTeamByIdAsync(Guid id, CancellationToken cancellationToken = default);
}
