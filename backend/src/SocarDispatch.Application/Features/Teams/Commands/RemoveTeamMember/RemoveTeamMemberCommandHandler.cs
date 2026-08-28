using MediatR;
using Microsoft.EntityFrameworkCore;
using SocarDispatch.Application.Common.Interfaces;
using SocarDispatch.Application.Common.Models;
using SocarDispatch.Application.Features.Teams.DTOs;
using SocarDispatch.Domain.Entities;
using SocarDispatch.Domain.Enums;
using SocarDispatch.Domain.Exceptions;

namespace SocarDispatch.Application.Features.Teams.Commands.RemoveTeamMember;

public class RemoveTeamMemberCommandHandler : IRequestHandler<RemoveTeamMemberCommand, ApiResponse<TeamDto>>
{
    private readonly IApplicationDbContext _context;

    public RemoveTeamMemberCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ApiResponse<TeamDto>> Handle(RemoveTeamMemberCommand request, CancellationToken cancellationToken)
    {
        var team = await _context.Teams
            .Include(t => t.Members)
            .FirstOrDefaultAsync(t => t.Id == request.TeamId, cancellationToken);

        if (team == null)
        {
            throw new EntityNotFoundException("Team", request.TeamId);
        }

        var requester = await _context.Users.FindAsync(new object[] { request.RequesterId }, cancellationToken);
        if (requester == null)
        {
            throw new EntityNotFoundException("User", request.RequesterId);
        }

        if (team.LeaderId != request.RequesterId && requester.RoleType != RoleType.Operator)
        {
            throw new ForbiddenAccessException("Only team leader or operator can remove team members.");
        }

        if (team.LeaderId == request.UserId)
        {
            throw new DomainException("The team leader cannot be removed from the team directly. Transfer leadership first.");
        }

        var hasActiveAssignment = await _context.Assignments
            .Include(a => a.Incident)
            .AnyAsync(a => a.TeamId == request.TeamId &&
                           a.CompletedAt == null &&
                           a.Incident.Status != IncidentStatus.Resolved &&
                           a.Incident.Status != IncidentStatus.Canceled,
                       cancellationToken);

        if (hasActiveAssignment)
        {
            throw new DomainException("Cannot remove member while the team is involved in an active emergency response.");
        }

        var teamMember = team.Members.FirstOrDefault(m => m.UserId == request.UserId);
        if (teamMember == null)
        {
            throw new EntityNotFoundException("TeamMember", request.UserId);
        }

        _context.TeamMembers.Remove(teamMember);
        team.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);

        var updatedTeam = await _context.Teams
            .Include(t => t.Leader)
            .Include(t => t.Members)
                .ThenInclude(m => m.User)
            .FirstAsync(t => t.Id == team.Id, cancellationToken);

        var dto = MapToTeamDto(updatedTeam);
        return ApiResponse<TeamDto>.SuccessResult(dto, "Team member removed successfully.");
    }

    private static TeamDto MapToTeamDto(Team t)
    {
        return new TeamDto
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
                SubRole = m.User.SubRole
            }).ToList()
        };
    }
}
