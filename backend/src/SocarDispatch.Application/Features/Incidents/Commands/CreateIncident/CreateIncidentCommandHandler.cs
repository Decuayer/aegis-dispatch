using MediatR;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using SocarDispatch.Application.Common.Interfaces;
using SocarDispatch.Application.Common.Models;
using SocarDispatch.Application.Features.Incidents.DTOs;
using SocarDispatch.Domain.Entities;
using SocarDispatch.Domain.Enums;
using SocarDispatch.Domain.Exceptions;
using SocarDispatch.Domain.Events;


namespace SocarDispatch.Application.Features.Incidents.Commands.CreateIncident;

public class CreateIncidentCommandHandler : IRequestHandler<CreateIncidentCommand, ApiResponse<IncidentDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IPublisher _publisher;

    public CreateIncidentCommandHandler(IApplicationDbContext context, IPublisher publisher)
    {
        _context = context;
        _publisher = publisher;
    }

    public async Task<ApiResponse<IncidentDto>> Handle(CreateIncidentCommand request, CancellationToken cancellationToken)
    {
        var reporter = await _context.Users.FindAsync(new object[] { request.ReporterId }, cancellationToken);
        if (reporter == null)
        {
            throw new EntityNotFoundException("User", request.ReporterId);
        }

        // EmergencyCode validation check against DB
        var emergencyCodeDef = await _context.EmergencyCodes
            .FirstOrDefaultAsync(c => c.Code.ToLower() == request.EmergencyCode.ToLower() && c.IsActive, cancellationToken);
        if (emergencyCodeDef == null)
        {
            throw new DomainException($"Invalid emergency code: '{request.EmergencyCode}'");
        }

        var incidentCategoryDef = await _context.IncidentCategories
            .FirstOrDefaultAsync(c => c.Code.ToLower() == request.Category.ToLower() && c.IsActive, cancellationToken);
        if (incidentCategoryDef == null)
        {
            throw new DomainException($"Invalid incident category: '{request.Category}'");
        }

        var incident = new Incident
        {
            ReporterId = request.ReporterId,
            Category = incidentCategoryDef.Code,
            EmergencyCode = emergencyCodeDef.Code,
            Description = request.Description,
            Status = IncidentStatus.Open,
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            Location = new Point((double)request.Longitude, (double)request.Latitude) { SRID = 4326 },
            CreatedAt = DateTime.UtcNow,
            MediaAttachments = request.MediaAttachments.Select(m => new IncidentMedia
            {
                MediaUrl = m.MediaUrl,
                MediaType = m.MediaType,
                CreatedAt = DateTime.UtcNow
            }).ToList()
        };

        _context.Incidents.Add(incident);
        await _context.SaveChangesAsync(cancellationToken);

        var reporterFullName = $"{reporter.FirstName} {reporter.LastName}".Trim();

        await _publisher.Publish(new IncidentCreatedEvent(
            incident.Id,
            incident.ReporterId,
            reporterFullName,
            incident.Category,
            incident.EmergencyCode,
            incident.Description ?? string.Empty,
            (double)incident.Latitude,
            (double)incident.Longitude,
            incident.CreatedAt
        ), cancellationToken);

        var dto = new IncidentDto
        {
            Id = incident.Id,
            ReporterId = reporter.Id,
            ReporterFullName = reporterFullName,
            ReporterPhone = reporter.Phone ?? string.Empty,
            ReporterDepartment = reporter.Department ?? string.Empty,
            ReporterEmail = reporter.Email ?? string.Empty,
            ReporterSubRole = reporter.SubRole ?? string.Empty,
            ReporterAvatarUrl = reporter.AvatarUrl ?? string.Empty,
            Category = incident.Category,
            EmergencyCode = incident.EmergencyCode,
            Description = incident.Description,
            Status = incident.Status.ToString(),
            Latitude = incident.Latitude,
            Longitude = incident.Longitude,
            CreatedAt = incident.CreatedAt,
            MediaAttachments = incident.MediaAttachments.Select(m => new IncidentMediaDto
            {
                Id = m.Id,
                MediaUrl = m.MediaUrl,
                MediaType = m.MediaType,
                CreatedAt = m.CreatedAt
            }).ToList()
        };

        return ApiResponse<IncidentDto>.SuccessResult(dto, "Incident successfully reported.");
    }
}
