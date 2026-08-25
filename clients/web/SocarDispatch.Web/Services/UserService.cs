using System.Net.Http.Json;
using SocarDispatch.Web.Models.Auth;
using SocarDispatch.Web.Models.Common;

namespace SocarDispatch.Web.Services;

public class UserService : IUserService
{
    private readonly HttpClient _http;

    public event Action<UserDto>? OnUserProfileUpdated;

    public UserService(HttpClient http)
    {
        _http = http;
    }

    public async Task<ApiResponse<List<UserDto>>?> GetUsersAsync(string? search = null, RoleType? role = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var queryParams = new List<string>();
            if (!string.IsNullOrWhiteSpace(search))
            {
                queryParams.Add($"search={Uri.EscapeDataString(search)}");
            }
            if (role.HasValue)
            {
                queryParams.Add($"role={role.Value}");
            }

            var queryString = queryParams.Count > 0 ? "?" + string.Join("&", queryParams) : string.Empty;
            var url = $"api/v1/users{queryString}";

            return await _http.GetFromJsonAsync<ApiResponse<List<UserDto>>>(url, cancellationToken);
        }
        catch (Exception ex)
        {
            return ApiResponse<List<UserDto>>.FailureResult($"Failed to retrieve users: {ex.Message}");
        }
    }

    public async Task<ApiResponse<UserDto>?> GetCurrentUserAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _http.GetFromJsonAsync<ApiResponse<UserDto>>("api/v1/users/me", cancellationToken);
        }
        catch (Exception ex)
        {
            return ApiResponse<UserDto>.FailureResult($"Failed to fetch profile: {ex.Message}");
        }
    }

    public async Task<ApiResponse<UserDto>?> UpdateCurrentUserProfileAsync(UpdateUserProfileRequestDto request, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _http.PutAsJsonAsync("api/v1/users/me", request, cancellationToken);
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<UserDto>>(cancellationToken: cancellationToken);

            if (result != null && result.Success && result.Data != null)
            {
                // Bilgilendir: Sağ üst köşe ve diğer bileşenler anında güncellensin
                OnUserProfileUpdated?.Invoke(result.Data);
            }

            return result;
        }
        catch (Exception ex)
        {
            return ApiResponse<UserDto>.FailureResult($"Failed to update profile: {ex.Message}");
        }
    }
}
