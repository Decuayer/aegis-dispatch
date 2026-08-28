using SocarDispatch.Application.Features.Reports.DTOs;

namespace SocarDispatch.Application.Features.Incidents.DTOs;

public class IncidentDto
{
    public Guid Id { get; set; }
    public Guid ReporterId { get; set; }
    public string ReporterFullName { get; set; } = string.Empty;
    public string ReporterPhone { get; set; } = string.Empty;
    public string ReporterDepartment { get; set; } = string.Empty;
    public string ReporterEmail { get; set; } = string.Empty;
    public string? ReporterSubRole { get; set; }
    public string? ReporterAvatarUrl { get; set; }

    public string Category { get; set; } = string.Empty;
    public string EmergencyCode { get; set; } = string.Empty;
    public string? Description { get; set; }
    public List<IncidentMediaDto> MediaAttachments { get; set; } = new();
    public string Status { get; set; } = string.Empty;
    public decimal Latitude { get; set; }
    public decimal Longitude { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? AssignedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    
    // Assigned Team Information
    public Guid? AssignedTeamId { get; set; }
    public string? AssignedTeamName { get; set; }
    public string? CompletionNotes { get; set; }
    public List<IncidentReportDto> Reports { get; set; } = new();
}

