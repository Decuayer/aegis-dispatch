using MediatR;
using Microsoft.EntityFrameworkCore;
using SocarDispatch.Application.Common.Extensions;
using SocarDispatch.Application.Common.Interfaces;
using SocarDispatch.Application.Common.Models;
using SocarDispatch.Application.Features.Incidents.DTOs;
using SocarDispatch.Domain.Enums;

namespace SocarDispatch.Application.Features.Incidents.Queries.GetIncidents;

public class GetIncidentsQueryHandler :
    IRequestHandler<GetIncidentsQuery, ApiResponse<PagedResult<IncidentDto>>>,
    IRequestHandler<GetIncidentsWithFiltersQuery, ApiResponse<PagedResult<IncidentDto>>>
{
    private readonly IApplicationDbContext _context;

    public GetIncidentsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ApiResponse<PagedResult<IncidentDto>>> Handle(GetIncidentsQuery request, CancellationToken cancellationToken)
    {
        var query = _context.Incidents
            .Include(i => i.Reporter)
            .Include(i => i.Assignments)
                .ThenInclude(a => a.Team)
                    .ThenInclude(t => t.Leader)
            .Include(i => i.Assignments)
                .ThenInclude(a => a.Team)
                    .ThenInclude(t => t.Members)
            .Include(i => i.MediaAttachments)
            .AsNoTracking()
            .AsQueryable();

        // Exact IncidentId filter
        if (request.IncidentId.HasValue)
        {
            query = query.Where(i => i.Id == request.IncidentId.Value);
        }

        // ReporterId filter
        if (request.ReporterId.HasValue)
        {
            query = query.Where(i => i.ReporterId == request.ReporterId.Value);
        }

        // Multi-field text search
        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var searchLower = request.SearchTerm.Trim().ToLowerInvariant();

            // Strip #INC- or INC- or # prefix if present for ID search
            var idSearchTerm = searchLower;
            if (idSearchTerm.StartsWith("#inc-", StringComparison.OrdinalIgnoreCase))
                idSearchTerm = idSearchTerm.Substring(5).Trim();
            else if (idSearchTerm.StartsWith("inc-", StringComparison.OrdinalIgnoreCase))
                idSearchTerm = idSearchTerm.Substring(4).Trim();
            else if (idSearchTerm.StartsWith("#"))
                idSearchTerm = idSearchTerm.Substring(1).Trim();

            if (string.IsNullOrEmpty(idSearchTerm))
                idSearchTerm = searchLower;

            query = query.Where(i =>
                i.Id.ToString().ToLower().Contains(idSearchTerm) ||
                i.Category.ToLower().Contains(searchLower) ||
                i.EmergencyCode.ToLower().Contains(searchLower) ||
                (i.Description != null && i.Description.ToLower().Contains(searchLower)));
        }

        // Status filter
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

        // Category filter
        if (!string.IsNullOrWhiteSpace(request.Category) && !request.Category.Equals("All", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(i => i.Category.ToLower() == request.Category.ToLower());
        }

        // Emergency code filter
        if (!string.IsNullOrWhiteSpace(request.EmergencyCode) && !request.EmergencyCode.Equals("All", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(i => i.EmergencyCode.ToLower() == request.EmergencyCode.ToLower());
        }

        // Temporal boundary filters
        if (request.FromDate.HasValue)
        {
            query = query.Where(i => i.CreatedAt >= request.FromDate.Value);
        }

        if (request.ToDate.HasValue)
        {
            query = query.Where(i => i.CreatedAt <= request.ToDate.Value);
        }

        // Dynamic ordering (SDDC-50)
        var sortBy = (request.SortBy ?? "createdAt").Trim().ToLowerInvariant();
        var sortDir = (request.SortDir ?? "desc").Trim().ToLowerInvariant();
        var isAsc = sortDir == "asc";

        query = sortBy switch
        {
            "createdat" => isAsc ? query.OrderBy(i => i.CreatedAt) : query.OrderByDescending(i => i.CreatedAt),
            "status" => isAsc
                ? query.OrderBy(i => i.Status).ThenByDescending(i => i.CreatedAt)
                : query.OrderByDescending(i => i.Status).ThenByDescending(i => i.CreatedAt),
            "category" => isAsc
                ? query.OrderBy(i => i.Category).ThenByDescending(i => i.CreatedAt)
                : query.OrderByDescending(i => i.Category).ThenByDescending(i => i.CreatedAt),
            "emergencycode" => isAsc
                ? query.OrderBy(i => i.EmergencyCode).ThenByDescending(i => i.CreatedAt)
                : query.OrderByDescending(i => i.EmergencyCode).ThenByDescending(i => i.CreatedAt),
            _ => query.OrderByDescending(i => i.CreatedAt)
        };

        // DTO projection
        var projectedQuery = query.Select(i => new IncidentDto
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
        });

        // Execute server-side pagination
        var pagedResult = await projectedQuery.ToPagedResultAsync(
            request.PageNumber,
            request.PageSize,
            cancellationToken);

        return ApiResponse<PagedResult<IncidentDto>>.SuccessResult(
            pagedResult,
            "Incidents retrieved successfully.");
    }

    public async Task<ApiResponse<PagedResult<IncidentDto>>> Handle(GetIncidentsWithFiltersQuery request, CancellationToken cancellationToken)
    {
        return await Handle((GetIncidentsQuery)request, cancellationToken);
    }
}
