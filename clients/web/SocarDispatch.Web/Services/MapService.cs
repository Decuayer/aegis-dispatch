using Microsoft.JSInterop;
using SocarDispatch.Web.Models.Map;

namespace SocarDispatch.Web.Services;

public class MapService : IMapService
{
    private readonly IJSRuntime _js;

    public MapService(IJSRuntime js)
    {
        _js = js;
    }

    public async Task InitializeMapAsync(string containerId, double lat = 40.409264, double lng = 49.867092, int zoom = 14, string tileProvider = "OpenStreetMap")
        => await _js.InvokeVoidAsync("leafletMap.initMap", containerId, lat, lng, zoom, tileProvider);

    public async Task UpdateMainTileLayerAsync(string tileProvider)
        => await _js.InvokeVoidAsync("leafletMap.updateMainTileLayer", tileProvider);

    public async Task ToggleResolvedIncidentsLayerAsync(bool visible)
        => await _js.InvokeVoidAsync("leafletMap.toggleResolvedIncidentsLayer", visible);

    public async Task AddIncidentMarkerAsync(MapIncidentDto incident)
        => await _js.InvokeVoidAsync("leafletMap.addIncidentMarker", new
        {
            id = incident.Id.ToString(),
            lat = (double)incident.Latitude,
            lng = (double)incident.Longitude,
            category = incident.Category,
            emergencyCode = incident.EmergencyCode,
            reporterFullName = incident.ReporterFullName,
            status = incident.Status,
            createdAt = incident.CreatedAt.ToString("O"),
            assignedTeamName = incident.AssignedTeamName
        });

    public async Task AddTeamMarkerAsync(MapTeamDto team)
    {
        if (!team.CurrentLatitude.HasValue || !team.CurrentLongitude.HasValue) return;

        await _js.InvokeVoidAsync("leafletMap.addTeamMarker", new
        {
            id = team.Id.ToString(),
            teamName = team.TeamName,
            status = team.Status,
            lat = (double)team.CurrentLatitude.Value,
            lng = (double)team.CurrentLongitude.Value,
            updatedAt = team.UpdatedAt.ToString("O")
        });
    }

    public async Task RemoveIncidentMarkerAsync(Guid incidentId)
        => await _js.InvokeVoidAsync("leafletMap.removeIncidentMarker", incidentId.ToString());

    public async Task RemoveTeamMarkerAsync(Guid teamId)
        => await _js.InvokeVoidAsync("leafletMap.removeTeamMarker", teamId.ToString());

    public async Task PanToIncidentAsync(Guid incidentId)
        => await _js.InvokeVoidAsync("leafletMap.panToIncident", incidentId.ToString());

    public async Task PanToTeamAsync(Guid teamId)
        => await _js.InvokeVoidAsync("leafletMap.panToTeam", teamId.ToString());

    public async Task PanToLocationAsync(double lat, double lng, int zoom = 15)
        => await _js.InvokeVoidAsync("leafletMap.panToLocation", lat, lng, zoom);

    public async Task FocusMarkerByIdAsync(string entityType, Guid entityId, int zoomLevel = 17)
        => await _js.InvokeVoidAsync("leafletMap.focusMarkerById", entityType, entityId.ToString(), zoomLevel);

    public async Task ToggleIncidentsLayerAsync(bool visible)
        => await _js.InvokeVoidAsync("leafletMap.toggleIncidentsLayer", visible);

    public async Task ToggleTeamsLayerAsync(bool visible)
        => await _js.InvokeVoidAsync("leafletMap.toggleTeamsLayer", visible);

    public async Task InvalidateSizeAsync()
        => await _js.InvokeVoidAsync("leafletMap.invalidateSize");

    public async Task SetDotNetReferenceAsync<T>(DotNetObjectReference<T> dotNetRef) where T : class
        => await _js.InvokeVoidAsync("leafletMap.setDotNetRef", dotNetRef);

    public async Task DestroyMapAsync()
        => await _js.InvokeVoidAsync("leafletMap.destroyMap");

    public async Task UpdateIncidentStatusAsync(Guid incidentId, string newStatus)
        => await _js.InvokeVoidAsync("leafletMap.updateIncidentStatus", incidentId.ToString(), newStatus);

    public async Task AnimateTeamMarkerAsync(Guid teamId, double lat, double lng)
        => await _js.InvokeVoidAsync("leafletMap.animateTeamMarker", teamId.ToString(), lat, lng);

    public async Task UpdateTeamStatusAsync(Guid teamId, string newStatus)
        => await _js.InvokeVoidAsync("leafletMap.updateTeamStatus", teamId.ToString(), newStatus);

    public async Task UpdateIncidentAssignmentAsync(Guid incidentId, Guid teamId, string teamName)
        => await _js.InvokeVoidAsync("leafletMap.updateIncidentAssignment", incidentId.ToString(), teamId.ToString(), teamName);

    public async Task InitializePickerMapAsync<T>(string containerId, double lat, double lng, int zoom, DotNetObjectReference<T> dotNetRef, string tileProvider = "OpenStreetMap") where T : class
        => await _js.InvokeVoidAsync("leafletMap.initPickerMap", containerId, lat, lng, zoom, dotNetRef, tileProvider);

    public async Task SetPickerLocationAsync(double lat, double lng, int? zoom = null)
        => await _js.InvokeVoidAsync("leafletMap.setPickerLocation", lat, lng, zoom);

    public async Task UpdatePickerTileLayerAsync(string tileProvider)
        => await _js.InvokeVoidAsync("leafletMap.updatePickerTileLayer", tileProvider);

    public async Task DestroyPickerMapAsync()
        => await _js.InvokeVoidAsync("leafletMap.destroyPickerMap");
}
