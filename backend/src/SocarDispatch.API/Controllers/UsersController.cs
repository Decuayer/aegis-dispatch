using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SocarDispatch.Application.Common.Interfaces;
using SocarDispatch.Application.Common.Models;
using SocarDispatch.Application.Features.Auth.DTOs;
using SocarDispatch.Application.Features.Users.Commands.UpdateDeviceToken;
using SocarDispatch.Application.Features.Users.Commands.UpdateUserProfile;
using SocarDispatch.Application.Features.Users.Commands.UpdateUserRole;
using SocarDispatch.Application.Features.Users.DTOs;
using SocarDispatch.Application.Features.Users.Queries.GetUsers;
using SocarDispatch.Domain.Enums;
using SocarDispatch.Domain.Exceptions;

namespace SocarDispatch.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly ISender _sender;
    private readonly IApplicationDbContext _context;

    public UsersController(ISender sender, IApplicationDbContext context)
    {
        _sender = sender;
        _context = context;
    }

    // GET /api/v1/users/me
    // Retrieves the profile information of the logged-in user.
    [HttpGet("me")]
    public async Task<ActionResult<ApiResponse<UserDto>>> GetCurrentUser()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");

        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            throw new DomainException("Invalid user session.");
        }

        var user = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null)
        {
            throw new EntityNotFoundException("User", userId);
        }

        var userDto = new UserDto
        {
            Id = user.Id,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email,
            Phone = user.Phone,
            Department = user.Department,
            RoleType = user.RoleType,
            SubRole = user.SubRole,
            AvatarUrl = user.AvatarUrl
        };

        return Ok(ApiResponse<UserDto>.SuccessResult(userDto, "User information successfully retrieved."));
    }

    // PUT /api/v1/users/me
    // Updates the logged-in user's profile information and avatar.
    [HttpPut("me")]
    public async Task<ActionResult<ApiResponse<UserDto>>> UpdateProfile([FromBody] UpdateUserProfileRequestDto request)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            throw new DomainException("Invalid user session.");
        }

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
    // Registers or updates the logged-in user's FCM device token for push notifications.
    [HttpPost("me/device-token")]
    public async Task<ActionResult<ApiResponse<string>>> UpdateDeviceToken([FromBody] UpdateDeviceTokenRequestDto request)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            throw new DomainException("Invalid user session.");
        }
        var command = new UpdateDeviceTokenCommand(userId, request.Token);
        var result = await _sender.Send(command);
        return Ok(result);
    }

    // GET /api/v1/users
    // Retrieves the contact/department list for all users (Search/Quick Contact Directory).
    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<UserDto>>>> GetUsers(
        [FromQuery] string? search,
        [FromQuery] string? department,
        [FromQuery(Name = "role")] RoleType? role,
        [FromQuery(Name = "roleType")] RoleType? roleType)
    {
        var selectedRole = role ?? roleType;
        var query = new GetUsersQuery(search, department, selectedRole);
        var result = await _sender.Send(query);
        return Ok(result);
    }

    // PATCH /api/v1/users/{id}/role
    // Updates target user's system role and sub-role (Operator access only).
    [HttpPatch("{id:guid}/role")]
    [Authorize(Roles = "Operator")]
    public async Task<ActionResult<ApiResponse<UserDto>>> UpdateUserRole(
        [FromRoute] Guid id,
        [FromBody] UpdateUserRoleRequestDto request)
    {
        var operatorIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        if (string.IsNullOrEmpty(operatorIdClaim) || !Guid.TryParse(operatorIdClaim, out var operatorId))
        {
            throw new DomainException("Invalid user session.");
        }

        var command = new UpdateUserRoleCommand(
            id,
            request.RoleType,
            request.SubRole,
            operatorId
        );

        var result = await _sender.Send(command);
        return Ok(result);
    }

    // Test endpoint: Accessible only by users with the Operator role.
    [HttpGet("admin-only-test")]
    [Authorize(Roles = "Operator")]
    public ActionResult<ApiResponse<string>> OperatorOnlyEndpoint()
    {
        return Ok(ApiResponse<string>.SuccessResult(
            "Success! Only users with the Operator role can view this data.",
            "Authorization Approved"));
    }
}
