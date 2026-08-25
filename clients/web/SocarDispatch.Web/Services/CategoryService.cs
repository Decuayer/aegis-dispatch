using System.Net.Http.Json;
using SocarDispatch.Web.Models.Common;
using SocarDispatch.Web.Models.Dispatch;

namespace SocarDispatch.Web.Services;

public class CategoryService : ICategoryService
{
    private readonly HttpClient _http;
    private const string BaseEndpoint = "api/v1/incident-categories";

    public CategoryService(HttpClient http)
    {
        _http = http;
    }

    public async Task<ApiResponse<List<IncidentCategoryDto>>?> GetCategoriesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _http.GetFromJsonAsync<ApiResponse<List<IncidentCategoryDto>>>(BaseEndpoint, cancellationToken);
        }
        catch (Exception ex)
        {
            return ApiResponse<List<IncidentCategoryDto>>.FailureResult($"Failed to fetch categories: {ex.Message}");
        }
    }

    public async Task<ApiResponse<IncidentCategoryDto>?> CreateCategoryAsync(CreateIncidentCategoryRequestDto request, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _http.PostAsJsonAsync(BaseEndpoint, request, cancellationToken);
            return await response.Content.ReadFromJsonAsync<ApiResponse<IncidentCategoryDto>>(cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            return ApiResponse<IncidentCategoryDto>.FailureResult($"Failed to create category: {ex.Message}");
        }
    }

    public async Task<ApiResponse<IncidentCategoryDto>?> UpdateCategoryAsync(Guid id, UpdateIncidentCategoryRequestDto request, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _http.PutAsJsonAsync($"{BaseEndpoint}/{id}", request, cancellationToken);
            return await response.Content.ReadFromJsonAsync<ApiResponse<IncidentCategoryDto>>(cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            return ApiResponse<IncidentCategoryDto>.FailureResult($"Failed to update category: {ex.Message}");
        }
    }

    public async Task<ApiResponse<bool>?> DeleteCategoryAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _http.DeleteAsync($"{BaseEndpoint}/{id}", cancellationToken);
            return await response.Content.ReadFromJsonAsync<ApiResponse<bool>>(cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            return ApiResponse<bool>.FailureResult($"Failed to delete category: {ex.Message}");
        }
    }
}
