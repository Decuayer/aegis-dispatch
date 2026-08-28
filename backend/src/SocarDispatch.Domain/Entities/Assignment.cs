namespace SocarDispatch.Domain.Entities;

public class Assignment
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid IncidentId { get; set; }
    public Incident Incident { get; set; } = null!;

    public Guid TeamId { get; set; }
    public Team Team { get; set; } = null!;

    public Guid OperatorId { get; set; }
    public User Operator { get; set; } = null!;

    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;

    public DateTime? CompletedAt { get; set; }

    public string? CompletionNotes { get; set; }

}