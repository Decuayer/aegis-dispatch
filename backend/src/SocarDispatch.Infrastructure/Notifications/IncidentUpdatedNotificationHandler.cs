using MediatR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using SocarDispatch.Domain.Events;
using SocarDispatch.Infrastructure.Hubs;

namespace SocarDispatch.Infrastructure.Notifications;

public class IncidentUpdatedNotificationHandler : INotificationHandler<IncidentUpdatedEvent>
{
    private readonly IHubContext<IncidentsHub> _hubContext;
    private readonly ILogger<IncidentUpdatedNotificationHandler> _logger;

    public IncidentUpdatedNotificationHandler(
        IHubContext<IncidentsHub> hubContext,
        ILogger<IncidentUpdatedNotificationHandler> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task Handle(IncidentUpdatedEvent notification, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Broadcasting IncidentUpdated event for IncidentId: {IncidentId}", notification.IncidentId);

            await _hubContext.Clients.All.SendAsync("IncidentUpdated", new
            {
                incidentId = notification.IncidentId,
                category = notification.Category,
                emergencyCode = notification.EmergencyCode,
                description = notification.Description,
                latitude = notification.Latitude,
                longitude = notification.Longitude,
                updatedById = notification.UpdatedById,
                updatedAt = notification.UpdatedAt
            }, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while broadcasting IncidentUpdated event for IncidentId: {IncidentId}", notification.IncidentId);
        }
    }
}
