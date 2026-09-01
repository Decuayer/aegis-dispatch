using MediatR;
using NetTopologySuite.Geometries;
using Microsoft.EntityFrameworkCore;
using SocarDispatch.Application.Common.Interfaces;
using SocarDispatch.Application.Common.Models;
using SocarDispatch.Application.Features.Incidents.DTOs;
using SocarDispatch.Domain.Exceptions;
using SocarDispatch.Domain.Entities;
using SocarDispatch.Domain.Enums;
using SocarDispatch.Domain.Events;

namespace SocarDispatch.Application.Features.Incidents.Commands.UpdateIncident;

public class UpdateIncidentCommandHandler : IRequestHandler<UpdateIncidentCommand, ApiResponse<IncidentDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IPublisher? _publisher;

    public UpdateIncidentCommandHandler(IApplicationDbContext context, IPublisher? publisher = null)
    {
        _context = context;
        _publisher = publisher;
    }

    public async Task<ApiResponse<IncidentDto>> Handle(UpdateIncidentCommand request, CancellationToken cancellationToken)
    {
        var incident = await _context.Incidents
            .Include(i => i.Reporter)
            .Include(i => i.Assignments).ThenInclude(a => a.Team)
            .Include(i => i.MediaAttachments)
            .FirstOrDefaultAsync(i => i.Id == request.Id, cancellationToken);

        if (incident == null)
        {
            throw new EntityNotFoundException("Incident", request.Id);
        }

        // Authorization / Ownership Check (Reporter or Operator)
        var requester = await _context.Users.FirstOrDefaultAsync(u => u.Id == request.RequesterId, cancellationToken);
        if (incident.ReporterId != request.RequesterId && requester?.RoleType != RoleType.Operator)
        {
            throw new ForbiddenAccessException("You do not have permission to update this incident.");
        }

        var categoryExists = await _context.IncidentCategories
            .AnyAsync(c => c.Code == request.Category && c.IsActive, cancellationToken);
        if (!categoryExists)
        {
            throw new DomainException($"Invalid incident category: '{request.Category}'");
        }

        incident.Category = request.Category;
        incident.EmergencyCode = request.EmergencyCode;
        incident.Description = request.Description;
        // Safely remove existing media attachments and add new ones
        foreach (var existing in incident.MediaAttachments.ToList())
        {
            _context.IncidentMedia.Remove(existing);
        }

        if (request.MediaAttachments != null && request.MediaAttachments.Count > 0)
        {
            foreach (var m in request.MediaAttachments)
            {
                _context.IncidentMedia.Add(new IncidentMedia
                {
                    IncidentId = incident.Id,
                    MediaUrl = m.MediaUrl,
                    MediaType = m.MediaType,
                    CreatedAt = DateTime.UtcNow
                });
            }
        }
        incident.Latitude = request.Latitude;
        incident.Longitude = request.Longitude;
        incident.Location = new Point((double)request.Longitude, (double)request.Latitude) { SRID = 4326 };

        await _context.SaveChangesAsync(cancellationToken);

        // Publish domain event for real-time SignalR broadcasts
        if (_publisher != null)
        {
            await _publisher.Publish(new IncidentUpdatedEvent(
                incident.Id,
                incident.Category,
                incident.EmergencyCode,
                incident.Description,
                incident.Latitude,
                incident.Longitude,
                request.RequesterId,
                DateTime.UtcNow
            ), cancellationToken);
        }

        var latestAssignment = incident.Assignments.OrderByDescending(a => a.AssignedAt).FirstOrDefault();

        var dto = new IncidentDto
        {
            Id = incident.Id,
            ReporterId = incident.ReporterId,
            ReporterFullName = $"{incident.Reporter.FirstName} {incident.Reporter.LastName}".Trim(),
            Category = incident.Category,
            EmergencyCode = incident.EmergencyCode,
            Description = incident.Description,
            Status = incident.Status.ToString(),
            Latitude = incident.Latitude,
            Longitude = incident.Longitude,
            CreatedAt = incident.CreatedAt,
            AssignedAt = latestAssignment?.AssignedAt,
            CompletedAt = latestAssignment?.CompletedAt,
            AssignedTeamId = latestAssignment?.TeamId,
            AssignedTeamName = latestAssignment?.Team.TeamName,
            CompletionNotes = latestAssignment?.CompletionNotes,
            MediaAttachments = incident.MediaAttachments.Select(m => new IncidentMediaDto
            {
                Id = m.Id,
                MediaUrl = m.MediaUrl,
                MediaType = m.MediaType,
                CreatedAt = m.CreatedAt
            }).ToList(),
        };

        return ApiResponse<IncidentDto>.SuccessResult(dto, "Incident updated successfully.");
    }
}
