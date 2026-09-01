using System.Collections.Concurrent;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace SocarDispatch.Infrastructure.Hubs;

[Authorize]
public class LocationHub : Hub
{
    private static readonly ConcurrentDictionary<Guid, DateTime> _lastUpdateTimes = new();
    private const int ThrottleMs = 1000;

    public async Task StreamTeamLocation(Guid teamId, double lat, double lng)
    {
        var now = DateTime.UtcNow;
        if (_lastUpdateTimes.TryGetValue(teamId, out var last) && (now - last).TotalMilliseconds < ThrottleMs)
        {
            return; // Throttle location updates within 1 second window
        }

        _lastUpdateTimes[teamId] = now;
        var payload = new { teamId, lat, lng, timestamp = now };
        await Clients.All.SendAsync("TeamLocationUpdated", payload);
        await Clients.All.SendAsync("ReceiveTeamLocationUpdated", payload);
    }
}
