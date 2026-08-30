using MediatR;
using SocarDispatch.Application.Common.Models;
using SocarDispatch.Application.Features.Teams.DTOs;

namespace SocarDispatch.Application.Features.Teams.Queries.GetAvailableTeams;

public record GetAvailableTeamsQuery() : IRequest<ApiResponse<List<AvailableTeamDto>>>;
