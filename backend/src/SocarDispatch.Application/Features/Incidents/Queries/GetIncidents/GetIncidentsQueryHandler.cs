using MediatR;
using Microsoft.EntityFrameworkCore;
using SocarDispatch.Application.Common.Interfaces;
using SocarDispatch.Application.Common.Models;
using SocarDispatch.Application.Features.Incidents.DTOs;
using SocarDispatch.Domain.Enums;

namespace SocarDispatch.Application.Features.Incidents.Queries.GetIncidents;

public class GetIncidentsQueryHandler : IRequestHandler<GetIncidentsQuery, ApiResponse<List<IncidentDto>>>
{
    private readonly IApplicationDbContext _context;

    public GetIncidentsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ApiResponse<List<IncidentDto>>> Handle(GetIncidentsQuery request, CancellationToken cancellationToken)
    {
        var query = _context.Incidents
            .Include(i => i.Reporter)
            .Include(i => i.Assignments)
                .ThenInclude(a => a.Team)
            .Include(i => i.MediaAttachments) 
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Status) && !request.Status.Equals("All", StringComparison.OrdinalIgnoreCase))
        {
            if (request.Status.Equals("Active", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(i => i.Status == IncidentStatus.Open || i.Status == IncidentStatus.Assigned);
            }
            else if (request.Status.Contains(','))
            {
                var statusList = request.Status.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Select(s => Enum.TryParse<IncidentStatus>(s, true, out var parsed) ? (IncidentStatus?)parsed : null)
                    .Where(s => s.HasValue)
                    .Select(s => s!.Value)
                    .ToList();

                if (statusList.Count > 0)
                {
                    query = query.Where(i => statusList.Contains(i.Status));
                }
            }
            else if (Enum.TryParse<IncidentStatus>(request.Status, true, out var parsedStatus))
            {
                query = query.Where(i => i.Status == parsedStatus);
            }
        }

        if (!string.IsNullOrWhiteSpace(request.Category) && !request.Category.Equals("All", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(i => i.Category.ToLower() == request.Category.ToLower());
        }

        // Apply temporal time-range boundaries
        if (request.From.HasValue)
        {
            query = query.Where(i => i.CreatedAt >= request.From.Value);
        }

        if (request.To.HasValue)
        {
            query = query.Where(i => i.CreatedAt <= request.To.Value);
        }

        var list = await query
            .OrderByDescending(i => i.CreatedAt)
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
                AssignedAt = i.Assignments.OrderBy(a => a.AssignedAt).Select(a => (DateTime?)a.AssignedAt).FirstOrDefault(),
                CompletedAt = i.Assignments.OrderByDescending(a => a.AssignedAt).Select(a => a.CompletedAt).FirstOrDefault(),
                AssignedTeamId = i.Assignments.OrderByDescending(a => a.AssignedAt).Select(a => (Guid?)a.TeamId).FirstOrDefault(),
                AssignedTeamName = i.Assignments.OrderByDescending(a => a.AssignedAt).Select(a => a.Team.TeamName).FirstOrDefault(),
                CompletionNotes = i.Assignments.OrderByDescending(a => a.AssignedAt).Select(a => a.CompletionNotes).FirstOrDefault()
            })
            .ToListAsync(cancellationToken);

        return ApiResponse<List<IncidentDto>>.SuccessResult(list, "Incidents retrieved successfully.");
    }
}
