using System.Collections.Concurrent;
using System.Globalization;
using Microsoft.JSInterop;

namespace SocarDispatch.Web.Services;

public class ToastService : IToastService
{
    private readonly IJSRuntime _js;
    private readonly ISettingsService _settingsService;

    // Deduplication tracking: stores key -> timestamp
    private readonly ConcurrentDictionary<string, DateTime> _recentToasts = new();
    private static readonly TimeSpan DeduplicationWindow = TimeSpan.FromSeconds(3);

    public event Action<ToastMessage>? OnToastAdded;

    public ToastService(IJSRuntime js, ISettingsService settingsService)
    {
        _js = js;
        _settingsService = settingsService;
    }

    public void Show(
        string title, 
        string message, 
        ToastLevel level = ToastLevel.Info, 
        int durationMs = 0,
        string? actionUrl = null,
        Guid? incidentId = null,
        double? latitude = null,
        double? longitude = null)
    {
        // Deduplication check: drop duplicate notifications within 3 seconds
        var dedupKey = $"{title}:{message}:{incidentId?.ToString() ?? actionUrl ?? ""}";
        var now = DateTime.UtcNow;

        // Clean up old entries
        foreach (var entry in _recentToasts)
        {
            if (now - entry.Value > DeduplicationWindow)
            {
                _recentToasts.TryRemove(entry.Key, out _);
            }
        }

        if (_recentToasts.TryGetValue(dedupKey, out var lastSeen))
        {
            if (now - lastSeen < DeduplicationWindow)
            {
                // Discard duplicate notification
                return;
            }
        }

        _recentToasts[dedupKey] = now;

        _ = ShowInternalAsync(title, message, level, durationMs, actionUrl, incidentId, latitude, longitude);
    }

    private async Task ShowInternalAsync(
        string title, 
        string message, 
        ToastLevel level, 
        int durationMs,
        string? actionUrl,
        Guid? incidentId,
        double? latitude,
        double? longitude)
    {
        if (durationMs <= 0)
        {
            var prefs = await _settingsService.GetPreferencesAsync();
            durationMs = Math.Max(3000, prefs.ToastDurationSeconds * 1000);
        }

        var toast = new ToastMessage
        {
            Title = title,
            Message = message,
            Level = level,
            DurationMs = durationMs,
            ActionUrl = actionUrl,
            IncidentId = incidentId,
            Latitude = latitude,
            Longitude = longitude
        };

        OnToastAdded?.Invoke(toast);
    }

    public void ShowEmergencyAlert(
        string category, 
        string emergencyCode, 
        string? reporter,
        Guid? incidentId = null,
        double? latitude = null,
        double? longitude = null)
    {
        _ = TriggerEmergencyAlertAsync(category, emergencyCode, reporter, incidentId, latitude, longitude);
    }

    private async Task TriggerEmergencyAlertAsync(
        string category, 
        string emergencyCode, 
        string? reporter,
        Guid? incidentId,
        double? latitude,
        double? longitude)
    {
        var prefs = await _settingsService.GetPreferencesAsync();

        string? actionUrl = null;
        if (incidentId.HasValue && latitude.HasValue && longitude.HasValue)
        {
            actionUrl = $"/map?incidentId={incidentId.Value}&lat={latitude.Value.ToString(CultureInfo.InvariantCulture)}&lng={longitude.Value.ToString(CultureInfo.InvariantCulture)}";
        }
        else if (incidentId.HasValue)
        {
            actionUrl = $"/map?incidentId={incidentId.Value}";
        }
        else
        {
            actionUrl = "/map";
        }

        Show(
            title: $"🚨 EMERGENCY ALERT: {emergencyCode}",
            message: $"{category} — Reported by: {reporter ?? "Unknown"} • Click to view on map",
            level: ToastLevel.Danger,
            durationMs: Math.Max(6000, prefs.ToastDurationSeconds * 1000 + 2000),
            actionUrl: actionUrl,
            incidentId: incidentId,
            latitude: latitude,
            longitude: longitude
        );

        if (prefs.EmergencyAudioAlertEnabled)
        {
            await PlayAlertSoundAsync();
        }
    }

    private async Task PlayAlertSoundAsync()
    {
        try
        {
            await _js.InvokeVoidAsync("socarAudio.playEmergencyBeep");
        }
        catch
        {
            // Ignore autoplay restrictions
        }
    }
}
