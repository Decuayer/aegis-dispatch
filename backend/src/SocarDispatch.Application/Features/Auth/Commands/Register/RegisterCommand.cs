using MediatR;
using SocarDispatch.Application.Common.Models;
using SocarDispatch.Application.Features.Auth.DTOs;
using SocarDispatch.Domain.Enums;

namespace SocarDispatch.Application.Features.Auth.Commands.Register;

public record RegisterCommand(
    string FirstName,
    string LastName,
    string Email,
    string Password,
    string Phone,
    string Department,
    RoleType RoleType,
    string? SubRole) : IRequest<ApiResponse<AuthResponseDto>>;
