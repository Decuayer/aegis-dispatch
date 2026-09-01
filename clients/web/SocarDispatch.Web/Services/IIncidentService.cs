using SocarDispatch.Web.Models.Common;
using SocarDispatch.Web.Models.Dispatch;

namespace SocarDispatch.Web.Services;

public interface IIncidentService
{
    /// <summary>
    /// Fetches full incident details by unique ID.
    /// </summary>
    Task<ApiResponse<IncidentDetailViewModel>?> GetIncidentByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Fetches all active/open emergency incidents.
    /// </summary>
    Task<ApiResponse<List<IncidentDetailViewModel>>?> GetActiveIncidentsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Fetches server-side paginated and filtered incidents matching query criteria.
    /// </summary>
    Task<ApiResponse<PagedResult<IncidentDetailViewModel>>?> GetIncidentsAsync(
        int pageNumber = 1,
        int pageSize = 25,
        string? searchTerm = null,
        string? status = null,
        string? category = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Fetches incidents filtered by optional status, category, and temporal date range.
    /// </summary>
    Task<ApiResponse<List<IncidentDetailViewModel>>?> GetAllIncidentsAsync(
        string? status = null, 
        string? category = null, 
        DateTime? from = null, 
        DateTime? to = null, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new emergency incident report submitted by the operator.
    /// </summary>
    Task<ApiResponse<IncidentDetailViewModel>?> CreateIncidentAsync(CreateIncidentRequestDto request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates core details (category, emergency code, description, location) of an existing incident.
    /// </summary>
    Task<ApiResponse<IncidentDetailViewModel>?> UpdateIncidentAsync(Guid id, UpdateIncidentRequestDto request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Transitions incident operational lifecycle status with optional completion notes.
    /// </summary>
    Task<ApiResponse<IncidentDetailViewModel>?> ChangeIncidentStatusAsync(Guid id, ChangeIncidentStatusRequestDto request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Legacy status update method forwarding to ChangeIncidentStatusAsync.
    /// </summary>
    Task<ApiResponse<IncidentDetailViewModel>?> UpdateStatusAsync(Guid id, string status, string? completionNotes = null, CancellationToken cancellationToken = default);
}
