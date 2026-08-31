using MediatR;
using SocarDispatch.Application.Common.Models;
using SocarDispatch.Application.Features.Teams.DTOs;
using SocarDispatch.Domain.Enums;

namespace SocarDispatch.Application.Features.Teams.Queries.GetTeams;

public record GetTeamsQuery : PaginationFilter, IRequest<ApiResponse<PagedResult<TeamDto>>>
{
    private readonly string? _searchTerm;

    public Guid? TeamId { get; init; }

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

    public TeamStatus? Status { get; init; }

    public GetTeamsQuery() { }

    public GetTeamsQuery(Guid? teamId = null, string? search = null, TeamStatus? status = null, int pageNumber = 1, int pageSize = 25)
    {
        TeamId = teamId;
        _searchTerm = search;
        Status = status;
        PageNumber = pageNumber;
        PageSize = pageSize;
    }
}
