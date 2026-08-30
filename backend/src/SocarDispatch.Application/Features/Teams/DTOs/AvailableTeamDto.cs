namespace SocarDispatch.Application.Features.Teams.DTOs;

public class AvailableTeamDto
{
    public Guid Id { get; set; }
    public string TeamName { get; set; } = string.Empty;
    public Guid? LeaderId { get; set; }
    public string? LeaderFullName { get; set; }
    public int MemberCount { get; set; }
    public DateTime CreatedAt { get; set; }
}
