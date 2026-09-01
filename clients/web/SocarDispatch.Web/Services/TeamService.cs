using System.Net.Http.Json;
using SocarDispatch.Web.Models.Common;
using SocarDispatch.Web.Models.Dispatch;

namespace SocarDispatch.Web.Services;

public class TeamService : ITeamService
{
    private readonly HttpClient _http;

    public TeamService(HttpClient http)
    {
        _http = http;
    }

        public async Task<ApiResponse<PagedResult<TeamDto>>?> GetTeamsAsync(
        int pageNumber = 1,
        int pageSize = 25,
        string? status = null,
        Guid? teamId = null,
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

            if (teamId.HasValue)
            {
                queryParams.Add($"teamId={teamId.Value}");
            }

            if (!string.IsNullOrWhiteSpace(status) && !status.Equals("All", StringComparison.OrdinalIgnoreCase))
            {
                queryParams.Add($"status={Uri.EscapeDataString(status.Trim())}");
            }

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                queryParams.Add($"searchTerm={Uri.EscapeDataString(searchTerm.Trim())}");
            }

            var queryString = "?" + string.Join("&", queryParams);
            var url = $"api/v1/teams{queryString}";

            return await _http.GetFromJsonAsync<ApiResponse<PagedResult<TeamDto>>>(url, cancellationToken);
        }
        catch (Exception ex)
        {
            return ApiResponse<PagedResult<TeamDto>>.FailureResult($"Failed to retrieve teams: {ex.Message}");
        }
    }

    public async Task<ApiResponse<List<TeamDto>>?> GetAllTeamsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await GetTeamsAsync(
                pageNumber: 1, 
                pageSize: 100, 
                cancellationToken: cancellationToken);

            if (response != null && response.Success && response.Data != null)
            {
                return ApiResponse<List<TeamDto>>.SuccessResult(response.Data.Items, response.Message);
            }

            return response != null
                ? ApiResponse<List<TeamDto>>.FailureResult(response.Message)
                : ApiResponse<List<TeamDto>>.FailureResult("Failed to retrieve teams.");
        }
        catch (Exception ex)
        {
            return ApiResponse<List<TeamDto>>.FailureResult($"Failed to retrieve teams: {ex.Message}");
        }
    }



    public async Task<ApiResponse<TeamDto>?> GetTeamByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _http.GetFromJsonAsync<ApiResponse<TeamDto>>(
                $"api/v1/teams/{id}", 
                cancellationToken);
        }
        catch (Exception ex)
        {
            return ApiResponse<TeamDto>.FailureResult($"Failed to retrieve team details: {ex.Message}");
        }
    }

    public async Task<ApiResponse<TeamDto>?> CreateTeamAsync(CreateTeamRequestDto request, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _http.PostAsJsonAsync("api/v1/teams", request, cancellationToken);
            return await response.Content.ReadFromJsonAsync<ApiResponse<TeamDto>>(cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            return ApiResponse<TeamDto>.FailureResult($"Failed to create team: {ex.Message}");
        }
    }

    public async Task<ApiResponse<TeamDto>?> UpdateTeamAsync(Guid id, UpdateTeamRequestDto request, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _http.PutAsJsonAsync($"api/v1/teams/{id}", request, cancellationToken);
            return await response.Content.ReadFromJsonAsync<ApiResponse<TeamDto>>(cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            return ApiResponse<TeamDto>.FailureResult($"Failed to update team: {ex.Message}");
        }
    }

    public async Task<ApiResponse<TeamDto>?> UpdateTeamStatusAsync(Guid id, string status, CancellationToken cancellationToken = default)
    {
        try
        {
            var request = new UpdateTeamStatusRequestDto { Status = status };
            var response = await _http.PatchAsJsonAsync($"api/v1/teams/{id}/status", request, cancellationToken);
            return await response.Content.ReadFromJsonAsync<ApiResponse<TeamDto>>(cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            return ApiResponse<TeamDto>.FailureResult($"Failed to update team status: {ex.Message}");
        }
    }

    public async Task<ApiResponse<TeamDto>?> AddMemberAsync(Guid teamId, Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var request = new AddTeamMemberRequestDto { UserId = userId };
            var response = await _http.PostAsJsonAsync($"api/v1/teams/{teamId}/members", request, cancellationToken);
            return await response.Content.ReadFromJsonAsync<ApiResponse<TeamDto>>(cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            return ApiResponse<TeamDto>.FailureResult($"Failed to add team member: {ex.Message}");
        }
    }

    public async Task<ApiResponse<TeamDto>?> RemoveMemberAsync(Guid teamId, Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _http.DeleteAsync($"api/v1/teams/{teamId}/members/{userId}", cancellationToken);
            return await response.Content.ReadFromJsonAsync<ApiResponse<TeamDto>>(cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            return ApiResponse<TeamDto>.FailureResult($"Failed to remove team member: {ex.Message}");
        }
    }

        public async Task<ApiResponse<TeamMemberDto>?> UpdateMemberStatusAsync(Guid teamId, Guid userId, string status, CancellationToken cancellationToken = default)
    {
        try
        {
            var request = new UpdateMemberStatusRequestDto { Status = status };
            var response = await _http.PatchAsJsonAsync($"api/v1/teams/{teamId}/members/{userId}/status", request, cancellationToken);
            return await response.Content.ReadFromJsonAsync<ApiResponse<TeamMemberDto>>(cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            return ApiResponse<TeamMemberDto>.FailureResult($"Failed to update member status: {ex.Message}");
        }
    }

}
