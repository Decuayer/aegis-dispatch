using MediatR;
using SocarDispatch.Application.Common.Models;
using SocarDispatch.Application.Features.Users.DTOs;

namespace SocarDispatch.Application.Features.Users.Commands.LinkGoogleAccount;

public record LinkGoogleAccountCommand(Guid UserId, string IdToken) : IRequest<ApiResponse<CurrentUserDto>>;
