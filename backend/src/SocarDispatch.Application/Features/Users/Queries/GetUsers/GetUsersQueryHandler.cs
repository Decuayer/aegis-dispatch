using MediatR;
using Microsoft.EntityFrameworkCore;
using SocarDispatch.Application.Common.Extensions;
using SocarDispatch.Application.Common.Interfaces;
using SocarDispatch.Application.Common.Models;
using SocarDispatch.Application.Features.Auth.DTOs;

namespace SocarDispatch.Application.Features.Users.Queries.GetUsers;

public class GetUsersQueryHandler : IRequestHandler<GetUsersQuery, ApiResponse<PagedResult<UserDto>>>
{
    private readonly IApplicationDbContext _context;

    public GetUsersQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ApiResponse<PagedResult<UserDto>>> Handle(GetUsersQuery request, CancellationToken cancellationToken)
    {
        var query = _context.Users.AsNoTracking().AsQueryable();

        // Exact UserId lookup
        if (request.UserId.HasValue)
        {
            query = query.Where(u => u.Id == request.UserId.Value);
        }

        // Multi-field text search matching FirstName, LastName, Email, and Phone
        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var searchLower = request.SearchTerm.Trim().ToLower();
            query = query.Where(u =>
                u.FirstName.ToLower().Contains(searchLower) ||
                u.LastName.ToLower().Contains(searchLower) ||
                u.Email.ToLower().Contains(searchLower) ||
                u.Phone.Contains(searchLower));
        }

        // Department filter
        if (!string.IsNullOrWhiteSpace(request.Department))
        {
            var deptLower = request.Department.Trim().ToLower();
            query = query.Where(u => u.Department.ToLower() == deptLower);
        }

        // Role filter (accepts either Role or RoleType)
        var roleFilter = request.Role ?? request.RoleType;
        if (roleFilter.HasValue)
        {
            query = query.Where(u => u.RoleType == roleFilter.Value);
        }

        // Default deterministic ordering (aligned with composite index IX_Users_RoleType_Department_FirstName_LastName)
        query = query.OrderBy(u => u.RoleType)
                     .ThenBy(u => u.Department)
                     .ThenBy(u => u.FirstName)
                     .ThenBy(u => u.LastName);

        // Project to UserDto
        var projectedQuery = query.Select(u => new UserDto
        {
            Id = u.Id,
            FirstName = u.FirstName,
            LastName = u.LastName,
            Email = u.Email,
            Phone = u.Phone,
            Department = u.Department,
            RoleType = u.RoleType,
            SubRole = u.SubRole,
            AvatarUrl = u.AvatarUrl
        });

        // Execute server-side pagination
        var pagedResult = await projectedQuery.ToPagedResultAsync(
            request.PageNumber,
            request.PageSize,
            cancellationToken);

        return ApiResponse<PagedResult<UserDto>>.SuccessResult(pagedResult, "User directory list retrieved successfully.");
    }
}
