using Minio;
using Minio.DataModel.Args;
using SocarDispatch.Application.Common.Interfaces;
using SocarDispatch.Application.Common.Models;
using SocarDispatch.Domain.Exceptions;
using SocarDispatch.Infrastructure.Settings;

namespace SocarDispatch.Infrastructure.Services;

public class MinioStorageService : IMediaStorageService
{
    private static readonly HashSet<string> AllowedMimeTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg",
        "image/png",
        "image/webp",
        "video/mp4",
        "video/quicktime"
    };

    private const long MaxFileSizeBytes = 50 * 1024 * 1024; // 50 MB (52,428,800 bytes)

    private readonly IMinioClient _minioClient;
    private readonly MinioSettings _settings;

    public MinioStorageService(MinioSettings settings)
    {
        _settings = settings;

        _minioClient = new MinioClient()
            .WithEndpoint(_settings.Endpoint)
            .WithCredentials(_settings.AccessKey, _settings.SecretKey)
            .WithSSL(_settings.UseSSL)
            .Build();
    }

    public async Task<MediaUploadResult> UploadAsync(
        Stream fileStream,
        string fileName,
        string contentType,
        string category,
        CancellationToken ct = default)
    {
        // 1. MIME Type Validation
        if (!AllowedMimeTypes.Contains(contentType))
        {
            throw new DomainException(
                $"Desteklenmeyen dosya tipi: '{contentType}'. Yalnızca JPEG, PNG, WEBP, MP4 ve QuickTime formatları kabul edilmektedir.");
        }

        // 2. File Size Validation (50MB)
        if (fileStream.Length > MaxFileSizeBytes)
        {
            throw new DomainException("Dosya boyutu 50MB limitini aşıyor.");
        }

        // 3. Bucket Existence Check
        await EnsureBucketExistsAsync(_settings.BucketName, ct);

        // 4. Object Key Formatting: {category}/yyyy/MM/dd/{Guid}{extension}
        var extension = Path.GetExtension(fileName);
        if (string.IsNullOrEmpty(extension))
        {
            extension = contentType switch
            {
                "image/jpeg" => ".jpg",
                "image/png" => ".png",
                "image/webp" => ".webp",
                "video/mp4" => ".mp4",
                "video/quicktime" => ".mov",
                _ => ""
            };
        }

        var sanitizedCategory = string.IsNullOrWhiteSpace(category) ? "general" : category.Trim().ToLowerInvariant();
        var objectKey = $"{sanitizedCategory}/{DateTime.UtcNow:yyyy/MM/dd}/{Guid.NewGuid()}{extension}";

        // 5. Upload to MinIO
        fileStream.Position = 0;
        var putArgs = new PutObjectArgs()
            .WithBucket(_settings.BucketName)
            .WithObject(objectKey)
            .WithStreamData(fileStream)
            .WithObjectSize(fileStream.Length)
            .WithContentType(contentType);

        await _minioClient.PutObjectAsync(putArgs, ct);

        // 6. Form Public Access URL
        var publicUrl = $"{_settings.PublicEndpoint.TrimEnd('/')}/{_settings.BucketName}/{objectKey}";

        return new MediaUploadResult(objectKey, publicUrl, fileStream.Length);
    }

    public async Task<MediaUploadResult> UploadFeedbackMediaAsync(
        Stream fileStream,
        string fileName,
        string contentType,
        Guid feedbackId,
        CancellationToken ct = default)
    {
        // 1. MIME Type Validation
        if (!AllowedMimeTypes.Contains(contentType))
        {
            throw new DomainException(
                $"Desteklenmeyen dosya tipi: '{contentType}'. Yalnızca JPEG, PNG, WEBP, MP4 ve QuickTime formatları kabul edilmektedir.");
        }

        // 2. File Size Validation (10MB for images, 50MB for videos)
        var maxLimit = contentType.StartsWith("video/", StringComparison.OrdinalIgnoreCase)
            ? 50 * 1024 * 1024
            : 10 * 1024 * 1024;

        if (fileStream.Length > maxLimit)
        {
            throw new DomainException($"Dosya boyutu izin verilen sınırı aşıyor ({(maxLimit / (1024 * 1024))}MB).");
        }

        // 3. Target Bucket & Existence Check
        var targetBucket = string.IsNullOrWhiteSpace(_settings.FeedbackBucketName)
            ? "socar-dispatch-feedbacks"
            : _settings.FeedbackBucketName;
        await EnsureBucketExistsAsync(targetBucket, ct);

        // 4. Object Key Formatting: {feedbackId}/{uniqueFileName}
        var cleanFileName = Path.GetFileName(fileName);
        if (string.IsNullOrWhiteSpace(cleanFileName))
        {
            var ext = contentType switch
            {
                "image/jpeg" => ".jpg",
                "image/png" => ".png",
                "image/webp" => ".webp",
                "video/mp4" => ".mp4",
                "video/quicktime" => ".mov",
                _ => ""
            };
            cleanFileName = $"{Guid.NewGuid()}{ext}";
        }
        else
        {
            var ext = Path.GetExtension(cleanFileName);
            var nameWithoutExt = Path.GetFileNameWithoutExtension(cleanFileName);
            cleanFileName = $"{nameWithoutExt}_{Guid.NewGuid():N}{ext}";
        }

        var objectKey = $"{feedbackId}/{cleanFileName}";

        // 5. Upload to MinIO
        fileStream.Position = 0;
        var putArgs = new PutObjectArgs()
            .WithBucket(targetBucket)
            .WithObject(objectKey)
            .WithStreamData(fileStream)
            .WithObjectSize(fileStream.Length)
            .WithContentType(contentType);

        await _minioClient.PutObjectAsync(putArgs, ct);

        // 6. Form Public Access URL
        var publicUrl = $"{_settings.PublicEndpoint.TrimEnd('/')}/{targetBucket}/{objectKey}";

        return new MediaUploadResult(objectKey, publicUrl, fileStream.Length);
    }

    public async Task<string> GetPreSignedUrlAsync(string objectKey, TimeSpan expiry, string? bucketName = null, CancellationToken ct = default)
    {
        var targetBucket = string.IsNullOrWhiteSpace(bucketName) ? _settings.BucketName : bucketName;
        var presignedArgs = new PresignedGetObjectArgs()
            .WithBucket(targetBucket)
            .WithObject(objectKey)
            .WithExpiry((int)expiry.TotalSeconds);

        return await _minioClient.PresignedGetObjectAsync(presignedArgs);
    }

    public async Task DeleteAsync(string objectKey, string? bucketName = null, CancellationToken ct = default)
    {
        var targetBucket = string.IsNullOrWhiteSpace(bucketName) ? _settings.BucketName : bucketName;
        var removeArgs = new RemoveObjectArgs()
            .WithBucket(targetBucket)
            .WithObject(objectKey);

        await _minioClient.RemoveObjectAsync(removeArgs, ct);
    }

    private async Task EnsureBucketExistsAsync(string bucketName, CancellationToken ct)
    {
        var existsArgs = new BucketExistsArgs().WithBucket(bucketName);
        bool found = await _minioClient.BucketExistsAsync(existsArgs, ct);
        if (!found)
        {
            var makeArgs = new MakeBucketArgs().WithBucket(bucketName);
            await _minioClient.MakeBucketAsync(makeArgs, ct);
        }
    }
}
