using System.ComponentModel.DataAnnotations;

namespace SocarDispatch.Web.Models.Settings;

public class UserPreferencesDto
{
    // Geospatial preferences
    [Range(-90.0, 90.0, ErrorMessage = "Latitude must be between -90 and 90.")]
    public double DefaultLatitude { get; set; } = 40.409264; // Baku / SOCAR HQ

    [Range(-180.0, 180.0, ErrorMessage = "Longitude must be between -180 and 180.")]
    public double DefaultLongitude { get; set; } = 49.867092;

    [Range(1, 18, ErrorMessage = "Zoom level must be between 1 and 18.")]
    public int DefaultZoom { get; set; } = 14;

    public string TileProvider { get; set; } = "OpenStreetMap";

    public bool LockMapToFacilityBoundary { get; set; } = false;

    // Notification preferences
    public bool EmergencyAudioAlertEnabled { get; set; } = true;

    [Range(3, 10, ErrorMessage = "Toast duration must be between 3 and 10 seconds.")]
    public int ToastDurationSeconds { get; set; } = 5;

    public UserPreferencesDto Clone()
    {
        return (UserPreferencesDto)MemberwiseClone();
    }
}
