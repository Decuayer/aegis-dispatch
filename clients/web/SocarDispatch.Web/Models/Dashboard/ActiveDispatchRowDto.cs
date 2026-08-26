namespace SocarDispatch.Web.Models.Dashboard;

public class ActiveDispatchRowDto
{
    // Incident Metadata
    public Guid IncidentId { get; set; }
    public string Category { get; set; } = string.Empty;
    public string EmergencyCode { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Status { get; set; } = "Open"; // "Open" or "Assigned"
    public decimal Latitude { get; set; }
    public decimal Longitude { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? AssignedAt { get; set; }
    public string ReporterFullName { get; set; } = string.Empty;

    // Assigned Team Metadata
    public Guid? AssignedTeamId { get; set; }
    public string? AssignedTeamName { get; set; }
    public string? TeamLeaderName { get; set; }
    public string? TeamStatus { get; set; } // "Idle", "Forwarded", "OnScene", "Busy"
    public int TeamMemberCount { get; set; }

    // Helpers
    public bool IsAssigned => AssignedTeamId.HasValue || Status == "Assigned";
    
    public string ElapsedTimeFormatted
    {
        get
        {
            var span = DateTime.UtcNow - CreatedAt;
            if (span.TotalMinutes < 1) return "< 1 min";
            if (span.TotalMinutes < 60) return $"{(int)span.TotalMinutes}m ago";
            if (span.TotalHours < 24) return $"{(int)span.TotalHours}h {span.Minutes}m ago";
            return $"{(int)span.TotalDays}d ago";
        }
    }

    public int SeverityRank => (EmergencyCode ?? string.Empty).ToLowerInvariant() switch
    {
        var c when c.Contains("red") || c.Contains("kirmizi") || c.Contains("1") => 1,
        var c when c.Contains("orange") || c.Contains("turuncu") || c.Contains("2") => 2,
        var c when c.Contains("yellow") || c.Contains("sari") || c.Contains("3") => 3,
        var c when c.Contains("green") || c.Contains("yesil") || c.Contains("4") => 4,
        _ => 5
    };
}