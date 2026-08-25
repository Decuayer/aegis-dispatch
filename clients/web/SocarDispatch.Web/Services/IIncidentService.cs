using SocarDispatch.Web.Models.Common;
using SocarDispatch.Web.Models.Dispatch;

namespace SocarDispatch.Web.Services;

public interface IIncidentService
{
    /// Fetches full incident details by unique ID.
    Task<ApiResponse<IncidentDetailViewModel>?> GetIncidentByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// Fetches all active/open emergency incidents.
    Task<ApiResponse<List<IncidentDetailViewModel>>?> GetActiveIncidentsAsync(CancellationToken cancellationToken = default);

    /// Fetches incidents filtered by optional status and category from backend.
    Task<ApiResponse<List<IncidentDetailViewModel>>?> GetAllIncidentsAsync(string? status = null, string? category = null, CancellationToken cancellationToken = default);

    /// Updates the operational status of an incident.
    Task<ApiResponse<IncidentDetailViewModel>?> UpdateStatusAsync(Guid id, string status, string? completionNotes = null, CancellationToken cancellationToken = default);
}
