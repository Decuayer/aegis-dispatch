// Pages/Map.razor.cs
using System.Net.Http.Json;
using Microsoft.AspNetCore.Components;
using SocarDispatch.Web.Components.Map;
using SocarDispatch.Web.Events;
using SocarDispatch.Web.Models.Common;
using SocarDispatch.Web.Models.Dispatch;
using SocarDispatch.Web.Models.Map;
using SocarDispatch.Web.Services;
using SocarDispatch.Web.Services.SignalR;

namespace SocarDispatch.Web.Pages;

public partial class Map : ComponentBase, IDisposable
{
    [Inject] private HttpClient Http { get; set; } = default!;
    [Inject] private IIncidentHubClient IncidentHub { get; set; } = default!;
    [Inject] private ILocationHubClient LocationHub { get; set; } = default!;
    [Inject] private ISettingsService SettingsService { get; set; } = default!;

    [SupplyParameterFromQuery(Name = "incidentId")]
    public Guid? TargetIncidentId { get; set; }

    [SupplyParameterFromQuery(Name = "lat")]
    public double? TargetLat { get; set; }

    [SupplyParameterFromQuery(Name = "lng")]
    public double? TargetLng { get; set; }

    [SupplyParameterFromQuery(Name = "teamId")]
    public Guid? TargetTeamId { get; set; }

    private LeafletMap? _mapRef;
    private List<MapIncidentDto> _incidents = new();
    private List<MapTeamDto> _teams = new();

    private Guid? _selectedIncidentId;
    private bool _isSidebarOpen = false;
    private Guid? _quickDispatchIncidentId;
    private bool _isQuickDispatchOpen = false;
    private string _selectedTileProvider = "OpenStreetMap";
    private bool _isBoundaryLocked = false;
    private bool _hasHandledTargetLocation = false;

    private int _activeIncidentsCount => _incidents.Count(i => i.Status is not ("Resolved" or "Canceled"));
    private int _activeTeamsCount => _teams.Count(t => t.Status == "Idle");
    private int _resolvedIncidentsCount => _incidents.Count(i => i.Status is "Resolved" or "Canceled");
    
    private async Task HandleToggleResolvedIncidents(bool visible)
    {
        if (_mapRef != null)
        {
            await _mapRef.ToggleResolvedIncidentsLayerAsync(visible);
        }
    }

    protected override async Task OnInitializedAsync()
    {
        await LoadSettingsPreferencesAsync();
        await LoadMapDataAsync();
        SubscribeToHubEvents();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!_hasHandledTargetLocation && _mapRef != null)
        {
            if (TargetIncidentId.HasValue)
            {
                _hasHandledTargetLocation = true;
                _selectedIncidentId = TargetIncidentId.Value;
                _isSidebarOpen = true;

                if (TargetLat.HasValue && TargetLng.HasValue)
                {
                    await _mapRef.PanToLocationAsync(TargetLat.Value, TargetLng.Value, 17);
                }
                else
                {
                    await _mapRef.FocusMarkerByIdAsync("incident", TargetIncidentId.Value, 17);
                }
            }
            else if (TargetLat.HasValue && TargetLng.HasValue)
            {
                _hasHandledTargetLocation = true;
                await _mapRef.PanToLocationAsync(TargetLat.Value, TargetLng.Value, 17);
            }
            else if (TargetTeamId.HasValue)
            {
                _hasHandledTargetLocation = true;
                await _mapRef.FocusMarkerByIdAsync("team", TargetTeamId.Value, 17);
            }
        }
    }

    private async Task LoadSettingsPreferencesAsync()
    {
        try
        {
            var prefs = await SettingsService.GetPreferencesAsync();
            if (!string.IsNullOrWhiteSpace(prefs.TileProvider))
            {
                _selectedTileProvider = prefs.TileProvider;
            }
            _isBoundaryLocked = prefs.LockMapToFacilityBoundary;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[MapPage] Failed to load preferences: {ex.Message}");
        }
    }

    private async Task LoadMapDataAsync()
    {
        try
        {
            var incidentsTask = Http.GetFromJsonAsync<ApiResponse<PagedResult<MapIncidentDto>>>("api/v1/incidents?pageSize=1000");
            var teamsTask = Http.GetFromJsonAsync<ApiResponse<PagedResult<MapTeamDto>>>("api/v1/teams?pageSize=100");

            await Task.WhenAll(incidentsTask, teamsTask);

            var incidentsRes = await incidentsTask;
            var teamsRes = await teamsTask;

            if (incidentsRes?.Data?.Items != null)
            {
                _incidents = incidentsRes.Data.Items;
            }

            if (teamsRes?.Data?.Items != null)
            {
                _teams = teamsRes.Data.Items;
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[MapPage] Failed to load data: {ex.Message}");
        }
    }

    // ──────────────────────────────────────────────
    // SignalR Real-time Event Subscriptions
    // ──────────────────────────────────────────────

    private void SubscribeToHubEvents()
    {
        IncidentHub.OnNewIncidentReceived += HandleNewIncidentReceived;
        IncidentHub.OnIncidentStatusChanged += HandleIncidentStatusChanged;
        IncidentHub.OnIncidentUpdated += HandleIncidentUpdatedRealtime;
        IncidentHub.OnTeamDispatched += HandleTeamDispatched;
        LocationHub.OnTeamLocationUpdated += HandleTeamLocationUpdated;
    }

    private void UnsubscribeFromHubEvents()
    {
        IncidentHub.OnNewIncidentReceived -= HandleNewIncidentReceived;
        IncidentHub.OnIncidentStatusChanged -= HandleIncidentStatusChanged;
        IncidentHub.OnIncidentUpdated -= HandleIncidentUpdatedRealtime;
        IncidentHub.OnTeamDispatched -= HandleTeamDispatched;
        LocationHub.OnTeamLocationUpdated -= HandleTeamLocationUpdated;
    }

    private void HandleNewIncidentReceived(object? sender, NewIncidentReceivedEventArgs e)
    {
        var existing = _incidents.FirstOrDefault(i => i.Id == e.Id);
        if (existing == null)
        {
            _incidents.Add(new MapIncidentDto
            {
                Id = e.Id,
                Category = e.Category,
                EmergencyCode = e.EmergencyCode,
                ReporterFullName = e.ReporterFullName ?? "Unknown",
                Status = "Open",
                Latitude = (decimal)e.Latitude,
                Longitude = (decimal)e.Longitude,
                CreatedAt = e.CreatedAt
            });
            InvokeAsync(StateHasChanged);
        }
    }

    private void HandleIncidentStatusChanged(object? sender, IncidentStatusChangedEventArgs e)
    {
        var target = _incidents.FirstOrDefault(i => i.Id == e.IncidentId);
        if (target != null)
        {
            target.Status = e.Status;
            InvokeAsync(StateHasChanged);
        }
    }

    private async void HandleIncidentUpdatedRealtime(object? sender, IncidentUpdatedEventArgs e)
    {
        await LoadMapDataAsync();
        await InvokeAsync(StateHasChanged);
    }

    private void HandleTeamDispatched(object? sender, TeamDispatchedEventArgs e)
    {
        var team = _teams.FirstOrDefault(t => t.Id == e.TeamId);
        if (team != null)
        {
            team.Status = "Forwarded";
        }

        var incident = _incidents.FirstOrDefault(i => i.Id == e.IncidentId);
        if (incident != null)
        {
            incident.Status = "Assigned";
            incident.AssignedTeamName = team?.TeamName ?? "Assigned Team";
        }

        InvokeAsync(StateHasChanged);
    }

    private void HandleTeamLocationUpdated(object? sender, TeamLocationUpdatedEventArgs e)
    {
        var team = _teams.FirstOrDefault(t => t.Id == e.TeamId);
        if (team != null)
        {
            team.CurrentLatitude = (decimal)e.Latitude;
            team.CurrentLongitude = (decimal)e.Longitude;
            team.UpdatedAt = e.UpdatedAt;
            InvokeAsync(StateHasChanged);
        }
    }

    // ──────────────────────────────────────────────
    // Map Marker & Action Callbacks
    // ──────────────────────────────────────────────

    private void HandleIncidentDetail(Guid incidentId)
    {
        _selectedIncidentId = incidentId;
        _isSidebarOpen = true;
    }

    private void HandleAssignTeam(Guid incidentId)
    {
        _quickDispatchIncidentId = incidentId;
        _isQuickDispatchOpen = true;
    }

    private void HandleCloseQuickDispatch()
    {
        _isQuickDispatchOpen = false;
        _quickDispatchIncidentId = null;
    }

    private async Task HandleIncidentFocused(Guid incidentId)
    {
        if (_mapRef != null)
        {
            await _mapRef.FocusMarkerByIdAsync("Incident", incidentId, 17);
        }
    }

    private async Task HandleTeamFocused(Guid teamId)
    {
        if (_mapRef != null)
        {
            await _mapRef.FocusMarkerByIdAsync("Team", teamId, 17);
        }
    }


    private async Task HandleToggleIncidents(bool visible)
    {
        if (_mapRef != null)
        {
            await _mapRef.ToggleIncidentsLayerAsync(visible);
        }
    }

    private async Task HandleToggleTeams(bool visible)
    {
        if (_mapRef != null)
        {
            await _mapRef.ToggleTeamsLayerAsync(visible);
        }
    }

    private async Task HandleTileProviderChanged(string provider)
    {
        _selectedTileProvider = provider;
        if (_mapRef != null)
        {
            await _mapRef.ChangeTileLayerAsync(provider);
        }
    }

    private async Task HandleResetView()
    {
        if (_mapRef != null)
        {
            await _mapRef.ResetViewAsync();
        }
    }

    private async Task HandleToggleBoundaryLock(bool enabled)
    {
        _isBoundaryLocked = enabled;
        if (_mapRef != null)
        {
            await _mapRef.SetBoundaryLockAsync(enabled);
        }

        var prefs = await SettingsService.GetPreferencesAsync();
        prefs.LockMapToFacilityBoundary = enabled;
        await SettingsService.SavePreferencesAsync(prefs);
    }

    private async Task HandleIncidentAssigned(Guid incidentId)
    {
        await LoadMapDataAsync();
        if (_mapRef != null)
        {
            await _mapRef.LoadIncidentsAsync();
            await _mapRef.LoadTeamsAsync();
        }
    }

    private async Task HandleIncidentUpdated(IncidentDetailViewModel incident)
    {
        await LoadMapDataAsync();
        if (_mapRef != null)
        {
            await _mapRef.LoadIncidentsAsync();
        }
    }

    // ──────────────────────────────────────────────
    // Resource Cleanup
    // ──────────────────────────────────────────────

    public void Dispose()
    {
        UnsubscribeFromHubEvents();
    }
}
