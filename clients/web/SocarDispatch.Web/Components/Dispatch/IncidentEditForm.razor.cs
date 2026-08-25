using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using SocarDispatch.Web.Models.Common;
using SocarDispatch.Web.Models.Dispatch;
using SocarDispatch.Web.Services;

namespace SocarDispatch.Web.Components.Dispatch;

public partial class IncidentEditForm : ComponentBase, IAsyncDisposable
{
    [Parameter] public IncidentDetailViewModel Incident { get; set; } = default!;
    [Parameter] public EventCallback<IncidentDetailViewModel> OnSaved { get; set; }
    [Parameter] public EventCallback OnCancel { get; set; }

    private readonly string _pickerContainerId = $"picker-map-edit-{Guid.NewGuid():N}";
    private DotNetObjectReference<IncidentEditForm>? _dotNetRef;

    private UpdateIncidentRequestDto _model = new();
    private List<IncidentCategoryDto> _categories = new();
    private List<EmergencyCodeDto> _emergencyCodes = new();
    private EmergencyCodeDto? _selectedCodeObj;

    private bool _isLoadingTaxonomies = true;
    private bool _isSubmitting = false;
    private bool _isLocating = false;
    private bool _mapInitialized = false;

    protected override async Task OnInitializedAsync()
    {
        _dotNetRef = DotNetObjectReference.Create(this);
    }

    protected override async Task OnParametersSetAsync()
    {
        if (Incident != null)
        {
            _model = UpdateIncidentRequestDto.FromIncident(Incident);

            if (_categories.Count == 0 || _emergencyCodes.Count == 0)
            {
                await LoadTaxonomiesAsync();
            }
            else
            {
                OnEmergencyCodeChanged();
            }
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!_mapInitialized && !_isLoadingTaxonomies && _dotNetRef != null)
        {
            await Task.Delay(100);
            var prefs = await SettingsService.GetPreferencesAsync();

            await MapService.InitializePickerMapAsync(
                _pickerContainerId,
                (double)_model.Latitude,
                (double)_model.Longitude,
                prefs.DefaultZoom > 0 ? prefs.DefaultZoom : 15,
                _dotNetRef,
                prefs.TileProvider ?? "OpenStreetMap"
            );
            _mapInitialized = true;
            StateHasChanged();
        }
    }

    private async Task LoadTaxonomiesAsync()
    {
        _isLoadingTaxonomies = true;
        try
        {
            var catTask = CategoryService.GetCategoriesAsync();
            var codeTask = EmergencyCodeService.GetEmergencyCodesAsync();

            await Task.WhenAll(catTask, codeTask);

            var catRes = await catTask;
            if (catRes?.Success == true && catRes.Data != null)
            {
                _categories = catRes.Data.Where(c => c.IsActive).ToList();
            }

            var codeRes = await codeTask;
            if (codeRes?.Success == true && codeRes.Data != null)
            {
                _emergencyCodes = codeRes.Data.Where(c => c.IsActive).OrderBy(c => c.SeverityLevel).ToList();
            }

            OnEmergencyCodeChanged();
        }
        catch (Exception ex)
        {
            ToastService.Show("Taxonomy Error", $"Failed to load taxonomies: {ex.Message}", ToastLevel.Danger);
        }
        finally
        {
            _isLoadingTaxonomies = false;
        }
    }

    private void OnEmergencyCodeChanged()
    {
        _selectedCodeObj = _emergencyCodes.FirstOrDefault(c => c.Code == _model.EmergencyCode);
    }

    [JSInvokable]
    public void NotifyLocationPicked(double lat, double lng)
    {
        _model.Latitude = Math.Round((decimal)lat, 6);
        _model.Longitude = Math.Round((decimal)lng, 6);
        StateHasChanged();
    }

    private async Task OnLatitudeInputChanged(ChangeEventArgs e)
    {
        if (decimal.TryParse(e.Value?.ToString(), out var lat))
        {
            _model.Latitude = lat;
            await MapService.SetPickerLocationAsync((double)_model.Latitude, (double)_model.Longitude);
        }
    }

    private async Task OnLongitudeInputChanged(ChangeEventArgs e)
    {
        if (decimal.TryParse(e.Value?.ToString(), out var lng))
        {
            _model.Longitude = lng;
            await MapService.SetPickerLocationAsync((double)_model.Latitude, (double)_model.Longitude);
        }
    }

    private async Task LocateUserGPS()
    {
        _isLocating = true;
        try
        {
            var pos = await JS.InvokeAsync<GeolocationCoordinates>("leafletMap.getCurrentBrowserLocation");
            if (pos != null && pos.Latitude != 0 && pos.Longitude != 0)
            {
                _model.Latitude = Math.Round((decimal)pos.Latitude, 6);
                _model.Longitude = Math.Round((decimal)pos.Longitude, 6);
                await MapService.SetPickerLocationAsync(pos.Latitude, pos.Longitude, 16);
                ToastService.Show("Location Found", "GPS location pinned on map.", ToastLevel.Success);
            }
        }
        catch (Exception ex)
        {
            ToastService.Show("GPS Warning", $"Could not acquire browser GPS: {ex.Message}", ToastLevel.Warning);
        }
        finally
        {
            _isLocating = false;
        }
    }

    private async Task HandleValidSubmit()
    {
        if (Incident == null) return;

        _isSubmitting = true;
        try
        {
            var response = await IncidentService.UpdateIncidentAsync(Incident.Id, _model);
            if (response?.Success == true && response.Data != null)
            {
                ToastService.Show("Incident Updated", "Incident details and coordinates updated successfully.", ToastLevel.Success);
                await OnSaved.InvokeAsync(response.Data);
            }
            else
            {
                ToastService.Show("Update Failed", response?.Message ?? "Failed to save incident changes.", ToastLevel.Danger);
            }
        }
        catch (Exception ex)
        {
            ToastService.Show("Update Error", ex.Message, ToastLevel.Danger);
        }
        finally
        {
            _isSubmitting = false;
        }
    }

    private async Task HandleCancel()
    {
        if (!_isSubmitting)
        {
            await OnCancel.InvokeAsync();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_mapInitialized)
        {
            await MapService.DestroyPickerMapAsync();
            _mapInitialized = false;
        }
        _dotNetRef?.Dispose();
    }

    private class GeolocationCoordinates
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public double? Accuracy { get; set; }
    }
}
