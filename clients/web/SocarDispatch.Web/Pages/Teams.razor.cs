using Microsoft.AspNetCore.Components;
using SocarDispatch.Web.Events;
using SocarDispatch.Web.Models.Common;
using SocarDispatch.Web.Models.Dispatch;
using SocarDispatch.Web.Services;
using SocarDispatch.Web.Services.SignalR;

namespace SocarDispatch.Web.Pages;

public partial class Teams : ComponentBase, IDisposable
{
    [Inject] private ITeamService TeamService { get; set; } = default!;
    [Inject] private ILocationHubClient LocationHub { get; set; } = default!;
    [Inject] private IIncidentHubClient IncidentHub { get; set; } = default!;
    [Inject] private IToastService ToastService { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;

    private List<TeamDto> _teams = new();
    private bool _isLoading = true;
    private string _selectedStatus = "All";
    private string _searchTerm = string.Empty;
    private Dictionary<string, int> _statusCounts = new(StringComparer.OrdinalIgnoreCase);

    // Server-Side Pagination
    private int CurrentPage { get; set; } = 1;
    private int PageSize { get; set; } = 25;
    private int _totalCount = 0;

    // Debounce CancellationTokenSource
    private CancellationTokenSource? _searchCts;

    // Modal States
    private bool _isDetailModalOpen;
    private bool _isCreateModalOpen;
    private TeamDto? _selectedTeam;

    protected override async Task OnInitializedAsync()
    {
        // Subscribe to real-time SignalR events
        LocationHub.OnTeamLocationUpdated += HandleLocationUpdated;
        IncidentHub.OnTeamDispatched += HandleTeamDispatched;
        IncidentHub.OnMemberStatusChanged += HandleMemberStatusChanged;

        await LoadTeamsAsync();
        await LoadStatusCountsAsync();
    }

    public async Task LoadTeamsAsync(CancellationToken cancellationToken = default)
    {
        _isLoading = true;
        StateHasChanged();

        try
        {
            Guid? teamId = null;
            string? search = null;

            var trimmed = _searchTerm?.Trim();
            if (!string.IsNullOrWhiteSpace(trimmed))
            {
                // Exact GUID match or text search
                if (Guid.TryParse(trimmed, out var parsedGuid))
                {
                    teamId = parsedGuid;
                }
                else
                {
                    search = trimmed;
                }
            }

            var statusFilter = _selectedStatus.Equals("All", StringComparison.OrdinalIgnoreCase)
                ? null
                : _selectedStatus;

            var response = await TeamService.GetTeamsAsync(
                pageNumber: CurrentPage,
                pageSize: PageSize,
                status: statusFilter,
                teamId: teamId,
                searchTerm: search,
                cancellationToken: cancellationToken);

            if (response != null && response.Success && response.Data != null)
            {
                _teams = response.Data.Items;
                _totalCount = response.Data.TotalCount;
                CurrentPage = response.Data.PageNumber;
            }
            else
            {
                _teams.Clear();
                _totalCount = 0;
                ToastService.ShowError(response?.Message ?? "Failed to load response teams.");
            }
        }
        catch (OperationCanceledException)
        {
            // Suppress cancelled search requests
            return;
        }
        catch (Exception ex)
        {
            ToastService.ShowError($"Error loading teams: {ex.Message}");
        }
        finally
        {
            _isLoading = false;
            StateHasChanged();
        }
    }

    private async Task LoadStatusCountsAsync()
    {
        try
        {
            var res = await TeamService.GetAllTeamsAsync();
            if (res?.Success == true && res.Data != null)
            {
                _statusCounts = res.Data
                    .GroupBy(t => t.Status, StringComparer.OrdinalIgnoreCase)
                    .ToDictionary(g => g.Key, g => g.Count(), StringComparer.OrdinalIgnoreCase);
                StateHasChanged();
            }
        }
        catch
        {
            // Ignore background stats load error
        }
    }

    private int GetStatusCount(string status)
    {
        if (status.Equals("All", StringComparison.OrdinalIgnoreCase))
        {
            return _statusCounts.Values.Sum();
        }
        return _statusCounts.TryGetValue(status, out var count) ? count : 0;
    }

    private async Task SetStatusFilter(string status)
    {
        _selectedStatus = status;
        CurrentPage = 1;
        await LoadTeamsAsync();
    }

    private async Task HandleSearchInput(ChangeEventArgs e)
    {
        _searchTerm = e.Value?.ToString() ?? string.Empty;
        CurrentPage = 1;

        _searchCts?.Cancel();
        _searchCts?.Dispose();
        _searchCts = new CancellationTokenSource();
        var token = _searchCts.Token;

        try
        {
            // Debounce for 350ms before querying backend
            await Task.Delay(350, token);
            await LoadTeamsAsync(token);
        }
        catch (OperationCanceledException)
        {
            // Request superseded by newer input
        }
    }

    private async Task HandlePageChanged(int newPage)
    {
        CurrentPage = newPage;
        await LoadTeamsAsync();
    }

    private async Task HandlePageSizeChanged(int newSize)
    {
        PageSize = newSize;
        CurrentPage = 1;
        await LoadTeamsAsync();
    }

    private async Task ResetFilters()
    {
        _selectedStatus = "All";
        _searchTerm = string.Empty;
        CurrentPage = 1;
        await LoadTeamsAsync();
    }

    private void HandleTeamSelected(TeamDto team)
    {
        _selectedTeam = team;
        _isDetailModalOpen = true;
    }

    private void CloseDetailModal()
    {
        _isDetailModalOpen = false;
    }

    private void OpenCreateModal()
    {
        _isCreateModalOpen = true;
    }

    private void CloseCreateModal()
    {
        _isCreateModalOpen = false;
    }

    private async Task HandleTeamCreated(TeamDto newTeam)
    {
        _isCreateModalOpen = false;
        _selectedTeam = newTeam;
        _isDetailModalOpen = true;
        await LoadTeamsAsync();
        await LoadStatusCountsAsync();
    }

    private async Task HandleTeamUpdated(TeamDto updatedTeam)
    {
        _selectedTeam = updatedTeam;
        await LoadTeamsAsync();
        await LoadStatusCountsAsync();
    }

    // Real-Time SignalR Event Handlers
    private void HandleLocationUpdated(object? sender, TeamLocationUpdatedEventArgs e)
    {
        InvokeAsync(() =>
        {
            var target = _teams.FirstOrDefault(t => t.Id == e.TeamId);
            if (target != null)
            {
                target.CurrentLatitude = (decimal)e.Latitude;
                target.CurrentLongitude = (decimal)e.Longitude;
                target.UpdatedAt = e.UpdatedAt;
                StateHasChanged();
            }
        });
    }

    private void HandleTeamDispatched(object? sender, TeamDispatchedEventArgs e)
    {
        InvokeAsync(async () =>
        {
            var target = _teams.FirstOrDefault(t => t.Id == e.TeamId);
            if (target != null)
            {
                target.Status = "Forwarded";
                target.UpdatedAt = e.AssignedAt;
            }
            await LoadStatusCountsAsync();
            StateHasChanged();
        });
    }

    private void HandleMemberStatusChanged(object? sender, MemberStatusChangedEventArgs e)
    {
        InvokeAsync(() =>
        {
            var targetTeam = _teams.FirstOrDefault(t => t.Id == e.TeamId);
            if (targetTeam != null)
            {
                var targetMember = targetTeam.Members.FirstOrDefault(m => m.UserId == e.UserId);
                if (targetMember != null)
                {
                    targetMember.MemberStatus = e.NewStatus;
                    targetMember.StatusUpdatedAt = e.ChangedAt;
                    StateHasChanged();
                }
            }
        });
    }

    public void Dispose()
    {
        LocationHub.OnTeamLocationUpdated -= HandleLocationUpdated;
        IncidentHub.OnTeamDispatched -= HandleTeamDispatched;
        IncidentHub.OnMemberStatusChanged -= HandleMemberStatusChanged;

        _searchCts?.Cancel();
        _searchCts?.Dispose();
    }
}
