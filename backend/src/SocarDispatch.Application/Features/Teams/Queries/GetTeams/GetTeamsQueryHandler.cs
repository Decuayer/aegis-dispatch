using MediatR;
using Microsoft.EntityFrameworkCore;
using SocarDispatch.Application.Common.Extensions;
using SocarDispatch.Application.Common.Interfaces;
using SocarDispatch.Application.Common.Models;
using SocarDispatch.Application.Features.Teams.DTOs;

namespace SocarDispatch.Application.Features.Teams.Queries.GetTeams;

public class GetTeamsQueryHandler : IRequestHandler<GetTeamsQuery, ApiResponse<PagedResult<TeamDto>>>
{
    private readonly IApplicationDbContext _context;

    public GetTeamsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ApiResponse<PagedResult<TeamDto>>> Handle(GetTeamsQuery request, CancellationToken cancellationToken)
    {
        var query = _context.Teams
            .Include(t => t.Leader)
            .Include(t => t.Members)
                .ThenInclude(tm => tm.User)
            .AsNoTracking()
            .AsQueryable();

        // Exact TeamId filter (direct lookup without pulling full rosters)
        if (request.TeamId.HasValue)
        {
            query = query.Where(t => t.Id == request.TeamId.Value);
        }

        // Status filter (utilizes composite index IX_Teams_Status_TeamName)
        if (request.Status.HasValue)
        {
            query = query.Where(t => t.Status == request.Status.Value);
        }

        // Case-insensitive team name search
        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var searchLower = request.SearchTerm.Trim().ToLower();
            query = query.Where(t => t.TeamName.ToLower().Contains(searchLower));
        }

        // Deterministic ordering for pagination stability
        query = query.OrderBy(t => t.Status).ThenBy(t => t.TeamName);

        // Project to DTO
        var projectedQuery = query.Select(t => new TeamDto
        {
            Id = t.Id,
            TeamName = t.TeamName,
            Status = t.Status.ToString(),
            LeaderId = t.LeaderId,
            LeaderFullName = t.Leader != null ? $"{t.Leader.FirstName} {t.Leader.LastName}".Trim() : null,
            CurrentLatitude = t.CurrentLatitude,
            CurrentLongitude = t.CurrentLongitude,
            UpdatedAt = t.UpdatedAt,
            Members = t.Members.Select(m => new TeamMemberDto
            {
                UserId = m.UserId,
                FullName = $"{m.User.FirstName} {m.User.LastName}".Trim(),
                Email = m.User.Email,
                Phone = m.User.Phone,
                Department = m.User.Department,
                SubRole = m.User.SubRole,
                MemberStatus = m.MemberStatus.ToString(),
                StatusUpdatedAt = m.StatusUpdatedAt,
                JoinedAt = m.JoinedAt
            }).ToList()
        });

        // Execute server-side pagination
        var pagedResult = await projectedQuery.ToPagedResultAsync(
            request.PageNumber,
            request.PageSize,
            cancellationToken);

        return ApiResponse<PagedResult<TeamDto>>.SuccessResult(pagedResult, "Teams retrieved successfully.");
    }
}
