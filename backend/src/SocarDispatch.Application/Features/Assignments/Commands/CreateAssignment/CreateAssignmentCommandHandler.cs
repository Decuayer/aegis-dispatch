using MediatR;
using Microsoft.EntityFrameworkCore;
using SocarDispatch.Application.Common.Interfaces;
using SocarDispatch.Application.Common.Models;
using SocarDispatch.Application.Features.Assignments.DTOs;
using SocarDispatch.Domain.Entities;
using SocarDispatch.Domain.Enums;
using SocarDispatch.Domain.Events;
using SocarDispatch.Domain.Exceptions;

namespace SocarDispatch.Application.Features.Assignments.Commands.CreateAssignment;

public class CreateAssignmentCommandHandler : IRequestHandler<CreateAssignmentCommand, ApiResponse<AssignmentDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IPublisher _publisher;

    public CreateAssignmentCommandHandler(IApplicationDbContext context, IPublisher publisher)
    {
        _context = context;
        _publisher = publisher;
    }

    public async Task<ApiResponse<AssignmentDto>> Handle(CreateAssignmentCommand request, CancellationToken cancellationToken)
    {
        var incident = await _context.Incidents.FindAsync(new object[] { request.IncidentId }, cancellationToken);
        if (incident == null)
        {
            throw new EntityNotFoundException("Incident", request.IncidentId);
        }

        var team = await _context.Teams.FindAsync(new object[] { request.TeamId }, cancellationToken);
        if (team == null)
        {
            throw new EntityNotFoundException("Team", request.TeamId);
        }

        var operatorUser = await _context.Users.FindAsync(new object[] { request.OperatorId }, cancellationToken);
        if (operatorUser == null)
        {
            throw new EntityNotFoundException("Operator", request.OperatorId);
        }

        // Active assignment validation
        var hasActiveAssignment = await _context.Assignments
            .Include(a => a.Incident)
            .AnyAsync(a => a.TeamId == request.TeamId &&
                           a.CompletedAt == null &&
                           a.Incident.Status != IncidentStatus.Resolved &&
                           a.Incident.Status != IncidentStatus.Canceled,
                       cancellationToken);

        if (hasActiveAssignment)
        {
            throw new DomainException("The selected team is already assigned to an active incident.");
        }

        // 1. Create an assignment record
        var assignment = new Assignment
        {
            IncidentId = request.IncidentId,
            TeamId = request.TeamId,
            OperatorId = request.OperatorId,
            AssignedAt = DateTime.UtcNow
        };

        _context.Assignments.Add(assignment);

        // 2nd Business Rule: Set the event status to 'Assigned' and the team status to 'Forwarded'.
        incident.Status = IncidentStatus.Assigned;
        team.Status = TeamStatus.Forwarded;
        team.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        // 3. Publish AssignmentCreatedEvent for downstream workflows
        await _publisher.Publish(new AssignmentCreatedEvent(
            assignment.Id,
            assignment.IncidentId,
            assignment.TeamId,
            assignment.OperatorId,
            assignment.AssignedAt
        ), cancellationToken);

        await _publisher.Publish(new TeamDispatchedEvent(
            assignment.Id,
            assignment.IncidentId,
            assignment.TeamId,
            team.TeamName,
            assignment.OperatorId,
            assignment.AssignedAt
        ), cancellationToken);

        var dto = new AssignmentDto
        {
            Id = assignment.Id,
            IncidentId = incident.Id,
            TeamId = team.Id,
            TeamName = team.TeamName,
            OperatorId = operatorUser.Id,
            OperatorFullName = $"{operatorUser.FirstName} {operatorUser.LastName}".Trim(),
            AssignedAt = assignment.AssignedAt
        };

        return ApiResponse<AssignmentDto>.SuccessResult(dto, "Team successfully assigned to the incident.");
    }
}
