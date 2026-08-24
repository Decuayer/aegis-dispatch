// Events/TeamLocationRealTimeEventArgs.cs
namespace SocarDispatch.Web.Events;

// Team GPS konum güncellendiğinde
public class TeamLocationUpdatedEventArgs : EventArgs
{
    public Guid TeamId { get; init; }
    public double Latitude { get; init; }
    public double Longitude { get; init; }
    public DateTime UpdatedAt { get; init; }
}

// Team/member durumu değiştiğinde
public class TeamStatusChangedEventArgs : EventArgs
{
    public Guid TeamId { get; init; }
    public Guid UserId { get; init; }
    public string PreviousStatus { get; init; } = string.Empty;
    public string NewStatus { get; init; } = string.Empty;
    public Guid ChangedById { get; init; }
    public DateTime ChangedAt { get; init; }
}
