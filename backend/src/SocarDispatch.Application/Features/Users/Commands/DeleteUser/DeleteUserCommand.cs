using MediatR;
using SocarDispatch.Application.Common.Models;

namespace SocarDispatch.Application.Features.Users.Commands.DeleteUser;

public record DeleteUserCommand(Guid Id, Guid RequesterId) : IRequest<ApiResponse<bool>>;
