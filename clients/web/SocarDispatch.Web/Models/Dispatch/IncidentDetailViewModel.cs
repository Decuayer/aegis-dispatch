namespace SocarDispatch.Web.Models.Dispatch;

public class IncidentDetailViewModel
{
    // Basic Event Information
    public Guid Id { get; set; }
    public string Category { get; set; } = string.Empty;
    public string EmergencyCode { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Status { get; set; } = "Open"; // Open, Assigned, Resolved, Canceled
    public decimal Latitude { get; set; }
    public decimal Longitude { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Information of the Reporting Person
    public Guid ReporterId { get; set; }
    public string ReporterFullName { get; set; } = string.Empty;
    public string? ReporterDepartment { get; set; }
    public string? ReporterPhone { get; set; }

    // Media Attachments
    public List<IncidentMediaViewModel> MediaAttachments { get; set; } = new();

    // Assigned Team Information (If the incident is assigned)
    public Guid? AssignedTeamId { get; set; }
    public string? AssignedTeamName { get; set; }

    // UI Facilitator Helper Features
    public bool IsAssigned => AssignedTeamId.HasValue || Status == "Assigned";
    public bool IsOpen => Status == "Open";
    public bool HasMedia => MediaAttachments != null && MediaAttachments.Count > 0;

    
    /// Formats the time elapsed since the event was reported (e.g., "5 min ago", "2 hours ago")
    public string TimeAgoFormatted
    {
        get
        {
            var span = DateTime.UtcNow - CreatedAt;
            if (span.TotalMinutes < 1) return "Just now";
            if (span.TotalMinutes < 60) return $"{(int)span.TotalMinutes} minutes ago";
            if (span.TotalHours < 24) return $"{(int)span.TotalHours} hours ago";
            return $"{(int)span.TotalDays} days ago";
        }
    }
}
