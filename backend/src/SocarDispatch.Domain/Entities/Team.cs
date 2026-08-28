using NetTopologySuite.Geometries;
using SocarDispatch.Domain.Enums;


namespace SocarDispatch.Domain.Entities;

public class Team
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string TeamName { get; set; } = string.Empty;
    public TeamStatus Status { get; set; } = TeamStatus.Idle;


    // Team Leader (The main user publishing their location)
    public Guid? LeaderId { get; set; }
    public User? Leader { get; set; }
    public decimal? CurrentLatitude { get; set; }
    public decimal? CurrentLongitude { get; set; }

    // PostGIS Spatial Point (SRID 4326 - WGS84 GPS Coordinates)
    public Point? Location { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation Properties
    public ICollection<TeamMember> Members { get; set; } = new List<TeamMember>();
    public ICollection<Assignment> Assignments { get; set; } = new List<Assignment>();
    public ICollection<IncidentReport> Reports { get; set; } = new List<IncidentReport>();
}
