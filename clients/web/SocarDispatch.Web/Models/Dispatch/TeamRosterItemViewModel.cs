namespace SocarDispatch.Web.Models.Dispatch;

/// View model representing an emergency response team in the operator dispatch roster.
/// Includes availability status, leader info, member count, and calculated distance.
public class TeamRosterItemViewModel
{
    /// Unique identifier of the response team.
    public Guid Id { get; set; }

    /// Name of the team (e.g., "Fire Rescue Alpha", "Medical Team 1").
    public string TeamName { get; set; } = string.Empty;

    /// Current operational status ("Idle", "Forwarded", "OnScene", "Busy").
    public string Status { get; set; } = "Idle";

    /// Unique identifier of the assigned team leader.
    public Guid? LeaderId { get; set; }

    /// Full name of the team leader.
    public string? LeaderFullName { get; set; }

    /// Total number of active members in the team.
    public int MembersCount { get; set; }

    /// Last known GPS Latitude coordinate.
    public decimal? CurrentLatitude { get; set; }

    /// Last known GPS Longitude coordinate.
    public decimal? CurrentLongitude { get; set; }

    /// Timestamp of the last status or location update.
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// Calculated straight-line distance in meters from the selected incident coordinates.
    public double? DistanceInMeters { get; set; }

    /// Indicates whether the team is available and ready for dispatch.
    /// Only teams in "Idle" status are dispatchable.
    public bool IsDispatchable => string.Equals(Status, "Idle", StringComparison.OrdinalIgnoreCase);

    /// Formatted human-readable distance (e.g., "450 m", "2.4 km", or "Location unavailable").
    public string DistanceFormatted
    {
        get
        {
            if (!DistanceInMeters.HasValue)
            {
                return "Location unavailable";
            }

            if (DistanceInMeters.Value < 1000)
            {
                return $"{Math.Round(DistanceInMeters.Value)} m";
            }

            var km = DistanceInMeters.Value / 1000.0;
            return $"{km:F1} km";
        }
    }

    /// Helper property returning appropriate CSS badge class based on status.
    public string StatusBadgeClass => Status.ToLowerInvariant() switch
    {
        "idle" => "badge-status-idle",
        "forwarded" => "badge-status-forwarded",
        "onscene" => "badge-status-onscene",
        "busy" => "badge-status-busy",
        _ => "badge-status-default"
    };
}
