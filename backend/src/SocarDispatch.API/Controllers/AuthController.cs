using MediatR;
using Microsoft.AspNetCore.Mvc;
using SocarDispatch.Application.Common.Models;
using SocarDispatch.Application.Features.Auth.Commands.GoogleLogin;
using SocarDispatch.Application.Features.Auth.Commands.GoogleRegister;
using SocarDispatch.Application.Features.Auth.Commands.Login;
using SocarDispatch.Application.Features.Auth.Commands.Register;
using SocarDispatch.Application.Features.Auth.DTOs;

namespace SocarDispatch.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class AuthController : ControllerBase
{
    private readonly ISender _sender;

    public AuthController(ISender sender)
    {
        _sender = sender;
    }

    [HttpPost("register")]
    public async Task<ActionResult<ApiResponse<AuthResponseDto>>> Register([FromBody] RegisterCommand command)
    {
        var result = await _sender.Send(command);
        return Ok(result);
    }

    [HttpPost("login")]
    public async Task<ActionResult<ApiResponse<AuthResponseDto>>> Login([FromBody] LoginCommand command)
    {
        var result = await _sender.Send(command);
        return Ok(result);
    }

    [HttpPost("google-login")]
    public async Task<ActionResult<ApiResponse<AuthResponseDto>>> GoogleLogin([FromBody] GoogleLoginCommand command)
    {
        var result = await _sender.Send(command);
        return Ok(result);
    }

    [HttpPost("google-register")]
    public async Task<ActionResult<ApiResponse<AuthResponseDto>>> GoogleRegister([FromBody] GoogleRegisterCommand command)
    {
        var result = await _sender.Send(command);
        return Ok(result);
    }
}