using Microsoft.JSInterop;

namespace SocarDispatch.Web.Services;

public class ToastService : IToastService
{
    private readonly IJSRuntime _js;

    public event Action<ToastMessage>? OnToastAdded;

    public ToastService(IJSRuntime js)
    {
        _js = js;
    }

    public void Show(string title, string message, ToastLevel level = ToastLevel.Info, int durationMs = 5000)
    {
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
        // 1. Görsel Toast Göster
        Show(
            title: $"🚨 YENİ ACİL DURUM: {emergencyCode}",
            message: $"{category} — Bildiren: {reporter ?? "Bilinmiyor"}",
            level: ToastLevel.Danger,
            durationMs: 7000
        );

        // 2. Tarayıcı Web Audio API ile kısa uyarı sesi çal
        _ = PlayAlertSoundAsync();
    }

    private async Task PlayAlertSoundAsync()
    {
        try
        {
            await _js.InvokeVoidAsync("socarAudio.playEmergencyBeep");
        }
        catch
        {
            // Tarayıcı otomatik oynatma kısıtlaması durumunda sessizce geç
        }
    }
}
