using MediatR;
using SocarDispatch.Application.Common.Models;

namespace SocarDispatch.Application.Features.Incidents.Commands.DeleteIncident;

public record DeleteIncidentCommand(Guid Id, Guid RequesterId, string? UserRole = null) : IRequest<ApiResponse<bool>>;
