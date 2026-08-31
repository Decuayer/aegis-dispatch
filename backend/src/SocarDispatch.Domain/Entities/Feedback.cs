using SocarDispatch.Domain.Enums;

namespace SocarDispatch.Domain.Entities;

public class Feedback
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public FeedbackStatus Status { get; set; } = FeedbackStatus.Pending;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Navigation Properties
    public ICollection<FeedbackMedia> MediaAttachments { get; set; } = new List<FeedbackMedia>();
}
