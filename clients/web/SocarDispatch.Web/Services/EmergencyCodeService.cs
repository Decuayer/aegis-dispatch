using System.Net.Http.Json;
using SocarDispatch.Web.Models.Common;
using SocarDispatch.Web.Models.Dispatch;

namespace SocarDispatch.Web.Services;

public class EmergencyCodeService : IEmergencyCodeService
{
    private readonly HttpClient _http;
    private const string BaseEndpoint = "api/v1/emergency-codes";

    public EmergencyCodeService(HttpClient http)
    {
        _http = http;
    }

    public async Task<ApiResponse<List<EmergencyCodeDto>>?> GetEmergencyCodesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _http.GetFromJsonAsync<ApiResponse<List<EmergencyCodeDto>>>(BaseEndpoint, cancellationToken);
        }
        catch (Exception ex)
        {
            return ApiResponse<List<EmergencyCodeDto>>.FailureResult($"Failed to fetch emergency codes: {ex.Message}");
        }
    }

    public async Task<ApiResponse<EmergencyCodeDto>?> CreateEmergencyCodeAsync(CreateEmergencyCodeRequestDto request, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _http.PostAsJsonAsync(BaseEndpoint, request, cancellationToken);
            return await response.Content.ReadFromJsonAsync<ApiResponse<EmergencyCodeDto>>(cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            return ApiResponse<EmergencyCodeDto>.FailureResult($"Failed to create emergency code: {ex.Message}");
        }
    }

    public async Task<ApiResponse<EmergencyCodeDto>?> UpdateEmergencyCodeAsync(Guid id, UpdateEmergencyCodeRequestDto request, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _http.PutAsJsonAsync($"{BaseEndpoint}/{id}", request, cancellationToken);
            return await response.Content.ReadFromJsonAsync<ApiResponse<EmergencyCodeDto>>(cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            return ApiResponse<EmergencyCodeDto>.FailureResult($"Failed to update emergency code: {ex.Message}");
        }
    }

    public async Task<ApiResponse<bool>?> DeleteEmergencyCodeAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _http.DeleteAsync($"{BaseEndpoint}/{id}", cancellationToken);
            return await response.Content.ReadFromJsonAsync<ApiResponse<bool>>(cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            return ApiResponse<bool>.FailureResult($"Failed to delete emergency code: {ex.Message}");
        }
    }
}
