using MediatR;
using SocarDispatch.Application.Common.Models;
using SocarDispatch.Application.Features.Auth.DTOs;

namespace SocarDispatch.Application.Features.Auth.Commands.Login;

public record LoginCommand(
    string Email,
    string Password) : IRequest<ApiResponse<AuthResponseDto>>;
