namespace SocarDispatch.Application.Features.Incidents.DTOs;

public class IncidentDto
{
    public Guid Id { get; set; }
    public Guid ReporterId { get; set; }
    public string ReporterFullName { get; set; } = string.Empty;
    // Detailed Reporter Information
    public string ReporterPhone { get; set; } = string.Empty;
    public string ReporterDepartment { get; set; } = string.Empty;
    public string ReporterEmail { get; set; } = string.Empty;

    public string Category { get; set; } = string.Empty;
    public string EmergencyCode { get; set; } = string.Empty;
    public string? Description { get; set; }
    public List<IncidentMediaDto> MediaAttachments { get; set; } = new();
    public string Status { get; set; } = string.Empty;
    public decimal Latitude { get; set; }
    public decimal Longitude { get; set; }
    public DateTime CreatedAt { get; set; }
    
    // Assigned Team Information (if any)
    public Guid? AssignedTeamId { get; set; }
    public string? AssignedTeamName { get; set; }
    public string? CompletionNotes { get; set; }
    public List<IncidentReportDto> Reports { get; set; } = new();
}

public class IncidentReportDto
{
    public Guid Id { get; set; }
    public Guid IncidentId { get; set; }
    public Guid TeamId { get; set; }
    public string TeamName { get; set; } = string.Empty;
    public Guid ReportedByUserId { get; set; }
    public string ReportedByFullName { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string? MediaUrl { get; set; }
    public DateTime ReportedAt { get; set; }
}