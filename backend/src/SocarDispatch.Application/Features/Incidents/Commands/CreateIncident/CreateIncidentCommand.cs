using MediatR;
using SocarDispatch.Application.Common.Models;
using SocarDispatch.Application.Features.Incidents.DTOs;


namespace SocarDispatch.Application.Features.Incidents.Commands.CreateIncident;

public record CreateIncidentCommand(
    Guid ReporterId,
    string Category,
    string EmergencyCode,
    string? Description,
    List<CreateIncidentMediaRequestDto> MediaAttachments,
    decimal Latitude,
    decimal Longitude
) : IRequest<ApiResponse<IncidentDto>>;