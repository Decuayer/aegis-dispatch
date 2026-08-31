namespace SocarDispatch.Domain.Entities;

public class FeedbackMedia
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid FeedbackId { get; set; }
    public Feedback Feedback { get; set; } = null!;

    public string MediaUrl { get; set; } = string.Empty;
    public string MediaType { get; set; } = string.Empty; // e.g., "image/jpeg", "video/mp4"
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
