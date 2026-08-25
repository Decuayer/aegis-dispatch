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
    [Parameter] public EventCallback<IncidentDetailViewModel> OnIncidentUpdated { get; set; }

    private IncidentDetailViewModel? _incident;
    private List<TeamDto> _teams = new();
    private TeamDto? _selectedTeam;

    private bool _isLoading = false;
    private bool _isDescExpanded = true;
    private bool _isConfirmationModalOpen = false;
    private bool _isDispatching = false;
    private bool _isEditMode = false;
    private Guid? _lastLoadedIncidentId;

    protected override async Task OnParametersSetAsync()
    {
        if (IsOpen && IncidentId.HasValue && IncidentId.Value != _lastLoadedIncidentId)
        {
            _lastLoadedIncidentId = IncidentId.Value;
            _isEditMode = false;
            await LoadIncidentAndTeamsAsync(IncidentId.Value);
        }
    }

    protected override void OnInitialized()
    {
        // Subscribe to real-time SignalR hub events
        LocationHub.OnTeamLocationUpdated += HandleTeamLocationUpdated;
        IncidentHub.OnTeamDispatched += HandleTeamDispatched;
        IncidentHub.OnIncidentStatusChanged += HandleIncidentStatusChanged;
        IncidentHub.OnIncidentUpdated += HandleIncidentUpdated;
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

    private void HandleTeamLocationUpdated(object? sender, TeamLocationUpdatedEventArgs e)
    {
        var team = _teams.FirstOrDefault(t => t.Id == e.TeamId);
        if (team != null)
        {
            team.CurrentLatitude = (decimal)e.Latitude;
            team.CurrentLongitude = (decimal)e.Longitude;
            team.UpdatedAt = e.UpdatedAt;

            if (_selectedTeam != null && _selectedTeam.Id == e.TeamId)
            {
                _selectedTeam.CurrentLatitude = (decimal)e.Latitude;
                _selectedTeam.CurrentLongitude = (decimal)e.Longitude;
                _selectedTeam.UpdatedAt = e.UpdatedAt;
            }

            InvokeAsync(StateHasChanged);
        }
    }

    private void HandleTeamDispatched(object? sender, TeamDispatchedEventArgs e)
    {
        var team = _teams.FirstOrDefault(t => t.Id == e.TeamId);
        if (team != null)
        {
            team.Status = "Forwarded";
        }

        if (_incident != null && _incident.Id == e.IncidentId)
        {
            _incident.Status = "Assigned";
            _incident.AssignedTeamId = e.TeamId;
            _incident.AssignedTeamName = team?.TeamName ?? "Assigned Team";
            _selectedTeam = null;
            _isConfirmationModalOpen = false;
        }
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

    private void HandleIncidentUpdated(object? sender, IncidentUpdatedEventArgs e)
    {
        if (_incident != null && _incident.Id == e.IncidentId && !_isEditMode)
        {
            _incident.Category = e.Category;
            _incident.EmergencyCode = e.EmergencyCode;
            _incident.Description = e.Description;
            _incident.Latitude = (decimal)e.Latitude;
            _incident.Longitude = (decimal)e.Longitude;

            InvokeAsync(StateHasChanged);
        }
    }

    // ──────────────────────────────────────────────
    // User Actions
    // ──────────────────────────────────────────────
    private async Task HandleIncidentSaved(IncidentDetailViewModel updatedIncident)
    {
        _incident = updatedIncident;
        _isEditMode = false;
        await OnIncidentUpdated.InvokeAsync(updatedIncident);
    }

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
                await OnIncidentUpdated.InvokeAsync(incidentToDispatch);
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

    private bool _isUpdatingStatus = false;
    private bool _isStatusModalOpen = false;
    private string _targetStatus = string.Empty;
    private string _statusModalTitle = string.Empty;
    private string _statusNotes = string.Empty;

    private void OpenResolveModal()
    {
        _targetStatus = "Resolved";
        _statusModalTitle = "Resolve Emergency Incident";
        _statusNotes = string.Empty;
        _isStatusModalOpen = true;
    }

    private void OpenCancelModal()
    {
        _targetStatus = "Canceled";
        _statusModalTitle = "Cancel Incident";
        _statusNotes = string.Empty;
        _isStatusModalOpen = true;
    }

    private async Task ReopenIncidentAsync()
    {
        await ChangeIncidentStatusAsync("Open", "Incident reopened by operator.");
    }

    private async Task ConfirmStatusChangeAsync()
    {
        _isStatusModalOpen = false;
        await ChangeIncidentStatusAsync(_targetStatus, _statusNotes);
    }

    private async Task ChangeIncidentStatusAsync(string targetStatus, string? notes)
    {
        if (_incident == null) return;

        var previousStatus = _incident.Status;
        var previousNotes = _incident.CompletionNotes;

        // Optimistic UI state update
        _incident.Status = targetStatus;
        _incident.CompletionNotes = notes;
        _isUpdatingStatus = true;

        try
        {
            var response = await IncidentService.ChangeIncidentStatusAsync(_incident.Id, new ChangeIncidentStatusRequestDto
            {
                Status = targetStatus,
                CompletionNotes = notes
            });

            if (response?.Success == true && response.Data != null)
            {
                _incident.Status = response.Data.Status;
                _incident.CompletionNotes = response.Data.CompletionNotes ?? notes;
                ToastService.Show("Status Updated", $"Incident status updated to {targetStatus}.", ToastLevel.Success);
                await OnIncidentAssigned.InvokeAsync(_incident.Id);
                await OnIncidentUpdated.InvokeAsync(_incident);
            }
            else
            {
                // Rollback on failure
                _incident.Status = previousStatus;
                _incident.CompletionNotes = previousNotes;
                ToastService.Show("Status Update Failed", response?.Message ?? "Could not update status.", ToastLevel.Danger);
            }
        }
        catch (Exception ex)
        {
            // Rollback on exception
            _incident.Status = previousStatus;
            _incident.CompletionNotes = previousNotes;
            ToastService.Show("Error", ex.Message, ToastLevel.Danger);
        }
        finally
        {
            _isUpdatingStatus = false;
        }
    }

    private async Task CloseSidebar()
    {
        _isEditMode = false;
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
        if (c.Contains("1") || c.Contains("red") || c.Contains("kirmizi")) return "badge-code-red";
        if (c.Contains("2") || c.Contains("orange") || c.Contains("turuncu")) return "badge-code-orange";
        if (c.Contains("3") || c.Contains("yellow") || c.Contains("sari")) return "badge-code-yellow";
        if (c.Contains("4") || c.Contains("green") || c.Contains("yesil")) return "badge-code-green";
        return "badge-code-default";
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
        IncidentHub.OnIncidentUpdated -= HandleIncidentUpdated;
    }
}
