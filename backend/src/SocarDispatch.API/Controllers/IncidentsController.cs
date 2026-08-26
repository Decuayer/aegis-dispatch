using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SocarDispatch.Application.Common.Models;
using SocarDispatch.Application.Features.Incidents.Commands.ChangeIncidentStatus;
using SocarDispatch.Application.Features.Incidents.Commands.CreateIncident;
using SocarDispatch.Application.Features.Incidents.Commands.UpdateIncident;
using SocarDispatch.Application.Features.Incidents.DTOs;
using SocarDispatch.Application.Features.Incidents.Queries.GetIncidentById;
using SocarDispatch.Application.Features.Incidents.Queries.GetIncidents;
using SocarDispatch.Domain.Exceptions;

namespace SocarDispatch.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class IncidentsController : ControllerBase
{
    private readonly ISender _sender;

    public IncidentsController(ISender sender)
    {
        _sender = sender;
    }

    /// POST /api/v1/incidents
    // Creates a new emergency incident report submitted by a field worker (returns 201 Created).
    [HttpPost]
    public async Task<ActionResult<ApiResponse<IncidentDto>>> Create([FromBody] CreateIncidentRequestDto request)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var reporterId))
        {
            throw new DomainException("Invalid user session. Token is missing or invalid.");
        }

        var command = new CreateIncidentCommand(
            reporterId,
            request.Category,
            request.EmergencyCode,
            request.Description,
            request.MediaAttachments,
            request.Latitude,
            request.Longitude
        );
        
        var result = await _sender.Send(command);
        return CreatedAtAction(nameof(GetById), new { id = result.Data.Id }, result);
    }

    // GET /api/v1/incidents
    // Retrieves the list and details of all events with optional temporal and category filters.
    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<IncidentDto>>>> GetAll(
        [FromQuery] string? status, 
        [FromQuery] string? category,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to)
    {
        var query = new GetIncidentsQuery(status, category, from, to);
        var result = await _sender.Send(query);
        return Ok(result);
    }

    // GET /api/v1/incidents/{id}
    // Retrieves all details of a specific event.
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<IncidentDto>>> GetById(Guid id)
    {
        var query = new GetIncidentByIdQuery(id);
        var result = await _sender.Send(query);
        return Ok(result);
    }

    // PUT /api/v1/incidents/{id}
    // Updates incident details (with operator authority).
    [HttpPut("{id:guid}")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<IncidentDto>>> Update(Guid id, [FromBody] UpdateIncidentRequestDto request)
    {   
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var requesterId))
        {
            throw new DomainException("Invalid user session. Token is missing or invalid.");
        }

        var command = new UpdateIncidentCommand(
            id,
            requesterId,
            request.Category,
            request.EmergencyCode,
            request.Description,
            request.MediaAttachments,
            request.Latitude,
            request.Longitude
        );
        var result = await _sender.Send(command);
        return Ok(result);
    }

    // PATCH /api/v1/incidents/{id}/status
    // Changes the status of the incident (Open, Assigned, Resolved, Canceled).
    [HttpPatch("{id:guid}/status")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<IncidentDto>>> ChangeStatus(Guid id, [FromBody] ChangeIncidentStatusRequestDto request)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var requesterId))
        {
            throw new DomainException("Invalid user session. Token is missing or invalid.");
        }
        var userRoleClaim = User.FindFirstValue(ClaimTypes.Role) ?? User.FindFirstValue("role") ?? string.Empty;
        var command = new ChangeIncidentStatusCommand(id, request.Status, requesterId, userRoleClaim, request.CompletionNotes);
        var result = await _sender.Send(command);
        return Ok(result);
    }
}
