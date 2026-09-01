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
            var response = await _http.GetFromJsonAsync<ApiResponse<PagedResult<IncidentDetailViewModel>>>(
                "api/v1/incidents?status=Open,Assigned&pageSize=1000", 
                cancellationToken);

            if (response != null && response.Success && response.Data != null)
            {
                return ApiResponse<List<IncidentDetailViewModel>>.SuccessResult(response.Data.Items, response.Message);
            }

            return response != null
                ? ApiResponse<List<IncidentDetailViewModel>>.FailureResult(response.Message)
                : ApiResponse<List<IncidentDetailViewModel>>.FailureResult("Failed to retrieve active incidents.");
        }
        catch (Exception ex)
        {
            return ApiResponse<List<IncidentDetailViewModel>>.FailureResult($"Failed to retrieve active incidents: {ex.Message}");
        }
    }

        public async Task<ApiResponse<PagedResult<IncidentDetailViewModel>>?> GetIncidentsAsync(
        int pageNumber = 1,
        int pageSize = 25,
        string? searchTerm = null,
        string? status = null,
        string? category = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var queryParams = new List<string>
            {
                $"pageNumber={Math.Max(1, pageNumber)}",
                $"pageSize={Math.Clamp(pageSize, 1, 100)}"
            };

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var cleanedSearch = searchTerm.Trim();
                if (cleanedSearch.StartsWith("#INC-", StringComparison.OrdinalIgnoreCase))
                    cleanedSearch = cleanedSearch.Substring(5).Trim();
                else if (cleanedSearch.StartsWith("INC-", StringComparison.OrdinalIgnoreCase))
                    cleanedSearch = cleanedSearch.Substring(4).Trim();
                else if (cleanedSearch.StartsWith("#", StringComparison.OrdinalIgnoreCase))
                    cleanedSearch = cleanedSearch.Substring(1).Trim();

                if (!string.IsNullOrWhiteSpace(cleanedSearch))
                    queryParams.Add($"searchTerm={Uri.EscapeDataString(cleanedSearch)}");
            }

            if (!string.IsNullOrWhiteSpace(status) && !status.Equals("All", StringComparison.OrdinalIgnoreCase))
                queryParams.Add($"status={Uri.EscapeDataString(status)}");

            if (!string.IsNullOrWhiteSpace(category) && !category.Equals("All", StringComparison.OrdinalIgnoreCase))
                queryParams.Add($"category={Uri.EscapeDataString(category)}");

            if (fromDate.HasValue)
                queryParams.Add($"fromDate={Uri.EscapeDataString(fromDate.Value.ToString("o"))}");

            if (toDate.HasValue)
                queryParams.Add($"toDate={Uri.EscapeDataString(toDate.Value.ToString("o"))}");

            var queryString = "?" + string.Join("&", queryParams);
            var url = $"api/v1/incidents{queryString}";

            return await _http.GetFromJsonAsync<ApiResponse<PagedResult<IncidentDetailViewModel>>>(url, cancellationToken);
        }
        catch (Exception ex)
        {
            return ApiResponse<PagedResult<IncidentDetailViewModel>>.FailureResult($"Failed to retrieve incidents: {ex.Message}");
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
            var response = await GetIncidentsAsync(
                pageNumber: 1,
                pageSize: 100,
                status: status,
                category: category,
                fromDate: from,
                toDate: to,
                cancellationToken: cancellationToken);

            if (response != null && response.Success && response.Data != null)
            {
                return ApiResponse<List<IncidentDetailViewModel>>.SuccessResult(response.Data.Items, response.Message);
            }

            return response != null
                ? ApiResponse<List<IncidentDetailViewModel>>.FailureResult(response.Message)
                : ApiResponse<List<IncidentDetailViewModel>>.FailureResult("Failed to retrieve incidents.");
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
