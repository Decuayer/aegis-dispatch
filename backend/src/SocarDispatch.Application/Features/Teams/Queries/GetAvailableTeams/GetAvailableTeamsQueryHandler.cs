using MediatR;
using Microsoft.EntityFrameworkCore;
using SocarDispatch.Application.Common.Interfaces;
using SocarDispatch.Application.Common.Models;
using SocarDispatch.Application.Features.Teams.DTOs;
using SocarDispatch.Domain.Enums;

namespace SocarDispatch.Application.Features.Teams.Queries.GetAvailableTeams;

public class GetAvailableTeamsQueryHandler : IRequestHandler<GetAvailableTeamsQuery, ApiResponse<List<AvailableTeamDto>>>
{
    private readonly IApplicationDbContext _context;

    public GetAvailableTeamsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ApiResponse<List<AvailableTeamDto>>> Handle(GetAvailableTeamsQuery request, CancellationToken cancellationToken)
    {
        var teams = await _context.Teams
            .Include(t => t.Leader)
            .Include(t => t.Members)
            .Where(t => t.Status == TeamStatus.Idle && t.Members.Count < TeamConstants.MaxOperationalCapacity)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var list = teams.Select(t => new AvailableTeamDto
        {
            Id = t.Id,
            TeamName = t.TeamName,
            LeaderId = t.LeaderId,
            LeaderFullName = t.Leader != null ? $"{t.Leader.FirstName} {t.Leader.LastName}".Trim() : null,
            MemberCount = t.Members.Count,
            CreatedAt = t.UpdatedAt
        }).ToList();

        return ApiResponse<List<AvailableTeamDto>>.SuccessResult(list, "Available teams retrieved successfully.");
    }
}
