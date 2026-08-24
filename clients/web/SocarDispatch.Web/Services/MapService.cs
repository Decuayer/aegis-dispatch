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

    public async Task InitializeMapAsync(string containerId, double lat = 40.409264, double lng = 49.867092, int zoom = 14)
        => await _js.InvokeVoidAsync("leafletMap.initMap", containerId, lat, lng, zoom);

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

    public async Task PanToLocationAsync(double lat, double lng, int zoom = 15)
        => await _js.InvokeVoidAsync("leafletMap.panToLocation", lat, lng, zoom);

    public async Task SetDotNetReferenceAsync(DotNetObjectReference<object> dotNetRef)
        => await _js.InvokeVoidAsync("leafletMap.setDotNetRef", dotNetRef);

    public async Task DestroyMapAsync()
        => await _js.InvokeVoidAsync("leafletMap.destroyMap");
}
