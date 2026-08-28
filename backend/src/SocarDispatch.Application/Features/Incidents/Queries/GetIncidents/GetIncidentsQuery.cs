using MediatR;
using SocarDispatch.Application.Common.Models;
using SocarDispatch.Application.Features.Incidents.DTOs;

namespace SocarDispatch.Application.Features.Incidents.Queries.GetIncidents;

public record GetIncidentsQuery(
    string? Status = null,
    string? Category = null,
    DateTime? From = null,
    DateTime? To = null
) : IRequest<ApiResponse<List<IncidentDto>>>;
