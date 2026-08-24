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
    Task SetDotNetReferenceAsync(DotNetObjectReference<object> dotNetRef);
    Task DestroyMapAsync();
}
