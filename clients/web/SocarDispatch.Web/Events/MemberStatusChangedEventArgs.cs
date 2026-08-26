namespace SocarDispatch.Web.Events;

public class MemberStatusChangedEventArgs : EventArgs
{
    public Guid TeamId { get; init; }
    public Guid UserId { get; init; }
    public string PreviousStatus { get; init; } = string.Empty;
    public string NewStatus { get; init; } = string.Empty;
    public Guid ChangedById { get; init; }
    public DateTime ChangedAt { get; init; }
}
