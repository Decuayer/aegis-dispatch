using MediatR;
using SocarDispatch.Application.Common.Models;
using SocarDispatch.Application.Features.Feedbacks.DTOs;
using SocarDispatch.Domain.Enums;

namespace SocarDispatch.Application.Features.Feedbacks.Queries.GetFeedbacks;

public class GetFeedbacksQuery : IRequest<ApiResponse<PagedResult<FeedbackDto>>>
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 25;
    public FeedbackStatus? Status { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public Guid? UserId { get; set; }
}
