using SocarDispatch.Web.Models.Common;
using SocarDispatch.Web.Models.Dispatch;

namespace SocarDispatch.Web.Services;

public interface ICategoryService
{
    Task<ApiResponse<List<IncidentCategoryDto>>?> GetCategoriesAsync(CancellationToken cancellationToken = default);
    Task<ApiResponse<IncidentCategoryDto>?> CreateCategoryAsync(CreateIncidentCategoryRequestDto request, CancellationToken cancellationToken = default);
    Task<ApiResponse<IncidentCategoryDto>?> UpdateCategoryAsync(Guid id, UpdateIncidentCategoryRequestDto request, CancellationToken cancellationToken = default);
    Task<ApiResponse<bool>?> DeleteCategoryAsync(Guid id, CancellationToken cancellationToken = default);
}
