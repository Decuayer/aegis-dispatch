using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;
using SocarDispatch.Web.Models.Common;
using SocarDispatch.Web.Models.Dispatch;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace SocarDispatch.Web.Components.Dispatch;

public partial class CreateIncidentModal : ComponentBase, IAsyncDisposable
{
    [Parameter] public bool IsVisible { get; set; }
    [Parameter] public EventCallback<bool> IsVisibleChanged { get; set; }
    [Parameter] public EventCallback<IncidentDetailViewModel> OnIncidentCreated { get; set; }

    private readonly string _pickerContainerId = $"picker-map-{Guid.NewGuid():N}";
    private DotNetObjectReference<CreateIncidentModal>? _dotNetRef;

    private CreateIncidentRequestDto _model = new();
    private List<IncidentCategoryDto> _categories = new();
    private List<EmergencyCodeDto> _emergencyCodes = new();
    private EmergencyCodeDto? _selectedCodeObj;

    private bool _isLoadingTaxonomies = true;
    private bool _isSubmitting = false;
    private bool _isUploadingMedia = false;
    private bool _isLocating = false;
    private bool _mapInitialized = false;

    protected override async Task OnInitializedAsync()
    {
        _dotNetRef = DotNetObjectReference.Create(this);
    }

    protected override async Task OnParametersSetAsync()
    {
        if (IsVisible)
        {
            // Set initial coordinates from user settings if not set
            if (_categories.Count == 0 || _emergencyCodes.Count == 0)
            {
                await LoadTaxonomiesAsync();
            }

            var prefs = await SettingsService.GetPreferencesAsync();
            _model.Latitude = Math.Round((decimal)prefs.DefaultLatitude, 6);
            _model.Longitude = Math.Round((decimal)prefs.DefaultLongitude, 6);
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (IsVisible && !_mapInitialized)
        {
            await Task.Delay(100);
            var prefs = await SettingsService.GetPreferencesAsync();
            
            _model.Latitude = Math.Round((decimal)prefs.DefaultLatitude, 6);
            _model.Longitude = Math.Round((decimal)prefs.DefaultLongitude, 6);

            if (_dotNetRef != null)
            {
                await MapService.InitializePickerMapAsync(
                    _pickerContainerId, 
                    (double)_model.Latitude, 
                    (double)_model.Longitude, 
                    prefs.DefaultZoom > 0 ? prefs.DefaultZoom : 14, 
                    _dotNetRef, 
                    prefs.TileProvider ?? "OpenStreetMap"
                );
                _mapInitialized = true;
                StateHasChanged();
            }
        }
        else if (!IsVisible && _mapInitialized)
        {
            await MapService.DestroyPickerMapAsync();
            _mapInitialized = false;
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
        }
        catch (Exception ex)
        {
            ToastService.ShowError($"Failed to load taxonomies: {ex.Message}");
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
                ToastService.ShowSuccess("GPS location pinpointed on map.");
            }
        }
        catch (Exception)
        {
            // Fallback to facility default location from settings
            var prefs = await SettingsService.GetPreferencesAsync();
            _model.Latitude = Math.Round((decimal)prefs.DefaultLatitude, 6);
            _model.Longitude = Math.Round((decimal)prefs.DefaultLongitude, 6);
            await MapService.SetPickerLocationAsync(prefs.DefaultLatitude, prefs.DefaultLongitude, prefs.DefaultZoom);
            ToastService.ShowWarning("Device GPS unavailable. Re-centered to facility default coordinates.");
        }
        finally
        {
            _isLocating = false;
        }
    }

    private async Task HandleFileUpload(InputFileChangeEventArgs e)
    {
        var files = e.GetMultipleFiles(5);
        if (files.Count == 0) return;

        _isUploadingMedia = true;
        try
        {
            foreach (var file in files)
            {
                using var stream = file.OpenReadStream(maxAllowedSize: 50 * 1024 * 1024);
                using var content = new MultipartFormDataContent();
                var fileContent = new StreamContent(stream);
                fileContent.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType);
                content.Add(fileContent, "File", file.Name);
                content.Add(new StringContent("incident"), "Category");

                var response = await Http.PostAsync("api/v1/media/upload", content);
                var result = await response.Content.ReadFromJsonAsync<ApiResponse<MediaUploadResponseDto>>();

                if (response.IsSuccessStatusCode && result?.Success == true && result.Data != null)
                {
                    var mediaType = file.ContentType.StartsWith("video", StringComparison.OrdinalIgnoreCase) 
                        ? IncidentMediaType.Video 
                        : IncidentMediaType.Photo;

                    _model.MediaAttachments.Add(new CreateIncidentMediaRequestDto
                    {
                        MediaUrl = result.Data.MediaUrl,
                        MediaType = mediaType,
                        FileName = file.Name,
                        FileSize = file.Size
                    });
                }
                else
                {
                    ToastService.ShowError(result?.Message ?? $"Failed to upload {file.Name}");
                }
            }
        }
        catch (Exception ex)
        {
            ToastService.ShowError($"File upload error: {ex.Message}");
        }
        finally
        {
            _isUploadingMedia = false;
        }
    }

    private void RemoveMedia(CreateIncidentMediaRequestDto media)
    {
        _model.MediaAttachments.Remove(media);
    }

    private async Task HandleValidSubmit()
    {
        _isSubmitting = true;
        try
        {
            var response = await IncidentService.CreateIncidentAsync(_model);
            if (response != null && response.Success && response.Data != null)
            {
                ToastService.ShowSuccess($"Incident #{response.Data.Id.ToString()[..8]} successfully reported and broadcasted!", "Incident Created");
                
                await OnIncidentCreated.InvokeAsync(response.Data);
                await CloseModalAsync();
            }
            else
            {
                ToastService.ShowError(response?.Message ?? "Failed to create incident report.");
            }
        }
        catch (Exception ex)
        {
            ToastService.ShowError($"Error creating incident: {ex.Message}");
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
            await CloseModalAsync();
        }
    }

    private async Task CloseModalAsync()
    {
        _model = new CreateIncidentRequestDto();
        _selectedCodeObj = null;
        if (_mapInitialized)
        {
            await MapService.DestroyPickerMapAsync();
            _mapInitialized = false;
        }
        IsVisible = false;
        await IsVisibleChanged.InvokeAsync(false);
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
