using SocarDispatch.Web.Models.Settings;

namespace SocarDispatch.Web.Services;

public interface ISettingsService
{
    event Action<UserPreferencesDto>? OnPreferencesChanged;
    Task<UserPreferencesDto> GetPreferencesAsync();
    Task SavePreferencesAsync(UserPreferencesDto preferences);
}
