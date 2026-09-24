using Blazored.LocalStorage;
using SocarDispatch.Web.Models.Settings;

namespace SocarDispatch.Web.Services;

public class SettingsService : ISettingsService
{
    private const string StorageKey = "aegis_user_preferences";
    private readonly ILocalStorageService _localStorage;
    private UserPreferencesDto? _cachedPreferences;

    public event Action<UserPreferencesDto>? OnPreferencesChanged;

    public SettingsService(ILocalStorageService localStorage)
    {
        _localStorage = localStorage;
    }

    public async Task<UserPreferencesDto> GetPreferencesAsync()
    {
        if (_cachedPreferences != null)
        {
            return _cachedPreferences;
        }

        try
        {
            var stored = await _localStorage.GetItemAsync<UserPreferencesDto>(StorageKey);
            _cachedPreferences = stored ?? new UserPreferencesDto();
        }
        catch
        {
            _cachedPreferences = new UserPreferencesDto();
        }

        return _cachedPreferences;
    }

    public async Task SavePreferencesAsync(UserPreferencesDto preferences)
    {
        _cachedPreferences = preferences.Clone();
        await _localStorage.SetItemAsync(StorageKey, _cachedPreferences);
        OnPreferencesChanged?.Invoke(_cachedPreferences);
    }
}
