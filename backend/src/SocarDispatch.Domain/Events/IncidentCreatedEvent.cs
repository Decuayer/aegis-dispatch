using MediatR;

namespace SocarDispatch.Domain.Events;

public record IncidentCreatedEvent(
    Guid IncidentId,
    Guid ReporterId,
    string ReporterFullName,
    string Category,
    string EmergencyCode,
    string Description,
    double Latitude,
    double Longitude,
    DateTime CreatedAt
) : INotification;

