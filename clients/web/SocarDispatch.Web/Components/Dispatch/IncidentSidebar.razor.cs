// Components/Dispatch/IncidentSidebar.razor.cs
using Microsoft.AspNetCore.Components;
using SocarDispatch.Web.Events;
using SocarDispatch.Web.Models.Dispatch;
using SocarDispatch.Web.Services;
using SocarDispatch.Web.Services.SignalR;

namespace SocarDispatch.Web.Components.Dispatch;

public partial class IncidentSidebar : ComponentBase, IDisposable
{
    [Inject] private IIncidentService IncidentService { get; set; } = default!;
    [Inject] private IAssignmentService AssignmentService { get; set; } = default!;
    [Inject] private ITeamService TeamService { get; set; } = default!;
    [Inject] private IToastService ToastService { get; set; } = default!;
    [Inject] private IIncidentHubClient IncidentHub { get; set; } = default!;
    [Inject] private ILocationHubClient LocationHub { get; set; } = default!;

    [Parameter] public Guid? IncidentId { get; set; }
    [Parameter] public bool IsOpen { get; set; }
    [Parameter] public EventCallback<bool> IsOpenChanged { get; set; }
    [Parameter] public EventCallback<Guid> OnIncidentAssigned { get; set; }

    private IncidentDetailViewModel? _incident;
    private List<TeamDto> _teams = new();
    private TeamDto? _selectedTeam;

    private bool _isLoading = false;
    private bool _isDescExpanded = true;
    private bool _isConfirmationModalOpen = false;
    private bool _isDispatching = false;
    private Guid? _lastLoadedIncidentId;

    protected override async Task OnParametersSetAsync()
    {
        if (IsOpen && IncidentId.HasValue && IncidentId.Value != _lastLoadedIncidentId)
        {
            _lastLoadedIncidentId = IncidentId.Value;
            await LoadIncidentAndTeamsAsync(IncidentId.Value);
        }
    }

    protected override void OnInitialized()
    {
        // 1. Subscribe to real-time SignalR hub events
        LocationHub.OnTeamLocationUpdated += HandleTeamLocationUpdated;
        IncidentHub.OnTeamDispatched += HandleTeamDispatched;
        IncidentHub.OnIncidentStatusChanged += HandleIncidentStatusChanged;
    }

    private async Task LoadIncidentAndTeamsAsync(Guid incidentId)
    {
        _isLoading = true;
        _selectedTeam = null;
        try
        {
            var incidentTask = IncidentService.GetIncidentByIdAsync(incidentId);
            var teamsTask = TeamService.GetTeamsAsync();

            await Task.WhenAll(incidentTask, teamsTask);

            var incidentResult = await incidentTask;
            var teamsResult = await teamsTask;

            if (incidentResult?.Success == true && incidentResult.Data != null)
            {
                _incident = incidentResult.Data;
            }

            if (teamsResult?.Success == true && teamsResult.Data != null)
            {
                _teams = teamsResult.Data;
            }
        }
        catch (Exception ex)
        {
            ToastService.Show("Error", $"Failed to load incident details: {ex.Message}", ToastLevel.Danger);
        }
        finally
        {
            _isLoading = false;
        }
    }

    // ──────────────────────────────────────────────
    // SignalR Real-Time Event Handlers
    // ──────────────────────────────────────────────

    /// <summary>
    /// Updates team GPS coordinates dynamically without clearing operator selection.
    /// </summary>
    private void HandleTeamLocationUpdated(object? sender, TeamLocationUpdatedEventArgs e)
    {
        var team = _teams.FirstOrDefault(t => t.Id == e.TeamId);
        if (team != null)
        {
            team.CurrentLatitude = (decimal)e.Latitude;
            team.CurrentLongitude = (decimal)e.Longitude;
            team.UpdatedAt = e.UpdatedAt;

            // Preserve active selection state with updated coordinates
            if (_selectedTeam != null && _selectedTeam.Id == e.TeamId)
            {
                _selectedTeam.CurrentLatitude = (decimal)e.Latitude;
                _selectedTeam.CurrentLongitude = (decimal)e.Longitude;
                _selectedTeam.UpdatedAt = e.UpdatedAt;
            }

            InvokeAsync(StateHasChanged);
        }
    }

    /// <summary>
    /// Handles response unit status updates when a team is dispatched.
    /// </summary>
    private void HandleTeamDispatched(object? sender, TeamDispatchedEventArgs e)
    {
        var team = _teams.FirstOrDefault(t => t.Id == e.TeamId);
        if (team != null)
        {
            team.Status = "Forwarded";
        }

        // If the currently inspected incident is assigned
        if (_incident != null && _incident.Id == e.IncidentId)
        {
            _incident.Status = "Assigned";
            _incident.AssignedTeamId = e.TeamId;
            _incident.AssignedTeamName = team?.TeamName ?? "Assigned Team";
            _selectedTeam = null;
            _isConfirmationModalOpen = false;
        }
        // If the selected team was assigned to another incident elsewhere
        else if (_selectedTeam != null && _selectedTeam.Id == e.TeamId)
        {
            _selectedTeam = null;
            ToastService.Show(
                "Team Status Changed", 
                $"{team?.TeamName ?? "Selected team"} was assigned to another incident and is no longer available.", 
                ToastLevel.Warning);
        }

        InvokeAsync(StateHasChanged);
    }

    /// <summary>
    /// Handles incident status transitions (Assigned, Resolved, Canceled).
    /// </summary>
    private void HandleIncidentStatusChanged(object? sender, IncidentStatusChangedEventArgs e)
    {
        if (_incident != null && _incident.Id == e.IncidentId)
        {
            _incident.Status = e.Status;

            if (e.Status is "Resolved" or "Canceled")
            {
                ToastService.Show(
                    "Incident Status Updated", 
                    $"This incident was marked as {e.Status}.", 
                    ToastLevel.Info);
            }

            InvokeAsync(StateHasChanged);
        }
    }

    // ──────────────────────────────────────────────
    // User Actions
    // ──────────────────────────────────────────────
    private void HandleTeamSelected(TeamDto team)
    {
        _selectedTeam = team;
    }

    private void ToggleDescription()
    {
        _isDescExpanded = !_isDescExpanded;
    }

    private void OpenDispatchConfirmation()
    {
        if (_selectedTeam == null || _incident == null) return;
        _isConfirmationModalOpen = true;
    }

    private void CloseDispatchConfirmation()
    {
        _isConfirmationModalOpen = false;
    }

    private async Task ExecuteDispatchAsync(string? notes)
    {
        if (_incident == null || _selectedTeam == null) return;

        // Cache references to prevent race condition with incoming SignalR TeamDispatched events
        var teamToDispatch = _selectedTeam;
        var incidentToDispatch = _incident;

        _isDispatching = true;
        _isConfirmationModalOpen = false;

        try
        {
            var request = new DispatchRequestDto
            {
                IncidentId = incidentToDispatch.Id,
                TeamId = teamToDispatch.Id,
                Notes = notes
            };

            var result = await AssignmentService.AssignTeamAsync(request);

            if (result?.Success == true)
            {
                ToastService.Show(
                    "Team Dispatched", 
                    $"{teamToDispatch.TeamName} has been assigned to {incidentToDispatch.Category}.", 
                    ToastLevel.Success);

                incidentToDispatch.Status = "Assigned";
                incidentToDispatch.AssignedTeamId = teamToDispatch.Id;
                incidentToDispatch.AssignedTeamName = teamToDispatch.TeamName;
                _selectedTeam = null;

                await OnIncidentAssigned.InvokeAsync(incidentToDispatch.Id);
            }
            else
            {
                ToastService.Show("Dispatch Failed", result?.Message ?? "Could not assign team.", ToastLevel.Danger);
            }
        }
        catch (Exception ex)
        {
            ToastService.Show("Dispatch Error", ex.Message, ToastLevel.Danger);
        }
        finally
        {
            _isDispatching = false;
        }
    }


    private async Task CloseSidebar()
    {
        IsOpen = false;
        await IsOpenChanged.InvokeAsync(false);
    }

    // ──────────────────────────────────────────────
    // Helper Methods
    // ──────────────────────────────────────────────
    private string GetInitials(string fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName)) return "OP";
        var parts = fullName.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length > 1 
            ? $"{parts[0][0]}{parts[1][0]}".ToUpper() 
            : $"{parts[0][0]}".ToUpper();
    }

    private string GetEmergencyCodeBadgeClass(string code)
    {
        var c = (code ?? string.Empty).ToLowerInvariant();
        if (c.Contains("1") || c.Contains("red") || c.Contains("kirmizi")) return "badge-code-1";
        if (c.Contains("2") || c.Contains("orange") || c.Contains("turuncu")) return "badge-code-2";
        return "badge-code-3";
    }

    private string GetStatusBadgeClass(string status)
    {
        return (status ?? string.Empty).ToLowerInvariant() switch
        {
            "open" => "badge-status-open",
            "assigned" => "badge-status-assigned",
            "resolved" => "badge-status-resolved",
            "canceled" => "badge-status-canceled",
            _ => "badge-status-open"
        };
    }

    public void Dispose()
    {
        LocationHub.OnTeamLocationUpdated -= HandleTeamLocationUpdated;
        IncidentHub.OnTeamDispatched -= HandleTeamDispatched;
        IncidentHub.OnIncidentStatusChanged -= HandleIncidentStatusChanged;
    }
}
