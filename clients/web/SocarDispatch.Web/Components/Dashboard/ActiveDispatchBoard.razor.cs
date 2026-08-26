using Microsoft.AspNetCore.Components;
using SocarDispatch.Web.Events;
using SocarDispatch.Web.Models.Dashboard;
using SocarDispatch.Web.Models.Dispatch;
using SocarDispatch.Web.Services;
using SocarDispatch.Web.Services.SignalR;

namespace SocarDispatch.Web.Components.Dashboard;

public partial class ActiveDispatchBoard : ComponentBase, IDisposable
{
    [Inject] private IIncidentService IncidentService { get; set; } = default!;
    [Inject] private ITeamService TeamService { get; set; } = default!;
    [Inject] private IIncidentHubClient IncidentHub { get; set; } = default!;
    [Inject] private ILocationHubClient LocationHub { get; set; } = default!;

    [Parameter] public EventCallback<Guid> OnIncidentSelected { get; set; }
    [Parameter] public EventCallback<Guid> OnQuickDispatchRequested { get; set; }

    private List<ActiveDispatchRowDto> _activeRows = new();
    private Dictionary<Guid, TeamDto> _teamsMap = new();
    private readonly HashSet<Guid> _animatingRowIds = new();
    
    private bool _isLoading = true;
    private string? _errorMessage;
    private string _searchTerm = string.Empty;
    private string _filterStatus = "All"; // "All", "Unassigned", "Assigned"
    private string _sortBy = "Severity"; // "Severity", "TimeDesc", "TimeAsc", "Status"

    protected override async Task OnInitializedAsync()
    {
        await LoadBoardDataAsync();

        // SignalR Event Subscriptions
        IncidentHub.OnNewIncidentReceived += HandleNewIncident;
        IncidentHub.OnIncidentStatusChanged += HandleIncidentStatusChanged;
        IncidentHub.OnIncidentUpdated += HandleIncidentUpdated;
        IncidentHub.OnTeamDispatched += HandleTeamDispatched;
        IncidentHub.OnMemberStatusChanged += HandleMemberStatusChanged;
    }

    public async Task LoadBoardDataAsync()
    {
        _isLoading = true;
        _errorMessage = null;

        try
        {
            var incidentsTask = IncidentService.GetActiveIncidentsAsync();
            var teamsTask = TeamService.GetTeamsAsync();

            await Task.WhenAll(incidentsTask, teamsTask);

            var incidentsRes = await incidentsTask;
            var teamsRes = await teamsTask;

            if (teamsRes?.Success == true && teamsRes.Data != null)
            {
                _teamsMap = teamsRes.Data.ToDictionary(t => t.Id, t => t);
            }

            if (incidentsRes?.Success == true && incidentsRes.Data != null)
            {
                _activeRows = incidentsRes.Data
                    .Where(i => i.Status is "Open" or "Assigned")
                    .Select(i => MapToRowDto(i))
                    .ToList();
            }
            else
            {
                _errorMessage = incidentsRes?.Message ?? "Failed to load active incidents.";
            }
        }
        catch (Exception ex)
        {
            _errorMessage = $"Error loading dispatch board: {ex.Message}";
        }
        finally
        {
            _isLoading = false;
        }
    }

    private ActiveDispatchRowDto MapToRowDto(IncidentDetailViewModel incident)
    {
        var row = new ActiveDispatchRowDto
        {
            IncidentId = incident.Id,
            Category = incident.Category,
            EmergencyCode = incident.EmergencyCode,
            Description = incident.Description,
            Status = incident.Status,
            Latitude = incident.Latitude,
            Longitude = incident.Longitude,
            CreatedAt = incident.CreatedAt,
            AssignedAt = incident.AssignedAt,
            ReporterFullName = incident.ReporterFullName,
            AssignedTeamId = incident.AssignedTeamId,
            AssignedTeamName = incident.AssignedTeamName
        };

        if (row.AssignedTeamId.HasValue && _teamsMap.TryGetValue(row.AssignedTeamId.Value, out var team))
        {
            row.AssignedTeamName = team.TeamName;
            row.TeamLeaderName = team.LeaderFullName;
            row.TeamStatus = team.Status;
            row.TeamMemberCount = team.Members?.Count ?? 0;
        }

        return row;
    }

    // Filtered and Sorted Row Pipeline
    private IEnumerable<ActiveDispatchRowDto> FilteredRows
    {
        get
        {
            var query = _activeRows.AsEnumerable();

            // Status Filter
            if (_filterStatus == "Unassigned")
            {
                query = query.Where(r => !r.IsAssigned);
            }
            else if (_filterStatus == "Assigned")
            {
                query = query.Where(r => r.IsAssigned);
            }

            // Search Filter
            if (!string.IsNullOrWhiteSpace(_searchTerm))
            {
                var term = _searchTerm.Trim().ToLowerInvariant();
                query = query.Where(r => 
                    r.Category.ToLowerInvariant().Contains(term) ||
                    r.EmergencyCode.ToLowerInvariant().Contains(term) ||
                    (!string.IsNullOrEmpty(r.AssignedTeamName) && r.AssignedTeamName.ToLowerInvariant().Contains(term)) ||
                    (!string.IsNullOrEmpty(r.ReporterFullName) && r.ReporterFullName.ToLowerInvariant().Contains(term)) ||
                    (!string.IsNullOrEmpty(r.Description) && r.Description.ToLowerInvariant().Contains(term))
                );
            }

            // Sorting
            query = _sortBy switch
            {
                "Severity" => query.OrderBy(r => r.SeverityRank).ThenByDescending(r => r.CreatedAt),
                "TimeDesc" => query.OrderByDescending(r => r.CreatedAt),
                "TimeAsc" => query.OrderBy(r => r.CreatedAt),
                "Status" => query.OrderBy(r => r.IsAssigned ? 1 : 0).ThenBy(r => r.SeverityRank),
                _ => query.OrderBy(r => r.SeverityRank)
            };

            return query;
        }
    }

    // Interactions
    private async Task HandleRowClick(Guid incidentId)
    {
        if (OnIncidentSelected.HasDelegate)
        {
            await OnIncidentSelected.InvokeAsync(incidentId);
        }
    }

    private async Task HandleQuickDispatchClick(Guid incidentId)
    {
        if (OnQuickDispatchRequested.HasDelegate)
        {
            await OnQuickDispatchRequested.InvokeAsync(incidentId);
        }
    }

    // SignalR Real-Time Handlers
    private async void HandleNewIncident(object? sender, NewIncidentReceivedEventArgs e)
    {
        await InvokeAsync(async () =>
        {
            var newRow = new ActiveDispatchRowDto
            {
                IncidentId = e.Id,
                Category = e.Category,
                EmergencyCode = e.EmergencyCode,
                Description = e.Description,
                ReporterFullName = e.ReporterFullName ?? "Field Reporter",
                Status = "Open",
                Latitude = (decimal)e.Latitude,
                Longitude = (decimal)e.Longitude,
                CreatedAt = e.CreatedAt
            };

            _activeRows.Insert(0, newRow);
            _animatingRowIds.Add(e.Id);
            StateHasChanged();

            await Task.Delay(3500);
            _animatingRowIds.Remove(e.Id);
            StateHasChanged();
        });
    }

    private async void HandleIncidentStatusChanged(object? sender, IncidentStatusChangedEventArgs e)
    {
        await InvokeAsync(() =>
        {
            if (e.Status is "Resolved" or "Canceled")
            {
                _activeRows.RemoveAll(r => r.IncidentId == e.IncidentId);
            }
            else
            {
                var row = _activeRows.FirstOrDefault(r => r.IncidentId == e.IncidentId);
                if (row != null)
                {
                    row.Status = e.Status;
                }
            }
            StateHasChanged();
        });
    }

    private async void HandleIncidentUpdated(object? sender, IncidentUpdatedEventArgs e)
    {
        await InvokeAsync(() =>
        {
            var row = _activeRows.FirstOrDefault(r => r.IncidentId == e.IncidentId);
            if (row != null)
            {
                row.Category = e.Category;
                row.EmergencyCode = e.EmergencyCode;
                row.Description = e.Description;
                row.Latitude = (decimal)e.Latitude;
                row.Longitude = (decimal)e.Longitude;
                StateHasChanged();
            }
        });
    }

    private async void HandleTeamDispatched(object? sender, TeamDispatchedEventArgs e)
    {
        await InvokeAsync(() =>
        {
            var row = _activeRows.FirstOrDefault(r => r.IncidentId == e.IncidentId);
            if (row != null)
            {
                row.Status = "Assigned";
                row.AssignedTeamId = e.TeamId;
                row.AssignedAt = e.AssignedAt;

                if (_teamsMap.TryGetValue(e.TeamId, out var team))
                {
                    row.AssignedTeamName = team.TeamName;
                    row.TeamLeaderName = team.LeaderFullName;
                    row.TeamStatus = "Forwarded";
                    row.TeamMemberCount = team.Members?.Count ?? 0;
                }
                else
                {
                    row.AssignedTeamName = "Assigned Team";
                    row.TeamStatus = "Forwarded";
                }

                StateHasChanged();
            }
        });
    }

    private async void HandleMemberStatusChanged(object? sender, MemberStatusChangedEventArgs e)
    {
        await InvokeAsync(() =>
        {
            StateHasChanged();
        });
    }

    // Helper UI Class Formatters
    private string GetEmergencyCodeBadgeClass(string? code)
    {
        var c = (code ?? string.Empty).ToLowerInvariant();
        if (c.Contains("red") || c.Contains("kirmizi") || c.Contains("1")) return "badge-code-red";
        if (c.Contains("orange") || c.Contains("turuncu") || c.Contains("2")) return "badge-code-orange";
        if (c.Contains("yellow") || c.Contains("sari") || c.Contains("3")) return "badge-code-yellow";
        if (c.Contains("green") || c.Contains("yesil") || c.Contains("4")) return "badge-code-green";
        return "badge-code-default";
    }

    private string GetTeamStatusBadgeClass(string? status)
    {
        var s = (status ?? "idle").ToLowerInvariant();
        return s switch
        {
            "idle" => "badge-team-idle",
            "forwarded" => "badge-team-forwarded",
            "onscene" => "badge-team-onscene",
            "busy" => "badge-team-busy",
            _ => "badge-team-idle"
        };
    }

    public void Dispose()
    {
        IncidentHub.OnNewIncidentReceived -= HandleNewIncident;
        IncidentHub.OnIncidentStatusChanged -= HandleIncidentStatusChanged;
        IncidentHub.OnIncidentUpdated -= HandleIncidentUpdated;
        IncidentHub.OnTeamDispatched -= HandleTeamDispatched;
        IncidentHub.OnMemberStatusChanged -= HandleMemberStatusChanged;
    }
}
