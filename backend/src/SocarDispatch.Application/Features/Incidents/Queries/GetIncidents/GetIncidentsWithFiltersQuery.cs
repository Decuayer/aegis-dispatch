namespace SocarDispatch.Application.Features.Incidents.Queries.GetIncidents;

public record GetIncidentsWithFiltersQuery(
    string? Status = null,
    string? Category = null,
    string? EmergencyCode = null,
    DateTime? DateFrom = null,
    DateTime? DateTo = null,
    string SortBy = "createdAt",
    string SortDir = "desc",
    int Page = 1,
    int PageSize = 20
) : GetIncidentsQuery(Status, Category, EmergencyCode, DateFrom, DateTo, SortBy, SortDir, Page, PageSize);
