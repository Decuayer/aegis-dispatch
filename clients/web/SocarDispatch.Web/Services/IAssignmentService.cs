using SocarDispatch.Web.Models.Common;
using SocarDispatch.Web.Models.Dispatch;

namespace SocarDispatch.Web.Services;

public interface IAssignmentService
{
    /// Dispatches and assigns an emergency team to an incident.
    Task<ApiResponse<AssignmentDto>?> AssignTeamAsync(DispatchRequestDto request, CancellationToken cancellationToken = default);
}
