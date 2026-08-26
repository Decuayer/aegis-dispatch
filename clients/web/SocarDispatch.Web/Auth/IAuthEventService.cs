namespace SocarDispatch.Web.Auth;

public interface IAuthEventService
{
    /// Session termination event triggered upon a 401 error or token expiration.
    event Func<string?, Task>? OnSessionExpired;

    /// Raises the session termination event.
    Task TriggerSessionExpiredAsync(string? returnUrl = null);
}
