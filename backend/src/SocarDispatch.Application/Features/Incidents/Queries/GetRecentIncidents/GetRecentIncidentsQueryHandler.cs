using MediatR;
using Microsoft.EntityFrameworkCore;
using SocarDispatch.Application.Common.Interfaces;
using SocarDispatch.Application.Common.Models;
using SocarDispatch.Application.Features.Incidents.DTOs;
using SocarDispatch.Domain.Enums;

namespace SocarDispatch.Application.Features.Incidents.Queries.GetRecentIncidents;

public class GetRecentIncidentsQueryHandler : IRequestHandler<GetRecentIncidentsQuery, ApiResponse<IReadOnlyList<IncidentDto>>>
{
    private readonly IApplicationDbContext _context;

    public GetRecentIncidentsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ApiResponse<IReadOnlyList<IncidentDto>>> Handle(GetRecentIncidentsQuery request, CancellationToken cancellationToken)
    {
        var clampedLimit = Math.Clamp(request.Limit, 1, 50);

        var items = await _context.Incidents
            .Include(i => i.Reporter)
            .Include(i => i.Assignments)
                .ThenInclude(a => a.Team)
                    .ThenInclude(t => t.Leader)
            .Include(i => i.Assignments)
                .ThenInclude(a => a.Team)
                    .ThenInclude(t => t.Members)
            .Include(i => i.MediaAttachments)
            .AsNoTracking()
            .Where(i => i.Status == IncidentStatus.Open || i.Status == IncidentStatus.Assigned)
            .OrderByDescending(i => i.CreatedAt)
            .Take(clampedLimit)
            .Select(i => new IncidentDto
            {
                Id = i.Id,
                ReporterId = i.ReporterId,
                ReporterFullName = $"{i.Reporter.FirstName} {i.Reporter.LastName}".Trim(),
                ReporterPhone = i.Reporter.Phone ?? string.Empty,
                ReporterDepartment = i.Reporter.Department ?? string.Empty,
                ReporterEmail = i.Reporter.Email ?? string.Empty,
                ReporterSubRole = i.Reporter.SubRole ?? string.Empty,
                ReporterAvatarUrl = i.Reporter.AvatarUrl ?? string.Empty,
                Category = i.Category,
                EmergencyCode = i.EmergencyCode,
                Description = i.Description,
                MediaAttachments = i.MediaAttachments.Select(m => new IncidentMediaDto
                {
                    Id = m.Id,
                    MediaUrl = m.MediaUrl,
                    MediaType = m.MediaType,
                    CreatedAt = m.CreatedAt
                }).ToList(),
                Status = i.Status.ToString(),
                Latitude = i.Latitude,
                Longitude = i.Longitude,
                CreatedAt = i.CreatedAt,
                IsDeleted = i.IsDeleted,
                DeletedAt = i.DeletedAt,
                AssignedAt = i.Status == IncidentStatus.Open ? null : i.Assignments.OrderBy(a => a.AssignedAt).Select(a => (DateTime?)a.AssignedAt).FirstOrDefault(),
                CompletedAt = i.Status == IncidentStatus.Open ? null : i.Assignments.OrderByDescending(a => a.AssignedAt).Select(a => a.CompletedAt).FirstOrDefault(),
                AssignedTeamId = i.Status == IncidentStatus.Open ? null : i.Assignments.OrderByDescending(a => a.AssignedAt).Select(a => (Guid?)a.TeamId).FirstOrDefault(),
                AssignedTeamName = i.Status == IncidentStatus.Open ? null : i.Assignments.OrderByDescending(a => a.AssignedAt).Select(a => a.Team.TeamName).FirstOrDefault(),
                AssignedTeamLeaderName = i.Status == IncidentStatus.Open ? null : i.Assignments.OrderByDescending(a => a.AssignedAt).Select(a => a.Team.Leader != null ? (a.Team.Leader.FirstName + " " + a.Team.Leader.LastName).Trim() : null).FirstOrDefault(),
                AssignedTeamLeaderPhone = i.Status == IncidentStatus.Open ? null : i.Assignments.OrderByDescending(a => a.AssignedAt).Select(a => a.Team.Leader != null ? a.Team.Leader.Phone : null).FirstOrDefault(),
                AssignedTeamStatus = i.Status == IncidentStatus.Open ? null : i.Assignments.OrderByDescending(a => a.AssignedAt).Select(a => a.Team.Status.ToString()).FirstOrDefault(),
                AssignedTeamMemberCount = i.Status == IncidentStatus.Open ? null : i.Assignments.OrderByDescending(a => a.AssignedAt).Select(a => (int?)a.Team.Members.Count).FirstOrDefault(),
                CompletionNotes = (i.Status == IncidentStatus.Resolved || i.Status == IncidentStatus.Canceled)
                    ? i.Assignments.OrderByDescending(a => a.AssignedAt).Select(a => a.CompletionNotes).FirstOrDefault()
                    : null
            })
            .ToListAsync(cancellationToken);

        return ApiResponse<IReadOnlyList<IncidentDto>>.SuccessResult(items, "Recent incidents retrieved successfully.");
    }
}
