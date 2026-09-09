using MediatR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using SocarDispatch.Domain.Events;
using SocarDispatch.Infrastructure.Hubs;

namespace SocarDispatch.Infrastructure.Notifications;

public class IncidentStatusChangedNotificationHandler : INotificationHandler<IncidentStatusChangedEvent>
{
    private readonly IHubContext<IncidentsHub> _hubContext;
    private readonly ILogger<IncidentStatusChangedNotificationHandler> _logger;

    public IncidentStatusChangedNotificationHandler(
        IHubContext<IncidentsHub> hubContext,
        ILogger<IncidentStatusChangedNotificationHandler> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task Handle(IncidentStatusChangedEvent notification, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation(
                "Incident status changed notification received. IncidentId: {IncidentId}, PreviousStatus: {PreviousStatus}, NewStatus: {NewStatus}",
                notification.IncidentId, notification.PreviousStatus, notification.NewStatus
            );

            var statusPayload = new
            {
                incidentId = notification.IncidentId,
                previousStatus = notification.PreviousStatus.ToString(),
                status = notification.NewStatus.ToString(),
                changedById = notification.ChangedById,
                changedAt = notification.ChangedAt
            };

            // SignalR Live Broadcast
            await _hubContext.Clients.All.SendAsync("IncidentStatusChanged", statusPayload, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while processing IncidentStatusChangedEvent for IncidentId: {IncidentId}", notification.IncidentId);
        }
    }
}
