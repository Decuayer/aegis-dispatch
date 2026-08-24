using Microsoft.JSInterop;
using SocarDispatch.Web.Models.Map;

namespace SocarDispatch.Web.Services;

public interface IMapService
{
    Task InitializeMapAsync(string containerId, double lat = 40.409264, double lng = 49.867092, int zoom = 14);
    Task AddIncidentMarkerAsync(MapIncidentDto incident);
    Task AddTeamMarkerAsync(MapTeamDto team);
    Task RemoveIncidentMarkerAsync(Guid incidentId);
    Task RemoveTeamMarkerAsync(Guid teamId);
    Task PanToIncidentAsync(Guid incidentId);
    Task PanToLocationAsync(double lat, double lng, int zoom = 15);
    Task SetDotNetReferenceAsync<T>(DotNetObjectReference<T> dotNetRef) where T : class;
    Task DestroyMapAsync();
    
    // Incident marker rengini/badge'ini güncelle (status değiştiğinde)
    Task UpdateIncidentStatusAsync(Guid incidentId, string newStatus);

    // Team marker'ını smooth animasyonla hareket ettir (setLatLng yerine)
    Task AnimateTeamMarkerAsync(Guid teamId, double lat, double lng);

    // Team marker status rengini güncelle
    Task UpdateTeamStatusAsync(Guid teamId, string newStatus);

    // Incident'a atanmış takım label'ını güncelle
    Task UpdateIncidentAssignmentAsync(Guid incidentId, Guid teamId, string teamName);

}
