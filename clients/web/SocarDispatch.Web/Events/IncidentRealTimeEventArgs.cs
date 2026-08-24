// Events/IncidentRealTimeEventArgs.cs
namespace SocarDispatch.Web.Events;

// Yeni incident geldiğinde
public class NewIncidentReceivedEventArgs : EventArgs
{
    public Guid Id { get; init; }
    public string Category { get; init; } = string.Empty;
    public string EmergencyCode { get; init; } = string.Empty;
    public string? ReporterFullName { get; init; }
    public string? Description { get; init; }
    public double Latitude { get; init; }
    public double Longitude { get; init; }
    public DateTime CreatedAt { get; init; }
}

// Incident durumu değiştiğinde
public class IncidentStatusChangedEventArgs : EventArgs
{
    public Guid IncidentId { get; init; }
    public string PreviousStatus { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public Guid ChangedById { get; init; }
    public DateTime ChangedAt { get; init; }
}

// Takım atandığında
public class TeamDispatchedEventArgs : EventArgs
{
    public Guid AssignmentId { get; init; }
    public Guid IncidentId { get; init; }
    public Guid TeamId { get; init; }
    public Guid OperatorId { get; init; }
    public DateTime AssignedAt { get; init; }
}
