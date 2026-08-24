using MediatR;
using SocarDispatch.Application.Common.Models;
using SocarDispatch.Application.Features.Auth.DTOs;
using SocarDispatch.Domain.Enums;

namespace SocarDispatch.Application.Features.Users.Commands.UpdateUserRole;

public record UpdateUserRoleCommand(
    Guid UserId,
    RoleType RoleType,
    string? SubRole,
    Guid OperatorId
) : IRequest<ApiResponse<UserDto>>;
