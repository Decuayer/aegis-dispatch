namespace SocarDispatch.Application.Features.Feedbacks.DTOs;

public class FeedbackMediaDto
{
    public Guid Id { get; set; }
    public Guid FeedbackId { get; set; }
    public string MediaUrl { get; set; } = string.Empty;
    public string MediaType { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
