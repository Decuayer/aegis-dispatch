using MediatR;
using SocarDispatch.Application.Common.Models;
using SocarDispatch.Application.Features.Feedbacks.DTOs;

namespace SocarDispatch.Application.Features.Feedbacks.Queries.GetFeedbackById;

public record GetFeedbackByIdQuery(
    Guid Id,
    Guid RequestingUserId,
    string RequestingUserRole
) : IRequest<ApiResponse<FeedbackDto>>;
