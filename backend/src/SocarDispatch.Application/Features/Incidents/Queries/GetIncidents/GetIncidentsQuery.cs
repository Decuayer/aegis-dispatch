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
    public Guid? ReporterId { get; init; }

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

    // Alias for FromDate (SDDC-50)
    public DateTime? DateFrom
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

    // Alias for ToDate (SDDC-50)
    public DateTime? DateTo
    {
        get => _toDate;
        init => _toDate = value;
    }

    // Dynamic sorting parameters (SDDC-50)
    public string SortBy { get; init; } = "createdAt";
    public string SortDir { get; init; } = "desc";

    // Page alias for PageNumber (SDDC-45)
    public int Page
    {
        get => PageNumber;
        init => PageNumber = value;
    }

    public GetIncidentsQuery()
    {
        PageSize = 20;
    }

    public GetIncidentsQuery(string? status, string? category, int page = 1, int pageSize = 20)
    {
        Status = status;
        Category = category;
        Page = page;
        PageSize = pageSize;
    }

    public GetIncidentsQuery(string? status, string? category, DateTime? from, DateTime? to)
    {
        Status = status;
        Category = category;
        _fromDate = from;
        _toDate = to;
        PageSize = 20;
    }

    public GetIncidentsQuery(Guid? reporterId, string? status, string? category, DateTime? from, DateTime? to)
    {
        ReporterId = reporterId;
        Status = status;
        Category = category;
        _fromDate = from;
        _toDate = to;
        PageSize = 20;
    }

    public GetIncidentsQuery(
        string? status,
        string? category,
        string? emergencyCode,
        DateTime? dateFrom,
        DateTime? dateTo,
        string sortBy = "createdAt",
        string sortDir = "desc",
        int page = 1,
        int pageSize = 20)
    {
        Status = status;
        Category = category;
        EmergencyCode = emergencyCode;
        _fromDate = dateFrom;
        _toDate = dateTo;
        SortBy = sortBy;
        SortDir = sortDir;
        Page = page;
        PageSize = pageSize;
    }
}
