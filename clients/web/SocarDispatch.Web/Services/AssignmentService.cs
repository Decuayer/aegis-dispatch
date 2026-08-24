using System.Net.Http.Json;
using SocarDispatch.Web.Models.Common;
using SocarDispatch.Web.Models.Dispatch;

namespace SocarDispatch.Web.Services;

public class AssignmentService : IAssignmentService
{
    private readonly HttpClient _http;

    public AssignmentService(HttpClient http)
    {
        _http = http;
    }

    public async Task<ApiResponse<AssignmentDto>?> AssignTeamAsync(DispatchRequestDto request, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _http.PostAsJsonAsync("api/v1/assignments", request, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<ApiResponse<AssignmentDto>>(cancellationToken: cancellationToken);
            }

            // Attempt to parse structured error response from backend API
            var errorResponse = await response.Content.ReadFromJsonAsync<ApiResponse<AssignmentDto>>(cancellationToken: cancellationToken);
            return errorResponse ?? ApiResponse<AssignmentDto>.FailureResult($"Assignment failed with HTTP status: {response.StatusCode}");
        }
        catch (Exception ex)
        {
            return ApiResponse<AssignmentDto>.FailureResult($"Failed to dispatch team: {ex.Message}");
        }
    }
}
