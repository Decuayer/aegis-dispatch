using MediatR;
using Microsoft.EntityFrameworkCore;
using SocarDispatch.Application.Common.Extensions;
using SocarDispatch.Application.Common.Interfaces;
using SocarDispatch.Application.Common.Models;
using SocarDispatch.Application.Features.Feedbacks.DTOs;

namespace SocarDispatch.Application.Features.Feedbacks.Queries.GetFeedbacks;

public class GetFeedbacksQueryHandler : IRequestHandler<GetFeedbacksQuery, ApiResponse<PagedResult<FeedbackDto>>>
{
    private readonly IApplicationDbContext _context;

    public GetFeedbacksQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ApiResponse<PagedResult<FeedbackDto>>> Handle(GetFeedbacksQuery request, CancellationToken cancellationToken)
    {
        var query = _context.Feedbacks
            .AsNoTracking()
            .Include(f => f.User)
            .Include(f => f.MediaAttachments)
            .AsQueryable();

        if (request.Status.HasValue)
        {
            query = query.Where(f => f.Status == request.Status.Value);
        }

        if (request.FromDate.HasValue)
        {
            query = query.Where(f => f.CreatedAt >= request.FromDate.Value);
        }

        if (request.ToDate.HasValue)
        {
            query = query.Where(f => f.CreatedAt <= request.ToDate.Value);
        }

        if (request.UserId.HasValue)
        {
            query = query.Where(f => f.UserId == request.UserId.Value);
        }

        query = query.OrderByDescending(f => f.CreatedAt);

        var projectedQuery = query.Select(f => new FeedbackDto
        {
            Id = f.Id,
            UserId = f.UserId,
            UserFullName = (f.User.FirstName + " " + f.User.LastName).Trim(),
            UserEmail = f.User.Email,
            Title = f.Title,
            Description = f.Description,
            Status = f.Status,
            CreatedAt = f.CreatedAt,
            UpdatedAt = f.UpdatedAt,
            MediaAttachments = f.MediaAttachments.Select(m => new FeedbackMediaDto
            {
                Id = m.Id,
                FeedbackId = m.FeedbackId,
                MediaUrl = m.MediaUrl,
                MediaType = m.MediaType,
                CreatedAt = m.CreatedAt
            }).ToList()
        });

        var pagedResult = await projectedQuery.ToPagedResultAsync(
            request.PageNumber,
            request.PageSize,
            cancellationToken);

        return ApiResponse<PagedResult<FeedbackDto>>.SuccessResult(pagedResult, "Feedback list retrieved successfully.");
    }
}
