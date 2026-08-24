namespace SocarDispatch.Web.Services;

public enum ToastLevel
{
    Info,
    Success,
    Warning,
    Danger
}

public class ToastMessage
{
    public Guid Id { get; } = Guid.NewGuid();
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public ToastLevel Level { get; set; } = ToastLevel.Info;
    public DateTime CreatedAt { get; } = DateTime.UtcNow;
    public int DurationMs { get; set; } = 5000;
}

public interface IToastService
{
    event Action<ToastMessage>? OnToastAdded;
    void Show(string title, string message, ToastLevel level = ToastLevel.Info, int durationMs = 5000);
    void ShowEmergencyAlert(string category, string emergencyCode, string? reporter);
}
