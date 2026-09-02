using Microsoft.Extensions.Logging;
using Minio;
using Minio.DataModel.Args;
using SocarDispatch.Application.Common.Interfaces;
using SocarDispatch.Infrastructure.Settings;

namespace SocarDispatch.Infrastructure.Storage;

public class MinioBucketInitializer : IStorageInitializer
{
    private readonly IMinioClient _minioClient;
    private readonly MinioSettings _settings;
    private readonly ILogger<MinioBucketInitializer> _logger;

    public MinioBucketInitializer(
        MinioSettings settings,
        ILogger<MinioBucketInitializer> logger,
        IMinioClient? minioClient = null)
    {
        _settings = settings;
        _logger = logger;
        _minioClient = minioClient ?? new MinioClient()
            .WithEndpoint(_settings.Endpoint)
            .WithCredentials(_settings.AccessKey, _settings.SecretKey)
            .WithSSL(_settings.UseSSL)
            .Build();
    }

    public async Task InitializeStorageAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("Starting MinIO object storage bucket provisioning...");

        var buckets = new[]
        {
            _settings.BucketName,
            _settings.FeedbackBucketName
        };

        foreach (var bucket in buckets)
        {
            if (string.IsNullOrWhiteSpace(bucket)) continue;

            await EnsureBucketWithPolicyAsync(bucket, ct);
        }

        _logger.LogInformation("MinIO object storage bucket provisioning completed successfully.");
    }

    private async Task EnsureBucketWithPolicyAsync(string bucketName, CancellationToken ct)
    {
        try
        {
            var existsArgs = new BucketExistsArgs().WithBucket(bucketName);
            bool exists = await _minioClient.BucketExistsAsync(existsArgs, ct);

            if (!exists)
            {
                _logger.LogInformation("Bucket '{BucketName}' does not exist. Creating...", bucketName);
                var makeArgs = new MakeBucketArgs().WithBucket(bucketName);
                await _minioClient.MakeBucketAsync(makeArgs, ct);
                _logger.LogInformation("Bucket '{BucketName}' created successfully.", bucketName);
            }
            else
            {
                _logger.LogInformation("Bucket '{BucketName}' already exists. Verifying policies...", bucketName);
            }

            // Apply read-only / download policy for media distribution
            var policyJson = $$"""
            {
              "Version": "2012-10-17",
              "Statement": [
                {
                  "Effect": "Allow",
                  "Principal": {"AWS": ["*"]},
                  "Action": ["s3:GetBucketLocation", "s3:ListBucket"],
                  "Resource": ["arn:aws:s3:::{{bucketName}}"]
                },
                {
                  "Effect": "Allow",
                  "Principal": {"AWS": ["*"]},
                  "Action": ["s3:GetObject"],
                  "Resource": ["arn:aws:s3:::{{bucketName}}/*"]
                }
              ]
            }
            """;

            var setPolicyArgs = new SetPolicyArgs()
                .WithBucket(bucketName)
                .WithPolicy(policyJson);

            await _minioClient.SetPolicyAsync(setPolicyArgs, ct);
            _logger.LogInformation("Bucket policy applied to '{BucketName}'.", bucketName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize MinIO bucket '{BucketName}'.", bucketName);
            throw;
        }
    }
}
