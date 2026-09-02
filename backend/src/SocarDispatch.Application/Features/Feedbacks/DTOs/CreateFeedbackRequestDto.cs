using Microsoft.AspNetCore.Http;

namespace SocarDispatch.Application.Features.Feedbacks.DTOs;

public class CreateFeedbackRequestDto
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<IFormFile>? Attachments { get; set; }
}
