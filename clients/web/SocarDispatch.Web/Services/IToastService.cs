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
    public string? ActionUrl { get; set; }
    public Guid? IncidentId { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
}

public interface IToastService
{
    event Action<ToastMessage>? OnToastAdded;
    void Show(string title, string message, ToastLevel level = ToastLevel.Info, int durationMs = 5000, string? actionUrl = null, Guid? incidentId = null, double? latitude = null, double? longitude = null);
    void ShowEmergencyAlert(string category, string emergencyCode, string? reporter, Guid? incidentId = null, double? latitude = null, double? longitude = null);

    // Convenience Helper Methods
    void ShowSuccess(string message, string title = "Success", string? actionUrl = null) => Show(title, message, ToastLevel.Success, actionUrl: actionUrl);
    void ShowError(string message, string title = "Error", string? actionUrl = null) => Show(title, message, ToastLevel.Danger, actionUrl: actionUrl);
    void ShowWarning(string message, string title = "Warning", string? actionUrl = null) => Show(title, message, ToastLevel.Warning, actionUrl: actionUrl);
    void ShowInfo(string message, string title = "Info", string? actionUrl = null) => Show(title, message, ToastLevel.Info, actionUrl: actionUrl);
}
