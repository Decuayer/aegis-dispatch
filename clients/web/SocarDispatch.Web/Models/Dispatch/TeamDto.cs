namespace SocarDispatch.Web.Models.Dispatch;

public class TeamDto
{
    public Guid Id { get; set; }
    public string TeamName { get; set; } = string.Empty;
    public string Status { get; set; } = "Idle"; // Idle, Forwarded, OnScene, Busy
    public Guid? LeaderId { get; set; }
    public string? LeaderFullName { get; set; }
    public decimal? CurrentLatitude { get; set; }
    public decimal? CurrentLongitude { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public List<TeamMemberDto> Members { get; set; } = new();
}

public class TeamMemberDto
{
    public Guid UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public string? SubRole { get; set; }
    public string MemberStatus { get; set; } = "Available";
    public DateTime? StatusUpdatedAt { get; set; }
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
}
