using MediatR;
using Microsoft.EntityFrameworkCore;
using SocarDispatch.Application.Common.Interfaces;
using SocarDispatch.Application.Common.Models;
using SocarDispatch.Application.Features.Incidents.DTOs;
using SocarDispatch.Domain.Enums;
using SocarDispatch.Domain.Events;
using SocarDispatch.Domain.Exceptions;

namespace SocarDispatch.Application.Features.Incidents.Commands.ChangeIncidentStatus;

public class ChangeIncidentStatusCommandHandler : IRequestHandler<ChangeIncidentStatusCommand, ApiResponse<IncidentDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IPublisher _publisher;

    public ChangeIncidentStatusCommandHandler(IApplicationDbContext context, IPublisher publisher)
    {
        _context = context;
        _publisher = publisher;
    }

    public async Task<ApiResponse<IncidentDto>> Handle(ChangeIncidentStatusCommand request, CancellationToken cancellationToken)
    {
        var incident = await _context.Incidents
            .Include(i => i.Reporter)
            .Include(i => i.Assignments)
                .ThenInclude(a => a.Team)
                    .ThenInclude(t => t.Members)
            .Include(i => i.MediaAttachments)
            .FirstOrDefaultAsync(i => i.Id == request.Id, cancellationToken);

        if (incident == null)
        {
            throw new EntityNotFoundException("Incident", request.Id);
        }

        var requester = await _context.Users.FirstOrDefaultAsync(u => u.Id == request.RequesterId, cancellationToken);
        if (requester == null)
        {
            throw new DomainException("Invalid user session. User not found.");
        }

        var activeAssignment = incident.Assignments
            .Where(a => a.CompletedAt == null)
            .OrderByDescending(a => a.AssignedAt)
            .FirstOrDefault();

        var targetStatus = Enum.Parse<IncidentStatus>(request.Status, true);

        // -------------------------------------------------------------
        // 1. Contextual Role-Based Authorization (RBAC Validation)
        // -------------------------------------------------------------
        if (requester.RoleType == RoleType.Operator)
        {
            // Operator herhangi bir statüye geçirebilir.
        }
        else if (requester.RoleType == RoleType.Team)
        {
            // Ekip yalnızca statüyü 'Resolved' yapabilir.
            if (targetStatus != IncidentStatus.Resolved)
            {
                throw new ForbiddenAccessException("Team members can only set incident status to Resolved.");
            }

            // Ekibin bu olaya atanmış aktif ekip olup olmadığının kontrolü
            if (activeAssignment == null)
            {
                throw new ForbiddenAccessException("You are not authorized to update status for an unassigned incident.");
            }

            var isTeamMember = activeAssignment.Team.Members.Any(m => m.UserId == request.RequesterId)
                               || activeAssignment.Team.LeaderId == request.RequesterId;

            if (!isTeamMember)
            {
                throw new ForbiddenAccessException("You are only authorized to resolve incidents assigned to your team.");
            }
        }
        else if (requester.RoleType == RoleType.Employee || requester.Id == incident.ReporterId)
        {
            // Reporter/Çalışan yalnızca kendi bildirdiği Open & unassigned olayları 'Canceled' yapabilir.
            if (targetStatus != IncidentStatus.Canceled)
            {
                throw new ForbiddenAccessException("Reporters can only cancel incidents.");
            }

            if (incident.ReporterId != request.RequesterId)
            {
                throw new ForbiddenAccessException("Only the original reporter can cancel this incident.");
            }

            if (incident.Status != IncidentStatus.Open || activeAssignment != null)
            {
                throw new DomainException("Incidents can only be canceled by reporter when status is Open and unassigned.");
            }
        }
        else
        {
            throw new ForbiddenAccessException("You do not have permission to change the status of this incident.");
        }

        // -------------------------------------------------------------
        // 2. Automatic Team Release & Assignment Completion
        // -------------------------------------------------------------
        var previousStatus = incident.Status;
        incident.Status = targetStatus;

        if (targetStatus == IncidentStatus.Resolved || targetStatus == IncidentStatus.Canceled)
        {
            if (activeAssignment != null)
            {
                activeAssignment.Team.Status = TeamStatus.Idle;
                activeAssignment.Team.UpdatedAt = DateTime.UtcNow;
                activeAssignment.CompletedAt = DateTime.UtcNow;
                if (!string.IsNullOrWhiteSpace(request.CompletionNotes))
                {
                    activeAssignment.CompletionNotes = request.CompletionNotes;
                }
            }
        }

        await _context.SaveChangesAsync(cancellationToken);

        // -------------------------------------------------------------
        // 3. Real-Time Event Dispatch
        // -------------------------------------------------------------
        await _publisher.Publish(new IncidentStatusChangedEvent(
            incident.Id,
            previousStatus,
            incident.Status,
            request.RequesterId,
            DateTime.UtcNow
        ), cancellationToken);

        // DTO Dönüşü
        var dto = new IncidentDto
        {
            Id = incident.Id,
            ReporterId = incident.ReporterId,
            ReporterFullName = $"{incident.Reporter.FirstName} {incident.Reporter.LastName}".Trim(),
            ReporterPhone = incident.Reporter.Phone ?? string.Empty,
            ReporterDepartment = incident.Reporter.Department ?? string.Empty,
            ReporterEmail = incident.Reporter.Email ?? string.Empty,
            ReporterSubRole = incident.Reporter.SubRole ?? string.Empty,
            ReporterAvatarUrl = incident.Reporter.AvatarUrl ?? string.Empty,
            Category = incident.Category,
            EmergencyCode = incident.EmergencyCode,
            Description = incident.Description,
            MediaAttachments = incident.MediaAttachments.Select(m => new IncidentMediaDto
            {
                Id = m.Id,
                MediaUrl = m.MediaUrl,
                MediaType = m.MediaType,
                CreatedAt = m.CreatedAt
            }).ToList(),
            Status = incident.Status.ToString(),
            Latitude = incident.Latitude,
            Longitude = incident.Longitude,
            CreatedAt = incident.CreatedAt,
            AssignedTeamId = activeAssignment?.TeamId,
            AssignedTeamName = activeAssignment?.Team.TeamName
        };

        return ApiResponse<IncidentDto>.SuccessResult(dto, "Incident status updated successfully.");
    }
}
