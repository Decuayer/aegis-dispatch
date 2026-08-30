using MediatR;
using Microsoft.EntityFrameworkCore;
using SocarDispatch.Application.Common.Interfaces;
using SocarDispatch.Application.Common.Models;
using SocarDispatch.Application.Features.Teams.DTOs;
using SocarDispatch.Domain.Entities;
using SocarDispatch.Domain.Enums;
using SocarDispatch.Domain.Exceptions;

namespace SocarDispatch.Application.Features.Teams.Commands.AddTeamMember;

public class AddTeamMemberCommandHandler : IRequestHandler<AddTeamMemberCommand, ApiResponse<TeamDto>>
{
    private readonly IApplicationDbContext _context;

    public AddTeamMemberCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ApiResponse<TeamDto>> Handle(AddTeamMemberCommand request, CancellationToken cancellationToken)
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

        bool isSelfJoin = request.RequesterId == request.UserId;

        if (isSelfJoin)
        {
            if (requester.RoleType != RoleType.Team)
            {
                throw new DomainException("User must have RoleType 'Team' to join a team.");
            }

            if (team.Status != TeamStatus.Idle)
            {
                throw new DomainException("Cannot join a team that is not in Idle status.");
            }
        }
        else
        {
            if (team.LeaderId != request.RequesterId && requester.RoleType != RoleType.Operator)
            {
                throw new ForbiddenAccessException("Only team leader or operator can add team members.");
            }
        }

        if (team.Members.Count >= TeamConstants.MaxOperationalCapacity)
        {
            throw new DomainException("Team has reached its maximum operational capacity.");
        }

        var targetUser = isSelfJoin ? requester : await _context.Users.FindAsync(new object[] { request.UserId }, cancellationToken);
        if (targetUser == null)
        {
            throw new EntityNotFoundException("User", request.UserId);
        }

        if (targetUser.RoleType != RoleType.Team)
        {
            throw new DomainException("User must have RoleType 'Team' to be added to a team.");
        }

        var isAlreadyInAnyTeam = await _context.TeamMembers.AnyAsync(tm => tm.UserId == request.UserId, cancellationToken);
        if (isAlreadyInAnyTeam)
        {
            throw new DomainException("User is already an active member of another team.");
        }

        _context.TeamMembers.Add(new TeamMember
        {
            TeamId = request.TeamId,
            UserId = request.UserId
        });

        team.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);

        var updatedTeam = await _context.Teams
            .Include(t => t.Leader)
            .Include(t => t.Members)
                .ThenInclude(m => m.User)
            .FirstAsync(t => t.Id == team.Id, cancellationToken);

        var dto = MapToTeamDto(updatedTeam);
        return ApiResponse<TeamDto>.SuccessResult(dto, "Team member added successfully.");
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
