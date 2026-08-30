using MediatR;
using SocarDispatch.Application.Common.Models;
using SocarDispatch.Application.Features.Incidents.DTOs;

namespace SocarDispatch.Application.Features.Incidents.Queries.GetIncidents;

public record GetIncidentsQuery : PaginationFilter, IRequest<ApiResponse<PagedResult<IncidentDto>>>
{
    private readonly string? _searchTerm;
    private readonly DateTime? _fromDate;
    private readonly DateTime? _toDate;

    public Guid? IncidentId { get; init; }

    public string? SearchTerm
    {
        get => _searchTerm;
        init => _searchTerm = value;
    }

    public string? Search
    {
        get => _searchTerm;
        init => _searchTerm = value;
    }

    public string? Status { get; init; }
    public string? Category { get; init; }
    public string? EmergencyCode { get; init; }

    public DateTime? FromDate
    {
        get => _fromDate;
        init => _fromDate = value;
    }

    public DateTime? From
    {
        get => _fromDate;
        init => _fromDate = value;
    }

    public DateTime? ToDate
    {
        get => _toDate;
        init => _toDate = value;
    }

    public DateTime? To
    {
        get => _toDate;
        init => _toDate = value;
    }

    public GetIncidentsQuery() { }

    public GetIncidentsQuery(string? status, string? category, DateTime? from, DateTime? to)
    {
        Status = status;
        Category = category;
        _fromDate = from;
        _toDate = to;
    }
}
