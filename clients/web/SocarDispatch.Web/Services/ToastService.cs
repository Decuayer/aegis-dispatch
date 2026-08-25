using Microsoft.JSInterop;

namespace SocarDispatch.Web.Services;

public class ToastService : IToastService
{
    private readonly IJSRuntime _js;
    private readonly ISettingsService _settingsService;

    public event Action<ToastMessage>? OnToastAdded;

    public ToastService(IJSRuntime js, ISettingsService settingsService)
    {
        _js = js;
        _settingsService = settingsService;
    }

    public void Show(string title, string message, ToastLevel level = ToastLevel.Info, int durationMs = 0)
    {
        _ = ShowInternalAsync(title, message, level, durationMs);
    }

    private async Task ShowInternalAsync(string title, string message, ToastLevel level, int durationMs)
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
            DurationMs = durationMs
        };

        OnToastAdded?.Invoke(toast);
    }

    public void ShowEmergencyAlert(string category, string emergencyCode, string? reporter)
    {
        _ = TriggerEmergencyAlertAsync(category, emergencyCode, reporter);
    }

    private async Task TriggerEmergencyAlertAsync(string category, string emergencyCode, string? reporter)
    {
        var prefs = await _settingsService.GetPreferencesAsync();

        Show(
            title: $"🚨 EMERGENCY ALERT: {emergencyCode}",
            message: $"{category} — Reported by: {reporter ?? "Unknown"}",
            level: ToastLevel.Danger,
            durationMs: Math.Max(5000, prefs.ToastDurationSeconds * 1000 + 2000)
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
