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
