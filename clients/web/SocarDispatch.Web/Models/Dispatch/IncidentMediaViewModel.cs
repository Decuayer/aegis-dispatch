namespace SocarDispatch.Web.Models.Dispatch;

/// Enum specifying the event media type (compatible with Backend MediaType)
public enum IncidentMediaType
{
    Photo = 1,
    Video = 2,
    Audio = 3
}

/// View model that holds media (photo, video) information associated with the event
public class IncidentMediaViewModel
{
    public Guid Id { get; set; }
    public string MediaUrl { get; set; } = string.Empty;
    public IncidentMediaType MediaType { get; set; } = IncidentMediaType.Photo;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // UI Helper Features
    public bool IsPhoto => MediaType == IncidentMediaType.Photo;
    public bool IsVideo => MediaType == IncidentMediaType.Video;
    public bool IsAudio => MediaType == IncidentMediaType.Audio;
}
