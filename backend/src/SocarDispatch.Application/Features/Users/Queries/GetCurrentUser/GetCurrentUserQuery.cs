using MediatR;
using SocarDispatch.Application.Common.Models;
using SocarDispatch.Application.Features.Users.DTOs;

namespace SocarDispatch.Application.Features.Users.Queries.GetCurrentUser;

public record GetCurrentUserQuery(Guid UserId) : IRequest<ApiResponse<CurrentUserDto>>;
