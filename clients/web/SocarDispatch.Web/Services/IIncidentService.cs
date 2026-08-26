using SocarDispatch.Web.Models.Common;
using SocarDispatch.Web.Models.Dispatch;

namespace SocarDispatch.Web.Services;

public interface IIncidentService
{
    /// Fetches full incident details by unique ID.
    Task<ApiResponse<IncidentDetailViewModel>?> GetIncidentByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// Fetches all active/open emergency incidents.
    Task<ApiResponse<List<IncidentDetailViewModel>>?> GetActiveIncidentsAsync(CancellationToken cancellationToken = default);

    /// Fetches incidents filtered by optional status, category, and temporal date range.
    Task<ApiResponse<List<IncidentDetailViewModel>>?> GetAllIncidentsAsync(
        string? status = null, 
        string? category = null, 
        DateTime? from = null, 
        DateTime? to = null, 
        CancellationToken cancellationToken = default);

    /// Creates a new emergency incident report submitted by the operator.
    Task<ApiResponse<IncidentDetailViewModel>?> CreateIncidentAsync(CreateIncidentRequestDto request, CancellationToken cancellationToken = default);

    /// Updates core details (category, emergency code, description, location) of an existing incident.
    Task<ApiResponse<IncidentDetailViewModel>?> UpdateIncidentAsync(Guid id, UpdateIncidentRequestDto request, CancellationToken cancellationToken = default);

    /// Transitions incident operational lifecycle status with optional completion notes.
    Task<ApiResponse<IncidentDetailViewModel>?> ChangeIncidentStatusAsync(Guid id, ChangeIncidentStatusRequestDto request, CancellationToken cancellationToken = default);

    /// Legacy status update method forwarding to ChangeIncidentStatusAsync.
    Task<ApiResponse<IncidentDetailViewModel>?> UpdateStatusAsync(Guid id, string status, string? completionNotes = null, CancellationToken cancellationToken = default);
}
