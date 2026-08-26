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
                "api/v1/incidents?status=Open,Assigned", 
                cancellationToken);
        }
        catch (Exception ex)
        {
            return ApiResponse<List<IncidentDetailViewModel>>.FailureResult($"Failed to retrieve active incidents: {ex.Message}");
        }
    }

    public async Task<ApiResponse<List<IncidentDetailViewModel>>?> GetAllIncidentsAsync(
        string? status = null, 
        string? category = null, 
        DateTime? from = null,
        DateTime? to = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var queryParams = new List<string>();
            if (!string.IsNullOrWhiteSpace(status) && status != "All")
                queryParams.Add($"status={Uri.EscapeDataString(status)}");
            if (!string.IsNullOrWhiteSpace(category) && category != "All")
                queryParams.Add($"category={Uri.EscapeDataString(category)}");
            if (from.HasValue)
                queryParams.Add($"from={Uri.EscapeDataString(from.Value.ToString("o"))}");
            if (to.HasValue)
                queryParams.Add($"to={Uri.EscapeDataString(to.Value.ToString("o"))}");

            var queryString = queryParams.Count > 0 ? "?" + string.Join("&", queryParams) : string.Empty;
            var url = $"api/v1/incidents{queryString}";
            return await _http.GetFromJsonAsync<ApiResponse<List<IncidentDetailViewModel>>>(url, cancellationToken);
        }
        catch (Exception ex)
        {
            return ApiResponse<List<IncidentDetailViewModel>>.FailureResult($"Failed to retrieve incidents: {ex.Message}");
        }
    }

    public async Task<ApiResponse<IncidentDetailViewModel>?> CreateIncidentAsync(
        CreateIncidentRequestDto request, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _http.PostAsJsonAsync("api/v1/incidents", request, cancellationToken);
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<IncidentDetailViewModel>>(cancellationToken: cancellationToken);
            
            if (response.IsSuccessStatusCode && result != null)
            {
                return result;
            }

            return result ?? ApiResponse<IncidentDetailViewModel>.FailureResult("Failed to create incident: Unknown server error.");
        }
        catch (Exception ex)
        {
            return ApiResponse<IncidentDetailViewModel>.FailureResult($"Failed to submit incident: {ex.Message}");
        }
    }

    public async Task<ApiResponse<IncidentDetailViewModel>?> UpdateIncidentAsync(
        Guid id, 
        UpdateIncidentRequestDto request, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _http.PutAsJsonAsync($"api/v1/incidents/{id}", request, cancellationToken);
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<IncidentDetailViewModel>>(cancellationToken: cancellationToken);

            if (response.IsSuccessStatusCode && result != null)
            {
                return result;
            }

            return result ?? ApiResponse<IncidentDetailViewModel>.FailureResult("Failed to update incident: Unknown server error.");
        }
        catch (Exception ex)
        {
            return ApiResponse<IncidentDetailViewModel>.FailureResult($"Failed to update incident: {ex.Message}");
        }
    }

    public async Task<ApiResponse<IncidentDetailViewModel>?> ChangeIncidentStatusAsync(
        Guid id, 
        ChangeIncidentStatusRequestDto request, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _http.PatchAsJsonAsync($"api/v1/incidents/{id}/status", request, cancellationToken);
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<IncidentDetailViewModel>>(cancellationToken: cancellationToken);

            if (response.IsSuccessStatusCode && result != null)
            {
                return result;
            }

            return result ?? ApiResponse<IncidentDetailViewModel>.FailureResult("Failed to change incident status: Unknown server error.");
        }
        catch (Exception ex)
        {
            return ApiResponse<IncidentDetailViewModel>.FailureResult($"Failed to change incident status: {ex.Message}");
        }
    }

    public Task<ApiResponse<IncidentDetailViewModel>?> UpdateStatusAsync(
        Guid id, 
        string status, 
        string? completionNotes = null, 
        CancellationToken cancellationToken = default)
    {
        return ChangeIncidentStatusAsync(id, new ChangeIncidentStatusRequestDto
        {
            Status = status,
            CompletionNotes = completionNotes
        }, cancellationToken);
    }
}
