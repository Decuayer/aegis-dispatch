using MediatR;
using Microsoft.AspNetCore.Http;
using SocarDispatch.Application.Common.Models;
using SocarDispatch.Application.Features.Feedbacks.DTOs;

namespace SocarDispatch.Application.Features.Feedbacks.Commands.CreateFeedback;

public record CreateFeedbackCommand(
    Guid UserId,
    string Title,
    string Description,
    IReadOnlyList<IFormFile>? Attachments = null
) : IRequest<ApiResponse<FeedbackDto>>;
