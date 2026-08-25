using SocarDispatch.Web.Models.Common;
using SocarDispatch.Web.Models.Dispatch;

namespace SocarDispatch.Web.Services;

public interface IEmergencyCodeService
{
    Task<ApiResponse<List<EmergencyCodeDto>>?> GetEmergencyCodesAsync(CancellationToken cancellationToken = default);
    Task<ApiResponse<EmergencyCodeDto>?> CreateEmergencyCodeAsync(CreateEmergencyCodeRequestDto request, CancellationToken cancellationToken = default);
    Task<ApiResponse<EmergencyCodeDto>?> UpdateEmergencyCodeAsync(Guid id, UpdateEmergencyCodeRequestDto request, CancellationToken cancellationToken = default);
    Task<ApiResponse<bool>?> DeleteEmergencyCodeAsync(Guid id, CancellationToken cancellationToken = default);
}
