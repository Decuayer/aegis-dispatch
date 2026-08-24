using System.Net.Http.Json;
using SocarDispatch.Web.Models.Common;
using SocarDispatch.Web.Models.Dispatch;

namespace SocarDispatch.Web.Services;

public class IncidentService : IIncidentService
{
    private readonly HttpClient _http;

    public IncidentService(HttpClient http)
    {
        _http = http;
    }

    public async Task<ApiResponse<IncidentDetailViewModel>?> GetIncidentByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _http.GetFromJsonAsync<ApiResponse<IncidentDetailViewModel>>(
                $"api/v1/incidents/{id}", 
                cancellationToken);
        }
        catch (Exception ex)
        {
            return ApiResponse<IncidentDetailViewModel>.FailureResult($"Failed to retrieve incident details: {ex.Message}");
        }
    }

    public async Task<ApiResponse<List<IncidentDetailViewModel>>?> GetActiveIncidentsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _http.GetFromJsonAsync<ApiResponse<List<IncidentDetailViewModel>>>(
                "api/v1/incidents?status=Open", 
                cancellationToken);
        }
        catch (Exception ex)
        {
            return ApiResponse<List<IncidentDetailViewModel>>.FailureResult($"Failed to retrieve active incidents: {ex.Message}");
        }
    }

    public async Task<ApiResponse<IncidentDetailViewModel>?> UpdateStatusAsync(Guid id, string status, string? completionNotes = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var payload = new { Status = status, CompletionNotes = completionNotes };
            var response = await _http.PatchAsJsonAsync($"api/v1/incidents/{id}/status", payload, cancellationToken);
            
            return await response.Content.ReadFromJsonAsync<ApiResponse<IncidentDetailViewModel>>(cancellationToken: cancellationToken)
                   ?? ApiResponse<IncidentDetailViewModel>.FailureResult("Empty response from server.");
        }
        catch (Exception ex)
        {
            return ApiResponse<IncidentDetailViewModel>.FailureResult($"Failed to update incident status: {ex.Message}");
        }
    }
}
