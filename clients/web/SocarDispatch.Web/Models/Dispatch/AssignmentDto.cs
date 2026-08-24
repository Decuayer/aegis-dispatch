namespace SocarDispatch.Web.Models.Dispatch;

/// The assignment detail DTO returned after a successful team assignment.
public class AssignmentDto
{
    public Guid Id { get; set; }
    public Guid IncidentId { get; set; }
    public Guid TeamId { get; set; }
    public string TeamName { get; set; } = string.Empty;
    public Guid OperatorId { get; set; }
    public string OperatorFullName { get; set; } = string.Empty;
    public DateTime AssignedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? CompletionNotes { get; set; }
}
