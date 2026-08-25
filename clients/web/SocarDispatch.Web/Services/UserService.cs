using System.Net.Http.Json;
using SocarDispatch.Web.Models.Auth;
using SocarDispatch.Web.Models.Common;

namespace SocarDispatch.Web.Services;

public class UserService : IUserService
{
    private readonly HttpClient _http;

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
}
