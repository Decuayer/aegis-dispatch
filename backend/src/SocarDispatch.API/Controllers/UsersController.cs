using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SocarDispatch.Application.Common.Models;
using SocarDispatch.Application.Features.Auth.DTOs;
using SocarDispatch.Application.Features.Users.Commands.DeleteUser;
using SocarDispatch.Application.Features.Users.Commands.UpdateDeviceToken;
using SocarDispatch.Application.Features.Users.Commands.UpdateUserProfile;
using SocarDispatch.Application.Features.Users.Commands.UpdateUserRole;
using SocarDispatch.Application.Features.Users.DTOs;
using SocarDispatch.Application.Features.Users.Queries.GetCurrentUser;
using SocarDispatch.Application.Features.Users.Queries.GetUsers;
using SocarDispatch.Domain.Exceptions;

namespace SocarDispatch.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly ISender _sender;

    public UsersController(ISender sender)
    {
        _sender = sender;
    }

    private Guid GetCurrentUserId()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");

        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            throw new DomainException("Invalid user session.");
        }

        return userId;
    }

    // GET /api/v1/users/me
    // Retrieves profile information of the logged-in user.
    [HttpGet("me")]
    [ProducesResponseType(typeof(ApiResponse<CurrentUserDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<CurrentUserDto>>> GetCurrentUser()
    {
        var userId = GetCurrentUserId();
        var result = await _sender.Send(new GetCurrentUserQuery(userId));
        return Ok(result);
    }

    // PUT /api/v1/users/me
    // Updates profile details of the logged-in user.
    [HttpPut("me")]
    public async Task<ActionResult<ApiResponse<UserDto>>> UpdateProfile([FromBody] UpdateUserProfileRequestDto request)
    {
        var userId = GetCurrentUserId();

        var command = new UpdateUserProfileCommand(
            userId,
            request.FirstName,
            request.LastName,
            request.Phone,
            request.Department,
            request.SubRole,
            request.AvatarUrl
        );

        var result = await _sender.Send(command);
        return Ok(result);
    }

    // POST /api/v1/users/me/device-token
    // Updates FCM push notification device token for the logged-in user.
    [HttpPost("me/device-token")]
    public async Task<ActionResult<ApiResponse<string>>> UpdateDeviceToken([FromBody] UpdateDeviceTokenRequestDto request)
    {
        var userId = GetCurrentUserId();
        var command = new UpdateDeviceTokenCommand(userId, request.Token);
        var result = await _sender.Send(command);
        return Ok(result);
    }

    // GET /api/v1/users
    // Retrieves paginated user contact directory.
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<UserDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PagedResult<UserDto>>>> GetUsers([FromQuery] GetUsersQuery query)
    {
        var result = await _sender.Send(query);
        return Ok(result);
    }

    // PATCH /api/v1/users/{id}/role
    // Updates system and operational roles of a user (Operator only).
    [HttpPatch("{id:guid}/role")]
    [Authorize(Roles = "Operator")]
    public async Task<ActionResult<ApiResponse<UserDto>>> UpdateUserRole(
        [FromRoute] Guid id,
        [FromBody] UpdateUserRoleRequestDto request)
    {
        var operatorId = GetCurrentUserId();

        var command = new UpdateUserRoleCommand(
            id,
            request.RoleType,
            request.SubRole,
            operatorId
        );

        var result = await _sender.Send(command);
        return Ok(result);
    }

    // DELETE /api/v1/users/{id}
    // Deletes a user account (Operator only).
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Operator")]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteUser([FromRoute] Guid id)
    {
        var operatorId = GetCurrentUserId();
        var command = new DeleteUserCommand(id, operatorId);
        var result = await _sender.Send(command);
        return Ok(result);
    }

    // GET /api/v1/users/admin-only-test
    // Role-based access verification test endpoint.
    [HttpGet("admin-only-test")]
    [Authorize(Roles = "Operator")]
    public ActionResult<ApiResponse<string>> OperatorOnlyEndpoint()
    {
        return Ok(ApiResponse<string>.SuccessResult(
            "Success! Only users with the Operator role can view this data.",
            "Authorization Approved"));
    }
}
