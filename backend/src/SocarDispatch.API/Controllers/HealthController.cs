using MediatR;
using Microsoft.AspNetCore.Mvc;
using SocarDispatch.Application.Common.Models;

namespace SocarDispatch.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class HealthController : ControllerBase
{
    private readonly ISender _sender;

    public HealthController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet("ping")]
    public async Task<IActionResult> Ping()
    {
        var result = await _sender.Send(new PingQuery());
        return Ok(result);
    }
}
