using Microsoft.JSInterop;
using SocarDispatch.Web.Models.Map;

namespace SocarDispatch.Web.Services;

public interface IMapService
{
    Task InitializeMapAsync(string containerId, double lat = 40.409264, double lng = 49.867092, int zoom = 14, string tileProvider = "OpenStreetMap");
    Task UpdateMainTileLayerAsync(string tileProvider);
    Task AddIncidentMarkerAsync(MapIncidentDto incident);
    Task AddTeamMarkerAsync(MapTeamDto team);
    Task RemoveIncidentMarkerAsync(Guid incidentId);
    Task RemoveTeamMarkerAsync(Guid teamId);
    Task PanToIncidentAsync(Guid incidentId);
    Task PanToTeamAsync(Guid teamId);
    Task PanToLocationAsync(double lat, double lng, int zoom = 15);
    Task ToggleIncidentsLayerAsync(bool visible);
    Task ToggleTeamsLayerAsync(bool visible);
    Task InvalidateSizeAsync();
    Task SetDotNetReferenceAsync<T>(DotNetObjectReference<T> dotNetRef) where T : class;
    Task DestroyMapAsync();
    
    Task UpdateIncidentStatusAsync(Guid incidentId, string newStatus);
    Task AnimateTeamMarkerAsync(Guid teamId, double lat, double lng);
    Task UpdateTeamStatusAsync(Guid teamId, string newStatus);
    Task UpdateIncidentAssignmentAsync(Guid incidentId, Guid teamId, string teamName);

    // Interactive Coordinate Picker Map
    Task InitializePickerMapAsync<T>(string containerId, double lat, double lng, int zoom, DotNetObjectReference<T> dotNetRef, string tileProvider = "OpenStreetMap") where T : class;
    Task SetPickerLocationAsync(double lat, double lng, int? zoom = null);
    Task UpdatePickerTileLayerAsync(string tileProvider);
    Task DestroyPickerMapAsync();
}
