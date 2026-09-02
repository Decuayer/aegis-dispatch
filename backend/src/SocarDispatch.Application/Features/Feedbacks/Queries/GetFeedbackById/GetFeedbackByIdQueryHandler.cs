using MediatR;
using Microsoft.EntityFrameworkCore;
using SocarDispatch.Application.Common.Interfaces;
using SocarDispatch.Application.Common.Models;
using SocarDispatch.Application.Features.Feedbacks.DTOs;
using SocarDispatch.Domain.Exceptions;

namespace SocarDispatch.Application.Features.Feedbacks.Queries.GetFeedbackById;

public class GetFeedbackByIdQueryHandler : IRequestHandler<GetFeedbackByIdQuery, ApiResponse<FeedbackDto>>
{
    private readonly IApplicationDbContext _context;

    public GetFeedbackByIdQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ApiResponse<FeedbackDto>> Handle(GetFeedbackByIdQuery request, CancellationToken cancellationToken)
    {
        var feedback = await _context.Feedbacks
            .AsNoTracking()
            .Include(f => f.User)
            .Include(f => f.MediaAttachments)
            .FirstOrDefaultAsync(f => f.Id == request.Id, cancellationToken);

        if (feedback == null)
        {
            throw new EntityNotFoundException("Feedback", request.Id);
        }

        var isOperator = string.Equals(request.RequestingUserRole, "Operator", StringComparison.OrdinalIgnoreCase);
        if (!isOperator && feedback.UserId != request.RequestingUserId)
        {
            throw new ForbiddenAccessException("You are not authorized to view this feedback report.");
        }

        var dto = new FeedbackDto
        {
            Id = feedback.Id,
            UserId = feedback.UserId,
            UserFullName = $"{feedback.User.FirstName} {feedback.User.LastName}".Trim(),
            UserEmail = feedback.User.Email,
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

        return ApiResponse<FeedbackDto>.SuccessResult(dto, "Feedback details retrieved successfully.");
    }
}
