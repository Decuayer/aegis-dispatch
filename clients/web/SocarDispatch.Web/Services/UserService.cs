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

        public async Task<ApiResponse<PagedResult<UserDto>>?> GetUsersAsync(
        int pageNumber = 1,
        int pageSize = 25,
        RoleType? role = null,
        string? department = null,
        Guid? userId = null,
        string? searchTerm = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var queryParams = new List<string>
            {
                $"pageNumber={Math.Max(1, pageNumber)}",
                $"pageSize={Math.Clamp(pageSize, 1, 100)}"
            };

            if (userId.HasValue)
            {
                queryParams.Add($"userId={userId.Value}");
            }

            if (role.HasValue)
            {
                queryParams.Add($"role={role.Value}");
            }

            if (!string.IsNullOrWhiteSpace(department))
            {
                queryParams.Add($"department={Uri.EscapeDataString(department.Trim())}");
            }

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                queryParams.Add($"searchTerm={Uri.EscapeDataString(searchTerm.Trim())}");
            }

            var queryString = "?" + string.Join("&", queryParams);
            var url = $"api/v1/users{queryString}";

            return await _http.GetFromJsonAsync<ApiResponse<PagedResult<UserDto>>>(url, cancellationToken);
        }
        catch (Exception ex)
        {
            return ApiResponse<PagedResult<UserDto>>.FailureResult($"Failed to retrieve users: {ex.Message}");
        }
    }

    public async Task<ApiResponse<List<UserDto>>?> GetAllUsersAsync(
        string? search = null, 
        RoleType? role = null, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await GetUsersAsync(
                pageNumber: 1,
                pageSize: 100,
                role: role,
                searchTerm: search,
                cancellationToken: cancellationToken);

            if (response != null && response.Success && response.Data != null)
            {
                return ApiResponse<List<UserDto>>.SuccessResult(response.Data.Items, response.Message);
            }

            return response != null
                ? ApiResponse<List<UserDto>>.FailureResult(response.Message)
                : ApiResponse<List<UserDto>>.FailureResult("Failed to retrieve users.");
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

    public async Task<ApiResponse<UserDto>?> UpdateUserRoleAsync(Guid userId, UpdateUserRoleRequestDto request, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _http.PatchAsJsonAsync($"api/v1/users/{userId}/role", request, cancellationToken);
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<UserDto>>(cancellationToken: cancellationToken);
            return result ?? ApiResponse<UserDto>.FailureResult("Failed to update user role.");
        }
        catch (Exception ex)
        {
            return ApiResponse<UserDto>.FailureResult($"Failed to update user role: {ex.Message}");
        }
    }

    public async Task<ApiResponse<bool>?> DeleteUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _http.DeleteAsync($"api/v1/users/{userId}", cancellationToken);
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<bool>>(cancellationToken: cancellationToken);
            return result ?? ApiResponse<bool>.FailureResult("Failed to delete user.");
        }
        catch (Exception ex)
        {
            return ApiResponse<bool>.FailureResult($"Failed to delete user: {ex.Message}");
        }
    }

    public async Task<ApiResponse<UserDto>?> LinkGoogleAccountAsync(string idToken, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _http.PostAsJsonAsync("api/v1/users/me/link-google", new { idToken }, cancellationToken);
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<UserDto>>(cancellationToken: cancellationToken);

            if (result != null && result.Success && result.Data != null)
            {
                OnUserProfileUpdated?.Invoke(result.Data);
            }

            return result ?? ApiResponse<UserDto>.FailureResult("Google hesabı bağlanamadı.");
        }
        catch (Exception ex)
        {
            return ApiResponse<UserDto>.FailureResult($"Google hesabı bağlanırken hata oluştu: {ex.Message}");
        }
    }

    public async Task<ApiResponse<UserDto>?> UnlinkGoogleAccountAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _http.PostAsync("api/v1/users/me/unlink-google", null, cancellationToken);
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<UserDto>>(cancellationToken: cancellationToken);

            if (result != null && result.Success && result.Data != null)
            {
                OnUserProfileUpdated?.Invoke(result.Data);
            }

            return result ?? ApiResponse<UserDto>.FailureResult("Google hesabı bağlantısı kaldırılamadı.");
        }
        catch (Exception ex)
        {
            return ApiResponse<UserDto>.FailureResult($"Google hesabı bağlantısı kaldırılırken hata oluştu: {ex.Message}");
        }
    }
}

