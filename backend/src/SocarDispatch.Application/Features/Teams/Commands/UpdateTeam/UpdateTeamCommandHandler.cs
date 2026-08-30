using MediatR;
using Microsoft.EntityFrameworkCore;
using SocarDispatch.Application.Common.Interfaces;
using SocarDispatch.Application.Common.Models;
using SocarDispatch.Application.Features.Teams.DTOs;
using SocarDispatch.Domain.Entities;
using SocarDispatch.Domain.Enums;
using SocarDispatch.Domain.Exceptions;

namespace SocarDispatch.Application.Features.Teams.Commands.UpdateTeam;

public class UpdateTeamCommandHandler : IRequestHandler<UpdateTeamCommand, ApiResponse<TeamDto>>
{
    private readonly IApplicationDbContext _context;

    public UpdateTeamCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ApiResponse<TeamDto>> Handle(UpdateTeamCommand request, CancellationToken cancellationToken)
    {
        var team = await _context.Teams
            .Include(t => t.Members)
            .FirstOrDefaultAsync(t => t.Id == request.Id, cancellationToken);

        if (team == null)
        {
            throw new EntityNotFoundException("Team", request.Id);
        }

        var requester = await _context.Users.FindAsync(new object[] { request.RequesterId }, cancellationToken);
        if (requester == null)
        {
            throw new EntityNotFoundException("User", request.RequesterId);
        }

        bool isOperator = requester.RoleType == RoleType.Operator;
        bool isCurrentLeader = team.LeaderId.HasValue && team.LeaderId.Value == request.RequesterId;
        bool isVacant = team.LeaderId == null;
        bool isRequesterActiveMember = team.Members.Any(m => m.UserId == request.RequesterId);

        if (isVacant)
        {
            if (!isOperator)
            {
                if (!isRequesterActiveMember || request.LeaderId != request.RequesterId)
                {
                    throw new ForbiddenAccessException("Only an active member of this team can claim vacant leadership for themselves, or an operator can manage the team.");
                }
            }
        }
        else
        {
            if (!isCurrentLeader && !isOperator)
            {
                throw new ForbiddenAccessException("Only the team leader or operator can update it.");
            }
        }

        if (!team.TeamName.Equals(request.TeamName, StringComparison.OrdinalIgnoreCase))
        {
            var nameExists = await _context.Teams
                .AnyAsync(t => t.Id != request.Id && t.TeamName.ToLower() == request.TeamName.ToLower(), cancellationToken);
            if (nameExists)
            {
                throw new DomainException($"A team with name '{request.TeamName}' already exists.");
            }
            team.TeamName = request.TeamName;
        }

        if (request.LeaderId.HasValue && request.LeaderId.Value != Guid.Empty)
        {
            var newLeader = await _context.Users.FindAsync(new object[] { request.LeaderId.Value }, cancellationToken);
            if (newLeader == null)
            {
                throw new EntityNotFoundException("User", request.LeaderId.Value);
            }

            if (newLeader.RoleType != RoleType.Team)
            {
                throw new DomainException("Designated team leader must have RoleType 'Team'.");
            }

            team.LeaderId = request.LeaderId;

            if (!team.Members.Any(m => m.UserId == request.LeaderId.Value))
            {
                team.Members.Add(new TeamMember
                {
                    TeamId = team.Id,
                    UserId = request.LeaderId.Value
                });
            }
        }
        else
        {
            team.LeaderId = null;
        }

        team.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);

        var updatedTeam = await _context.Teams
            .Include(t => t.Leader)
            .Include(t => t.Members)
                .ThenInclude(m => m.User)
            .FirstAsync(t => t.Id == team.Id, cancellationToken);

        var dto = MapToTeamDto(updatedTeam);
        return ApiResponse<TeamDto>.SuccessResult(dto, "Team updated successfully.");
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
