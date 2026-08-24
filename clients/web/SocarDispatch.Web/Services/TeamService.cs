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

    public async Task<ApiResponse<List<TeamDto>>?> GetTeamsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _http.GetFromJsonAsync<ApiResponse<List<TeamDto>>>(
                "api/v1/teams", 
                cancellationToken);
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
}
