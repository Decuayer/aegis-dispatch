using SocarDispatch.Domain.Enums;

namespace SocarDispatch.Domain.Entities;

public class IncidentMedia
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid IncidentId { get; set; }
    public Incident Incident { get; set; } = null!;

    public string MediaUrl { get; set; } = string.Empty;
    public MediaType MediaType { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
