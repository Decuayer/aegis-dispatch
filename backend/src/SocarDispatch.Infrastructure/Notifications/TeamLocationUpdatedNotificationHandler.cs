using MediatR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using SocarDispatch.Domain.Events;
using SocarDispatch.Infrastructure.Hubs;

namespace SocarDispatch.Infrastructure.Notifications;

public class TeamLocationUpdatedNotificationHandler : INotificationHandler<TeamLocationUpdatedEvent>
{
    private readonly IHubContext<LocationHub> _hubContext;
    private readonly ILogger<TeamLocationUpdatedNotificationHandler> _logger;

    public TeamLocationUpdatedNotificationHandler(
        IHubContext<LocationHub> hubContext,
        ILogger<TeamLocationUpdatedNotificationHandler> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task Handle(TeamLocationUpdatedEvent notification, CancellationToken cancellationToken)
    {
        try
        {
            var payload = new
            {
                teamId = notification.TeamId,
                lat = notification.Latitude,
                lng = notification.Longitude,
                updatedAt = notification.UpdatedAt
            };

            await _hubContext.Clients.All.SendAsync("TeamLocationUpdated", payload, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error broadcasting TeamLocationUpdated for TeamId: {TeamId}", notification.TeamId);
        }
    }
}
