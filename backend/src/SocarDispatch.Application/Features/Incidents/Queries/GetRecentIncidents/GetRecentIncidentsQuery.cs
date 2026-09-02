using MediatR;
using SocarDispatch.Application.Common.Models;
using SocarDispatch.Application.Features.Incidents.DTOs;

namespace SocarDispatch.Application.Features.Incidents.Queries.GetRecentIncidents;

public record GetRecentIncidentsQuery(
    int Limit = 10
) : IRequest<ApiResponse<IReadOnlyList<IncidentDto>>>;
