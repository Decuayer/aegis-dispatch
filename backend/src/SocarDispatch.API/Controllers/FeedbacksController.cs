using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SocarDispatch.Application.Common.Models;
using SocarDispatch.Application.Features.Feedbacks.Commands.CreateFeedback;
using SocarDispatch.Application.Features.Feedbacks.DTOs;
using SocarDispatch.Application.Features.Feedbacks.Queries.GetFeedbackById;
using SocarDispatch.Application.Features.Feedbacks.Queries.GetFeedbacks;
using SocarDispatch.Domain.Enums;
using SocarDispatch.Domain.Exceptions;

namespace SocarDispatch.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class FeedbacksController : ControllerBase
{
    private readonly ISender _sender;

    public FeedbacksController(ISender sender)
    {
        _sender = sender;
    }

    private Guid GetCurrentUserId()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            throw new DomainException("Invalid user session. Token is missing or invalid.");
        }
        return userId;
    }

    private string GetCurrentUserRole()
    {
        return User.FindFirstValue(ClaimTypes.Role) ?? User.FindFirstValue("role") ?? string.Empty;
    }

    // POST /api/v1/feedbacks
    // Creates a new operational feedback report with multipart media attachments.
    [HttpPost]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(100 * 1024 * 1024)]
    [ProducesResponseType(typeof(ApiResponse<FeedbackDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<FeedbackDto>>> Create([FromForm] CreateFeedbackRequestDto request)
    {
        var userId = GetCurrentUserId();
        var command = new CreateFeedbackCommand(userId, request.Title, request.Description, request.Attachments);

        var result = await _sender.Send(command);
        return CreatedAtAction(nameof(GetById), new { id = result.Data!.Id }, result);
    }

    // GET /api/v1/feedbacks
    // Retrieves paginated and filtered feedback reports (Operator authority only).
    [HttpGet]
    [Authorize(Roles = "Operator")]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<FeedbackDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<PagedResult<FeedbackDto>>>> GetAll([FromQuery] GetFeedbacksQuery query)
    {
        var result = await _sender.Send(query);
        return Ok(result);
    }

    // GET /api/v1/feedbacks/my
    // Retrieves feedback reports submitted by the logged-in user.
    [HttpGet("my")]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<FeedbackDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PagedResult<FeedbackDto>>>> GetMyFeedbacks(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 25,
        [FromQuery] FeedbackStatus? status = null,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null)
    {
        var userId = GetCurrentUserId();
        var query = new GetFeedbacksQuery
        {
            PageNumber = pageNumber,
            PageSize = pageSize,
            Status = status,
            FromDate = fromDate,
            ToDate = toDate,
            UserId = userId
        };

        var result = await _sender.Send(query);
        return Ok(result);
    }

    // GET /api/v1/feedbacks/{id}
    // Retrieves detailed feedback report with media attachments.
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<FeedbackDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<FeedbackDto>>> GetById(Guid id)
    {
        var userId = GetCurrentUserId();
        var userRole = GetCurrentUserRole();

        var query = new GetFeedbackByIdQuery(id, userId, userRole);
        var result = await _sender.Send(query);
        return Ok(result);
    }
}
