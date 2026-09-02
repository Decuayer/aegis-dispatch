using FluentValidation;
using Microsoft.AspNetCore.Http;

namespace SocarDispatch.Application.Features.Feedbacks.Commands.CreateFeedback;

public class CreateFeedbackCommandValidator : AbstractValidator<CreateFeedbackCommand>
{
    private static readonly HashSet<string> AllowedImageTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg",
        "image/png",
        "image/webp"
    };

    private static readonly HashSet<string> AllowedVideoTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "video/mp4",
        "video/quicktime"
    };

    private const int MaxAttachmentCount = 5;
    private const long MaxImageSizeBytes = 10 * 1024 * 1024; // 10 MB
    private const long MaxVideoSizeBytes = 50 * 1024 * 1024; // 50 MB

    public CreateFeedbackCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("User ID is required.");

        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required.")
            .MaximumLength(200).WithMessage("Title must not exceed 200 characters.");

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Description is required.")
            .MaximumLength(4000).WithMessage("Description must not exceed 4000 characters.");

        RuleFor(x => x.Attachments)
            .Must(attachments => attachments == null || attachments.Count <= MaxAttachmentCount)
            .WithMessage($"A maximum of {MaxAttachmentCount} attachments are allowed per feedback report.");

        RuleForEach(x => x.Attachments)
            .NotNull().WithMessage("Attachment file cannot be null.")
            .Must(file => file != null && file.Length > 0)
            .WithMessage("Attachment file cannot be empty.")
            .Must(file => file != null && (AllowedImageTypes.Contains(file.ContentType) || AllowedVideoTypes.Contains(file.ContentType)))
            .WithMessage("Unsupported file format. Only JPEG, PNG, WEBP images and MP4, QuickTime videos are accepted.")
            .Must(file =>
            {
                if (file == null) return false;
                if (AllowedImageTypes.Contains(file.ContentType))
                {
                    return file.Length <= MaxImageSizeBytes;
                }
                if (AllowedVideoTypes.Contains(file.ContentType))
                {
                    return file.Length <= MaxVideoSizeBytes;
                }
                return false;
            })
            .WithMessage("File size exceeds the allowed quota (10MB for images, 50MB for videos).");
    }
}
