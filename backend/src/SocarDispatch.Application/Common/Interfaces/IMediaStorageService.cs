using SocarDispatch.Application.Common.Models;

namespace SocarDispatch.Application.Common.Interfaces;

public interface IMediaStorageService
{
    Task<MediaUploadResult> UploadAsync(
        Stream fileStream,
        string fileName,
        string contentType,
        string category,
        CancellationToken ct = default);

    Task<MediaUploadResult> UploadFeedbackMediaAsync(
        Stream fileStream,
        string fileName,
        string contentType,
        Guid feedbackId,
        CancellationToken ct = default);

    Task<string> GetPreSignedUrlAsync(string objectKey, TimeSpan expiry, string? bucketName = null, CancellationToken ct = default);
    Task DeleteAsync(string objectKey, string? bucketName = null, CancellationToken ct = default);
}
