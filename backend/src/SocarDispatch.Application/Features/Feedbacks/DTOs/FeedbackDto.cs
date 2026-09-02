using SocarDispatch.Domain.Enums;

namespace SocarDispatch.Application.Features.Feedbacks.DTOs;

public class FeedbackDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string UserFullName { get; set; } = string.Empty;
    public string UserEmail { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public FeedbackStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public List<FeedbackMediaDto> MediaAttachments { get; set; } = new();
}
