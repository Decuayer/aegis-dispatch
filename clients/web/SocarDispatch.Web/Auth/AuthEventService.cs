namespace SocarDispatch.Web.Auth;

public class AuthEventService : IAuthEventService
{
    public event Func<string?, Task>? OnSessionExpired;

    public async Task TriggerSessionExpiredAsync(string? returnUrl = null)
    {
        if (OnSessionExpired != null)
        {
            await OnSessionExpired.Invoke(returnUrl);
        }
    }
}
