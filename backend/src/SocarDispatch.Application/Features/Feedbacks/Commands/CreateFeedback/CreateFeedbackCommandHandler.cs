using MediatR;
using Microsoft.EntityFrameworkCore;
using SocarDispatch.Application.Common.Interfaces;
using SocarDispatch.Application.Common.Models;
using SocarDispatch.Application.Features.Feedbacks.DTOs;
using SocarDispatch.Domain.Entities;
using SocarDispatch.Domain.Enums;
using SocarDispatch.Domain.Exceptions;

namespace SocarDispatch.Application.Features.Feedbacks.Commands.CreateFeedback;

public class CreateFeedbackCommandHandler : IRequestHandler<CreateFeedbackCommand, ApiResponse<FeedbackDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IMediaStorageService _mediaStorageService;

    public CreateFeedbackCommandHandler(
        IApplicationDbContext context,
        IMediaStorageService mediaStorageService)
    {
        _context = context;
        _mediaStorageService = mediaStorageService;
    }

    public async Task<ApiResponse<FeedbackDto>> Handle(CreateFeedbackCommand request, CancellationToken cancellationToken)
    {
        var user = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);

        if (user == null)
        {
            throw new EntityNotFoundException("User", request.UserId);
        }

        var feedback = new Feedback
        {
            Id = Guid.NewGuid(),
            UserId = request.UserId,
            Title = request.Title.Trim(),
            Description = request.Description.Trim(),
            Status = FeedbackStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            MediaAttachments = new List<FeedbackMedia>()
        };

        if (request.Attachments != null && request.Attachments.Count > 0)
        {
            foreach (var file in request.Attachments)
            {
                if (file == null || file.Length == 0) continue;

                using var stream = file.OpenReadStream();
                var uploadResult = await _mediaStorageService.UploadFeedbackMediaAsync(
                    stream,
                    file.FileName,
                    file.ContentType,
                    feedback.Id,
                    cancellationToken);

                feedback.MediaAttachments.Add(new FeedbackMedia
                {
                    Id = Guid.NewGuid(),
                    FeedbackId = feedback.Id,
                    MediaUrl = uploadResult.PublicUrl,
                    MediaType = file.ContentType,
                    CreatedAt = DateTime.UtcNow
                });
            }
        }

        _context.Feedbacks.Add(feedback);
        await _context.SaveChangesAsync(cancellationToken);

        var dto = new FeedbackDto
        {
            Id = feedback.Id,
            UserId = feedback.UserId,
            UserFullName = $"{user.FirstName} {user.LastName}".Trim(),
            UserEmail = user.Email,
            Title = feedback.Title,
            Description = feedback.Description,
            Status = feedback.Status,
            CreatedAt = feedback.CreatedAt,
            UpdatedAt = feedback.UpdatedAt,
            MediaAttachments = feedback.MediaAttachments.Select(m => new FeedbackMediaDto
            {
                Id = m.Id,
                FeedbackId = m.FeedbackId,
                MediaUrl = m.MediaUrl,
                MediaType = m.MediaType,
                CreatedAt = m.CreatedAt
            }).ToList()
        };

        return ApiResponse<FeedbackDto>.SuccessResult(dto, "Feedback submitted successfully.");
    }
}
