using MediatR;
using SocarDispatch.Application.Common.Models;
using SocarDispatch.Application.Features.Auth.DTOs;

namespace SocarDispatch.Application.Features.Auth.Commands.GoogleRegister;

public record GoogleRegisterCommand(
    string IdToken,
    string? Phone = null,
    string? Department = null
) : IRequest<ApiResponse<AuthResponseDto>>;
