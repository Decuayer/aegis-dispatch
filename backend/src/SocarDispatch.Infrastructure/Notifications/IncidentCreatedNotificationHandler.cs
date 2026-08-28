using MediatR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using SocarDispatch.Domain.Events;
using SocarDispatch.Infrastructure.Hubs;

namespace SocarDispatch.Infrastructure.Notifications;

public class IncidentCreatedNotificationHandler : INotificationHandler<IncidentCreatedEvent>
{
    private readonly IHubContext<IncidentsHub> _hubContext;
    private readonly ILogger<IncidentCreatedNotificationHandler> _logger;

    public IncidentCreatedNotificationHandler(
        IHubContext<IncidentsHub> hubContext,
        ILogger<IncidentCreatedNotificationHandler> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task Handle(IncidentCreatedEvent notification, CancellationToken cancellationToken)
    {
        try
        {
            await _hubContext.Clients.Group("operators").SendAsync("NewIncident", new
            {
                id = notification.IncidentId,
                reporterId = notification.ReporterId,
                reporterFullName = notification.ReporterFullName,
                category = notification.Category,
                emergencyCode = notification.EmergencyCode,
                description = notification.Description,
                latitude = notification.Latitude,
                longitude = notification.Longitude,
                createdAt = notification.CreatedAt
            }, cancellationToken);

            _logger.LogInformation("Broadcasted NewIncident event to operators for IncidentId: {IncidentId}", notification.IncidentId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error broadcasting NewIncident event for IncidentId: {IncidentId}", notification.IncidentId);
        }
    }
}
