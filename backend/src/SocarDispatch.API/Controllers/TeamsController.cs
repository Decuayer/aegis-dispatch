using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SocarDispatch.Application.Common.Models;
using SocarDispatch.Application.Features.Teams.Commands.AddTeamMember;
using SocarDispatch.Application.Features.Teams.Commands.CreateTeam;
using SocarDispatch.Application.Features.Teams.Commands.RemoveTeamMember;
using SocarDispatch.Application.Features.Teams.Commands.UpdateTeam;
using SocarDispatch.Application.Features.Teams.Commands.UpdateTeamLocation;
using SocarDispatch.Application.Features.Teams.Commands.UpdateTeamStatus;
using SocarDispatch.Application.Features.Teams.Commands.UpdateTeamMemberStatus;
using SocarDispatch.Application.Features.Teams.DTOs;
using SocarDispatch.Application.Features.Teams.Queries.GetTeamById;
using SocarDispatch.Application.Features.Teams.Queries.GetTeams;
using SocarDispatch.Domain.Exceptions;
using SocarDispatch.Application.Features.Teams.Queries.GetAvailableTeams;


namespace SocarDispatch.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class TeamsController : ControllerBase
{
    private readonly ISender _sender;

    public TeamsController(ISender sender)
    {
        _sender = sender;
    }

    // GET /api/v1/teams
    // Retrieves all emergency teams, their members, and their real-time locations and statuses.
    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<TeamDto>>>> GetAll()
    {
        var query = new GetTeamsQuery();
        var result = await _sender.Send(query);
        return Ok(result);
    }

    // GET /api/v1/teams/available
    // Retrieves available idle teams with open capacity.
    [HttpGet("available")]
    [Authorize(Roles = "Operator,Team")]
    public async Task<ActionResult<ApiResponse<List<AvailableTeamDto>>>> GetAvailableTeams()
    {
        var query = new GetAvailableTeamsQuery();
        var result = await _sender.Send(query);
        return Ok(result);
    }


    // GET /api/v1/teams/{id}
    // Retrieves details of a single emergency team by ID.
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<TeamDto>>> GetById(Guid id)
    {
        var query = new GetTeamByIdQuery(id);
        var result = await _sender.Send(query);
        return Ok(result);
    }

    // POST /api/v1/teams
    // Creates a new response team (Operator or Team role).
    [HttpPost]
    [Authorize(Roles = "Operator,Team")]
    public async Task<ActionResult<ApiResponse<TeamDto>>> Create([FromBody] CreateTeamRequestDto request)
    {
        var requesterId = GetRequesterId();
        var command = new CreateTeamCommand(request.TeamName, request.LeaderId, requesterId);
        var result = await _sender.Send(command);
        return CreatedAtAction(nameof(GetById), new { id = result.Data.Id }, result);
    }

    // PUT /api/v1/teams/{id}
    // Updates team name or leader (Operator or Team Leader).
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Operator,Team")]
    public async Task<ActionResult<ApiResponse<TeamDto>>> Update(Guid id, [FromBody] UpdateTeamRequestDto request)
    {
        var requesterId = GetRequesterId();
        var command = new UpdateTeamCommand(id, requesterId, request.TeamName, request.LeaderId);
        var result = await _sender.Send(command);
        return Ok(result);
    }

    // POST /api/v1/teams/{id}/members
    // Adds a user to the team roster (Operator or Team Leader).
    [HttpPost("{id:guid}/members")]
    [Authorize(Roles = "Operator,Team")]
    public async Task<ActionResult<ApiResponse<TeamDto>>> AddMember(Guid id, [FromBody] AddTeamMemberRequestDto request)
    {
        var requesterId = GetRequesterId();
        var command = new AddTeamMemberCommand(id, requesterId, request.UserId);
        var result = await _sender.Send(command);
        return Ok(result);
    }

    // DELETE /api/v1/teams/{id}/members/{userId}
    // Removes a user from the team roster (Operator or Team Leader).
    [HttpDelete("{id:guid}/members/{userId:guid}")]
    [Authorize(Roles = "Operator,Team")]
    public async Task<ActionResult<ApiResponse<TeamDto>>> RemoveMember(Guid id, Guid userId)
    {
        var requesterId = GetRequesterId();
        var command = new RemoveTeamMemberCommand(id, requesterId, userId);
        var result = await _sender.Send(command);
        return Ok(result);
    }

    // PATCH /api/v1/teams/{teamId}/status
    // Updates the status of the emergency team (Idle, Forwarded, OnScene, Busy).
    [HttpPatch("{teamId:guid}/status")]
    [Authorize(Roles = "Operator,Team")]
    public async Task<ActionResult<ApiResponse<TeamDto>>> UpdateStatus(Guid teamId, [FromBody] UpdateTeamStatusRequestDto request)
    {
        var requesterId = GetRequesterId();
        var command = new UpdateTeamStatusCommand(teamId, requesterId, request.Status);
        var result = await _sender.Send(command);
        return Ok(result);
    }

    // PATCH /api/v1/teams/{teamId}/members/{userId}/status
    [HttpPatch("{teamId:guid}/members/{userId:guid}/status")]
    [Authorize(Roles = "Operator,Team")]
    public async Task<ActionResult<ApiResponse<TeamMemberDto>>> UpdateMemberStatus(
        Guid teamId,
        Guid userId,
        [FromBody] UpdateTeamMemberStatusRequestDto request)
    {
        var requesterId = GetRequesterId();
        var command = new UpdateTeamMemberStatusCommand(teamId, userId, requesterId, request.Status);
        var result = await _sender.Send(command);
        return Ok(result);
    }

    // POST /api/v1/teams/location
    // Updates the emergency team's real-time GPS location data (Latitude/Longitude) to the server.
    [HttpPost("location")]
    [Authorize(Roles = "Operator,Team")]
    public async Task<ActionResult<ApiResponse<TeamDto>>> UpdateLocation([FromBody] UpdateTeamLocationRequestDto request)
    {
        var command = new UpdateTeamLocationCommand(request.TeamId, request.Latitude, request.Longitude);
        var result = await _sender.Send(command);
        return Ok(result);
    }

    private Guid GetRequesterId()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var requesterId))
        {
            throw new DomainException("Invalid user session. Token is missing or invalid.");
        }
        return requesterId;
    }
}
