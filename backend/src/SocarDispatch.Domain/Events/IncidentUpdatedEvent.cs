using MediatR;

namespace SocarDispatch.Domain.Events;

/// Domain event published when incident details (taxonomies, description, coordinates) are updated.
public record IncidentUpdatedEvent(
    Guid IncidentId,
    string Category,
    string EmergencyCode,
    string? Description,
    decimal Latitude,
    decimal Longitude,
    Guid UpdatedById,
    DateTime UpdatedAt
) : INotification;
