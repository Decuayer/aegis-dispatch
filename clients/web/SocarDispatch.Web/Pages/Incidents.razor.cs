using Microsoft.AspNetCore.Components;
using SocarDispatch.Web.Events;
using SocarDispatch.Web.Models.Common;
using SocarDispatch.Web.Models.Dispatch;
using SocarDispatch.Web.Services;
using SocarDispatch.Web.Services.SignalR;

namespace SocarDispatch.Web.Pages;

public partial class Incidents : ComponentBase, IDisposable
{
    [Inject] private IIncidentService IncidentService { get; set; } = default!;
    [Inject] private IIncidentHubClient IncidentHub { get; set; } = default!;
    [Inject] private IToastService ToastService { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;

    private bool _isLoading = true;
    private List<IncidentDetailViewModel> _pagedIncidents = new();
    private Dictionary<string, int> _statusCounts = new();

    // Filters
    private string _selectedStatus = "All";
    private string _selectedCategory = "All";
    private string _searchTerm = string.Empty;
    private string _selectedTimeRange = "All";
    private DateTime? _customFromDate;
    private DateTime? _customToDate;
    private string _selectedSort = "Newest";

    // Server-Side Pagination
    private int CurrentPage { get; set; } = 1;
    private int PageSize { get; set; } = 25;
    private int _totalCount = 0;

    // Modals and Drawer
    private bool _isDrawerOpen = false;
    private Guid? _selectedIncidentId;
    private bool _isCreateIncidentModalOpen = false;

    // Debounced search cancellation token
    private CancellationTokenSource? _searchCts;

    protected override async Task OnInitializedAsync()
    {
        await LoadIncidentsAsync();
        await LoadActiveOpenCountAsync();

        // Subscribe to real-time SignalR events
        IncidentHub.OnNewIncidentReceived += HandleNewIncidentReceivedRealtime;
        IncidentHub.OnIncidentStatusChanged += HandleIncidentStatusChangedRealtime;
        IncidentHub.OnIncidentUpdated += HandleIncidentUpdatedRealtime;
    }

    private (DateTime? from, DateTime? to) GetDateRangeBoundaries()
    {
        if (_selectedTimeRange == "Custom")
            return (_customFromDate, _customToDate);

        var now = DateTime.UtcNow;
        return _selectedTimeRange switch
        {
            "1h" => (now.AddHours(-1), null),
            "6h" => (now.AddHours(-6), null),
            "24h" => (now.AddHours(-24), null),
            "7d" => (now.AddDays(-7), null),
            "30d" => (now.AddDays(-30), null),
            _ => (null, null)
        };
    }

    public async Task LoadIncidentsAsync(CancellationToken cancellationToken = default)
    {
        _isLoading = true;
        StateHasChanged();

        try
        {
            var (from, to) = GetDateRangeBoundaries();

            var response = await IncidentService.GetIncidentsAsync(
                pageNumber: CurrentPage,
                pageSize: PageSize,
                searchTerm: _searchTerm,
                status: _selectedStatus.Equals("All", StringComparison.OrdinalIgnoreCase) ? null : _selectedStatus,
                category: _selectedCategory.Equals("All", StringComparison.OrdinalIgnoreCase) ? null : _selectedCategory,
                fromDate: from,
                toDate: to,
                cancellationToken: cancellationToken);

            if (response != null && response.Success && response.Data != null)
            {
                _pagedIncidents = response.Data.Items;
                _totalCount = response.Data.TotalCount;
                CurrentPage = response.Data.PageNumber;
            }
            else
            {
                _pagedIncidents.Clear();
                _totalCount = 0;
                ToastService.Show("Error", response?.Message ?? "Failed to load incidents.", ToastLevel.Danger);
            }
        }
        catch (OperationCanceledException)
        {
            // Suppress cancellation when rapid keystrokes arrive
            return;
        }
        catch (Exception ex)
        {
            ToastService.Show("Error", $"Error loading incidents: {ex.Message}", ToastLevel.Danger);
        }
        finally
        {
            _isLoading = false;
            StateHasChanged();
        }
    }

    private async Task LoadActiveOpenCountAsync()
    {
        try
        {
            var activeRes = await IncidentService.GetActiveIncidentsAsync();
            if (activeRes?.Success == true && activeRes.Data != null)
            {
                _statusCounts["Open"] = activeRes.Data.Count(i => i.Status.Equals("Open", StringComparison.OrdinalIgnoreCase));
                StateHasChanged();
            }
        }
        catch
        {
            // Ignore background telemetry count failure
        }
    }

    private async Task HandleStatusChanged(string status)
    {
        _selectedStatus = status;
        CurrentPage = 1;
        await LoadIncidentsAsync();
    }

    private async Task HandleCategoryChanged(string category)
    {
        _selectedCategory = category;
        CurrentPage = 1;
        await LoadIncidentsAsync();
    }

    private async Task HandleSearchChanged(string search)
    {
        _searchTerm = search;
        CurrentPage = 1;

        _searchCts?.Cancel();
        _searchCts?.Dispose();
        _searchCts = new CancellationTokenSource();
        var token = _searchCts.Token;

        try
        {
            // Debounce for 350ms before executing query
            await Task.Delay(350, token);
            await LoadIncidentsAsync(token);
        }
        catch (OperationCanceledException)
        {
            // Subsequent input superseded this request
        }
    }

    private async Task HandleTimeRangeChanged(string range)
    {
        _selectedTimeRange = range;
        CurrentPage = 1;
        await LoadIncidentsAsync();
    }

    private async Task HandleCustomDateRangeChanged((DateTime? from, DateTime? to) dates)
    {
        _customFromDate = dates.from;
        _customToDate = dates.to;
        CurrentPage = 1;
        await LoadIncidentsAsync();
    }

    private void HandleSortChanged(string sort)
    {
        _selectedSort = sort;
        // Sort current in-memory view if needed
        if (_selectedSort == "Oldest")
            _pagedIncidents = _pagedIncidents.OrderBy(i => i.CreatedAt).ToList();
        else
            _pagedIncidents = _pagedIncidents.OrderByDescending(i => i.CreatedAt).ToList();
    }

    private async Task HandlePageChanged(int page)
    {
        CurrentPage = page;
        await LoadIncidentsAsync();
    }

    private async Task HandlePageSizeChanged(int size)
    {
        PageSize = size;
        CurrentPage = 1;
        await LoadIncidentsAsync();
    }

    private async Task ResetFilters()
    {
        _searchCts?.Cancel();
        _selectedStatus = "All";
        _selectedCategory = "All";
        _searchTerm = string.Empty;
        _selectedTimeRange = "All";
        _customFromDate = null;
        _customToDate = null;
        _selectedSort = "Newest";
        CurrentPage = 1;
        PageSize = 25;

        await LoadIncidentsAsync();
    }

    private void HandleCreateIncident()
    {
        _isCreateIncidentModalOpen = true;
    }

    private async Task HandleIncidentCreated(IncidentDetailViewModel incident)
    {
        CurrentPage = 1;
        await LoadIncidentsAsync();
        await LoadActiveOpenCountAsync();
    }

    private void HandleIncidentSelected(IncidentDetailViewModel incident)
    {
        _selectedIncidentId = incident.Id;
        _isDrawerOpen = true;
    }

    private async Task HandleIncidentAssigned(Guid incidentId)
    {
        await LoadIncidentsAsync();
        await LoadActiveOpenCountAsync();
    }

    private async Task HandleIncidentUpdated(IncidentDetailViewModel updatedIncident)
    {
        var existing = _pagedIncidents.FirstOrDefault(x => x.Id == updatedIncident.Id);
        if (existing != null)
        {
            var index = _pagedIncidents.IndexOf(existing);
            _pagedIncidents[index] = updatedIncident;
            StateHasChanged();
        }
        else
        {
            await LoadIncidentsAsync();
        }
        await LoadActiveOpenCountAsync();
    }

    // Real-time SignalR Event Handlers
    private async void HandleNewIncidentReceivedRealtime(object? sender, NewIncidentReceivedEventArgs e)
    {
        await InvokeAsync(async () =>
        {
            // If operator is on the first page, reload to display newly reported incident
            if (CurrentPage == 1)
            {
                await LoadIncidentsAsync();
            }
            await LoadActiveOpenCountAsync();
        });
    }

    private async void HandleIncidentStatusChangedRealtime(object? sender, IncidentStatusChangedEventArgs e)
    {
        await InvokeAsync(async () =>
        {
            var incident = _pagedIncidents.FirstOrDefault(x => x.Id == e.IncidentId);
            if (incident != null)
            {
                incident.Status = e.Status;
                StateHasChanged();
            }
            await LoadActiveOpenCountAsync();
        });
    }

    private async void HandleIncidentUpdatedRealtime(object? sender, IncidentUpdatedEventArgs e)
    {
        await InvokeAsync(() =>
        {
            var incident = _pagedIncidents.FirstOrDefault(x => x.Id == e.IncidentId);
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

    public void Dispose()
    {
        _searchCts?.Cancel();
        _searchCts?.Dispose();

        IncidentHub.OnNewIncidentReceived -= HandleNewIncidentReceivedRealtime;
        IncidentHub.OnIncidentStatusChanged -= HandleIncidentStatusChangedRealtime;
        IncidentHub.OnIncidentUpdated -= HandleIncidentUpdatedRealtime;
    }
}
