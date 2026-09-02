using MediatR;
using Microsoft.EntityFrameworkCore;
using SocarDispatch.Application.Common.Interfaces;
using SocarDispatch.Application.Common.Models;
using SocarDispatch.Application.Features.Incidents.DTOs;
using SocarDispatch.Application.Features.Reports.DTOs;
using SocarDispatch.Domain.Exceptions;

namespace SocarDispatch.Application.Features.Incidents.Queries.GetIncidentById;

public class GetIncidentByIdQueryHandler : IRequestHandler<GetIncidentByIdQuery, ApiResponse<IncidentDto>>
{
    private readonly IApplicationDbContext _context;

    public GetIncidentByIdQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ApiResponse<IncidentDto>> Handle(GetIncidentByIdQuery request, CancellationToken cancellationToken)
    {
        var incident = await _context.Incidents
            .Include(i => i.Reporter)
            .Include(i => i.Assignments)
                .ThenInclude(a => a.Team)
                    .ThenInclude(t => t.Leader)
            .Include(i => i.Assignments)
                .ThenInclude(a => a.Team)
                    .ThenInclude(t => t.Members)
            .Include(i => i.MediaAttachments)
            .Include(i => i.Reports)
                .ThenInclude(r => r.Team)
            .Include(i => i.Reports)
                .ThenInclude(r => r.ReportedBy)
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.Id == request.Id, cancellationToken);

        if (incident == null)
        {
            throw new EntityNotFoundException("Incident", request.Id);
        }

        var latestAssignment = incident.Assignments.OrderByDescending(a => a.AssignedAt).FirstOrDefault();
        var assignedTeam = incident.Status == SocarDispatch.Domain.Enums.IncidentStatus.Open ? null : latestAssignment?.Team;
        var leader = assignedTeam?.Leader;

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
            IsDeleted = incident.IsDeleted,
            DeletedAt = incident.DeletedAt,
            AssignedAt = incident.Status == SocarDispatch.Domain.Enums.IncidentStatus.Open ? null : latestAssignment?.AssignedAt,
            CompletedAt = incident.Status == SocarDispatch.Domain.Enums.IncidentStatus.Open ? null : latestAssignment?.CompletedAt,
            AssignedTeamId = incident.Status == SocarDispatch.Domain.Enums.IncidentStatus.Open ? null : latestAssignment?.TeamId,
            AssignedTeamName = assignedTeam?.TeamName,
            AssignedTeamLeaderName = leader != null ? $"{leader.FirstName} {leader.LastName}".Trim() : null,
            AssignedTeamLeaderPhone = leader?.Phone,
            AssignedTeamStatus = assignedTeam?.Status.ToString(),
            AssignedTeamMemberCount = assignedTeam?.Members.Count,
            CompletionNotes = (incident.Status == SocarDispatch.Domain.Enums.IncidentStatus.Resolved || incident.Status == SocarDispatch.Domain.Enums.IncidentStatus.Canceled)
                ? latestAssignment?.CompletionNotes
                : null,
            Reports = incident.Reports.OrderByDescending(r => r.ReportedAt).Select(r => new IncidentReportDto
            {
                Id = r.Id,
                IncidentId = r.IncidentId,
                TeamId = r.TeamId,
                TeamName = r.Team != null ? r.Team.TeamName : string.Empty,
                ReportedByUserId = r.ReportedByUserId,
                ReportedByFullName = r.ReportedBy != null ? $"{r.ReportedBy.FirstName} {r.ReportedBy.LastName}".Trim() : string.Empty,
                Content = r.Content,
                MediaUrl = r.MediaUrl,
                ReportedAt = r.ReportedAt
            }).ToList()
        };

        return ApiResponse<IncidentDto>.SuccessResult(dto, "Incident details retrieved.");
    }
}
