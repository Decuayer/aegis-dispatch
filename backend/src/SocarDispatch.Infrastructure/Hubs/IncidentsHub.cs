using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace SocarDispatch.Infrastructure.Hubs;

[Authorize]
public class IncidentsHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        var role = Context.User?.FindFirst(ClaimTypes.Role)?.Value;
        if (!string.IsNullOrEmpty(role))
        {
            if (role == "Operator")
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, "operators");
            }
            else if (role == "Team")
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, "teams");
            }
            else if (role == "Employee")
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, "employees");
            }
        }

        var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value 
                     ?? Context.User?.FindFirst("sub")?.Value;
        if (!string.IsNullOrEmpty(userId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{userId}");
        }

        await base.OnConnectedAsync();
    }
}
