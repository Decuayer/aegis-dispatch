// Models/Map/MapTeamDto.cs
namespace SocarDispatch.Web.Models.Map;

public class MapTeamDto
{
    public Guid Id { get; set; }
    public string TeamName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty; // Idle | Forwarded | OnScene | Busy
    public decimal? CurrentLatitude { get; set; }
    public decimal? CurrentLongitude { get; set; }
    public DateTime UpdatedAt { get; set; }
}
