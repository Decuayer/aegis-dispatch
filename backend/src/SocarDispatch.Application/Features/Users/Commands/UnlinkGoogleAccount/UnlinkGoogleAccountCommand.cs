using MediatR;
using SocarDispatch.Application.Common.Models;
using SocarDispatch.Application.Features.Users.DTOs;

namespace SocarDispatch.Application.Features.Users.Commands.UnlinkGoogleAccount;

public record UnlinkGoogleAccountCommand(Guid UserId) : IRequest<ApiResponse<CurrentUserDto>>;
