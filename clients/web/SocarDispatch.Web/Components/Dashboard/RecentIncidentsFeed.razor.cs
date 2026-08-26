using Microsoft.AspNetCore.Components;
using SocarDispatch.Web.Events;
using SocarDispatch.Web.Models.Dispatch;
using SocarDispatch.Web.Services;
using SocarDispatch.Web.Services.SignalR;

namespace SocarDispatch.Web.Components.Dashboard;

public partial class RecentIncidentsFeed : ComponentBase, IDisposable
{
    [Inject] private IIncidentService IncidentService { get; set; } = default!;
    [Inject] private IIncidentHubClient IncidentHub { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;

    [Parameter] public int MaxItems { get; set; } = 15;
    [Parameter] public EventCallback<Guid> OnIncidentSelected { get; set; }
    [Parameter] public string Class { get; set; } = string.Empty;

    private List<IncidentDetailViewModel> _recentIncidents = new();
    private readonly HashSet<Guid> _animatingIncidentIds = new();
    private bool _isLoading = true;
    private string? _errorMessage;

    protected override async Task OnInitializedAsync()
    {
        await LoadRecentIncidentsAsync();

        // Subscribe to real-time SignalR events
        IncidentHub.OnNewIncidentReceived += HandleNewIncident;
        IncidentHub.OnIncidentStatusChanged += HandleIncidentStatusChanged;
        IncidentHub.OnIncidentUpdated += HandleIncidentUpdated;
    }

    public async Task LoadRecentIncidentsAsync()
    {
        _isLoading = true;
        _errorMessage = null;

        try
        {
            var response = await IncidentService.GetAllIncidentsAsync();
            if (response != null && response.Success && response.Data != null)
            {
                _recentIncidents = response.Data
                    .OrderByDescending(i => i.CreatedAt)
                    .Take(MaxItems)
                    .ToList();
            }
            else
            {
                _errorMessage = response?.Message ?? "Failed to load recent incidents.";
            }
        }
        catch (Exception ex)
        {
            _errorMessage = $"Error loading recent incidents: {ex.Message}";
        }
        finally
        {
            _isLoading = false;
        }
    }

    private async Task HandleCardClick(Guid incidentId)
    {
        if (OnIncidentSelected.HasDelegate)
        {
            await OnIncidentSelected.InvokeAsync(incidentId);
        }
    }

    private void NavigateToIncidents()
    {
        Navigation.NavigateTo("/incidents");
    }

    private async void HandleNewIncident(object? sender, NewIncidentReceivedEventArgs e)
    {
        await InvokeAsync(async () =>
        {
            var newIncident = new IncidentDetailViewModel
            {
                Id = e.Id,
                Category = e.Category,
                EmergencyCode = e.EmergencyCode,
                ReporterFullName = e.ReporterFullName ?? string.Empty,
                Description = e.Description,
                Latitude = (decimal)e.Latitude,
                Longitude = (decimal)e.Longitude,
                CreatedAt = e.CreatedAt,
                Status = "Open"
            };

            // Prepend new incident and maintain list capacity
            _recentIncidents.Insert(0, newIncident);
            if (_recentIncidents.Count > MaxItems)
            {
                _recentIncidents.RemoveAt(_recentIncidents.Count - 1);
            }

            // Trigger temporary arrival animation
            _animatingIncidentIds.Add(e.Id);
            StateHasChanged();

            await Task.Delay(3500);
            _animatingIncidentIds.Remove(e.Id);
            StateHasChanged();
        });
    }

    private async void HandleIncidentStatusChanged(object? sender, IncidentStatusChangedEventArgs e)
    {
        await InvokeAsync(() =>
        {
            var incident = _recentIncidents.FirstOrDefault(i => i.Id == e.IncidentId);
            if (incident != null)
            {
                incident.Status = e.Status;
                StateHasChanged();
            }
        });
    }

    private async void HandleIncidentUpdated(object? sender, IncidentUpdatedEventArgs e)
    {
        await InvokeAsync(() =>
        {
            var incident = _recentIncidents.FirstOrDefault(i => i.Id == e.IncidentId);
            if (incident != null)
            {
                incident.Category = e.Category;
                incident.EmergencyCode = e.EmergencyCode;
                incident.Description = e.Description;
                incident.Latitude = (decimal)e.Latitude;
                incident.Longitude = (decimal)e.Longitude;
                StateHasChanged();
            }
        });
    }

    private string GetSeverityBorderClass(string? code)
    {
        var c = (code ?? string.Empty).ToLowerInvariant();
        if (c.Contains("red") || c.Contains("kirmizi") || c.Contains("1")) return "severity-red";
        if (c.Contains("orange") || c.Contains("turuncu") || c.Contains("2")) return "severity-orange";
        if (c.Contains("yellow") || c.Contains("sari") || c.Contains("3")) return "severity-yellow";
        if (c.Contains("green") || c.Contains("yesil") || c.Contains("4")) return "severity-green";
        return string.Empty;
    }

    private string GetEmergencyCodeBadgeClass(string? code)
    {
        var c = (code ?? string.Empty).ToLowerInvariant();
        if (c.Contains("red") || c.Contains("kirmizi") || c.Contains("1")) return "badge-code-red";
        if (c.Contains("orange") || c.Contains("turuncu") || c.Contains("2")) return "badge-code-orange";
        if (c.Contains("yellow") || c.Contains("sari") || c.Contains("3")) return "badge-code-yellow";
        if (c.Contains("green") || c.Contains("yesil") || c.Contains("4")) return "badge-code-green";
        return "badge-code-default";
    }

    private string GetStatusBadgeClass(string? status)
    {
        var s = (status ?? string.Empty).ToLowerInvariant();
        return s switch
        {
            "open" => "badge-status-open",
            "assigned" => "badge-status-assigned",
            "resolved" => "badge-status-resolved",
            "canceled" => "badge-status-canceled",
            _ => "badge-status-default"
        };
    }

    private string GetInitials(string? fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName)) return "U";
        var parts = fullName.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 1) return parts[0][0].ToString().ToUpper();
        return $"{parts[0][0]}{parts[^1][0]}".ToUpper();
    }

    public void Dispose()
    {
        IncidentHub.OnNewIncidentReceived -= HandleNewIncident;
        IncidentHub.OnIncidentStatusChanged -= HandleIncidentStatusChanged;
        IncidentHub.OnIncidentUpdated -= HandleIncidentUpdated;
    }
}
