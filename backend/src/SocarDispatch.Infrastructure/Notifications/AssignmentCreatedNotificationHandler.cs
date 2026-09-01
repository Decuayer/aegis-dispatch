using System.Globalization;
using MediatR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SocarDispatch.Application.Common.Interfaces;
using SocarDispatch.Domain.Events;
using SocarDispatch.Infrastructure.Hubs;

namespace SocarDispatch.Infrastructure.Notifications;

public class AssignmentCreatedNotificationHandler : INotificationHandler<AssignmentCreatedEvent>
{
    private readonly IApplicationDbContext _context;
    private readonly IPushNotificationService _pushService;
    private readonly IHubContext<IncidentsHub> _hubContext;
    private readonly ILogger<AssignmentCreatedNotificationHandler> _logger;

    public AssignmentCreatedNotificationHandler(
        IApplicationDbContext context,
        IPushNotificationService pushService,
        IHubContext<IncidentsHub> hubContext,
        ILogger<AssignmentCreatedNotificationHandler> logger)
    {
        _context = context;
        _pushService = pushService;
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task Handle(AssignmentCreatedEvent notification, CancellationToken cancellationToken)
    {
        try
        {
            // 1. SignalR Real-Time TeamDispatched & ReceiveAssignmentCreated Broadcast
            var dispatchPayload = new
            {
                assignmentId = notification.AssignmentId,
                incidentId = notification.IncidentId,
                teamId = notification.TeamId,
                operatorId = notification.OperatorId,
                assignedAt = notification.AssignedAt
            };

            await _hubContext.Clients.All.SendAsync("TeamDispatched", dispatchPayload, cancellationToken);
            await _hubContext.Clients.All.SendAsync("ReceiveAssignmentCreated", dispatchPayload, cancellationToken);

            // 2. FCM Push Notification
            var teamMembers = await _context.TeamMembers
                .Include(tm => tm.User)
                .Where(tm => tm.TeamId == notification.TeamId && tm.User.DeviceToken != null)
                .ToListAsync(cancellationToken);

            var tokens = teamMembers
                .Select(tm => tm.User.DeviceToken!)
                .Where(t => !string.IsNullOrWhiteSpace(t))
                .Distinct()
                .ToList();

            if (!tokens.Any()) return;

            var incident = await _context.Incidents
                .AsNoTracking()
                .FirstOrDefaultAsync(i => i.Id == notification.IncidentId, cancellationToken);

            if (incident == null) return;

            var data = new Dictionary<string, string>
            {
                { "incidentId", notification.IncidentId.ToString() },
                { "emergencyCode", incident.EmergencyCode },
                { "category", incident.Category },
                { "latitude", incident.Latitude.ToString(CultureInfo.InvariantCulture) },
                { "longitude", incident.Longitude.ToString(CultureInfo.InvariantCulture) },
                { "type", "DISPATCH_ALERT" }
            };

            await _pushService.SendMulticastAsync(
                tokens,
                title: $"Emergency Dispatched — {incident.EmergencyCode}",
                body: $"{incident.Category} | Proceed to coordinates.",
                data: data,
                ct: cancellationToken
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error occurred in AssignmentCreatedNotificationHandler for AssignmentId: {AssignmentId}",
                notification.AssignmentId
            );
        }
    }
}
