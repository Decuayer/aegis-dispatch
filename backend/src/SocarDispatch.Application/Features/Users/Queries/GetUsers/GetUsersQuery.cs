using MediatR;
using SocarDispatch.Application.Common.Models;
using SocarDispatch.Application.Features.Auth.DTOs;
using SocarDispatch.Domain.Enums;

namespace SocarDispatch.Application.Features.Users.Queries.GetUsers;

public record GetUsersQuery : PaginationFilter, IRequest<ApiResponse<PagedResult<UserDto>>>
{
    private readonly string? _search;
    private readonly RoleType? _role;

    public Guid? UserId { get; init; }

    public string? SearchTerm
    {
        get => _search;
        init => _search = value;
    }

    public string? Search
    {
        get => _search;
        init => _search = value;
    }

    public string? Department { get; init; }

    public RoleType? Role
    {
        get => _role;
        init => _role = value;
    }

    public RoleType? RoleType
    {
        get => _role;
        init => _role = value;
    }

    public GetUsersQuery() { }

    public GetUsersQuery(string? search = null, string? department = null, RoleType? role = null, Guid? userId = null, int pageNumber = 1, int pageSize = 25)
    {
        _search = search;
        Department = department;
        _role = role;
        UserId = userId;
        PageNumber = pageNumber;
        PageSize = pageSize;
    }
}
