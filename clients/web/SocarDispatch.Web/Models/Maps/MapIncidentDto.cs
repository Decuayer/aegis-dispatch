// Models/Map/MapIncidentDto.cs
namespace SocarDispatch.Web.Models.Map;

public class MapIncidentDto
{
    public Guid Id { get; set; }
    public string Category { get; set; } = string.Empty;
    public string EmergencyCode { get; set; } = string.Empty;
    public string ReporterFullName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public decimal Latitude { get; set; }
    public decimal Longitude { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? AssignedAt { get; set; }
    public string? AssignedTeamName { get; set; }
}
