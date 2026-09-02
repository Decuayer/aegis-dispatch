using Microsoft.Extensions.Logging;
using Minio;
using Minio.DataModel.Args;
using Moq;
using SocarDispatch.Infrastructure.Settings;
using SocarDispatch.Infrastructure.Storage;
using Xunit;

namespace SocarDispatch.Infrastructure.Tests;

public class MinioBucketInitializerTests
{
    private readonly Mock<IMinioClient> _minioClientMock;
    private readonly Mock<ILogger<MinioBucketInitializer>> _loggerMock;
    private readonly MinioSettings _settings;

    public MinioBucketInitializerTests()
    {
        _minioClientMock = new Mock<IMinioClient>();
        _loggerMock = new Mock<ILogger<MinioBucketInitializer>>();
        _settings = new MinioSettings
        {
            Endpoint = "localhost:9000",
            BucketName = "test-media-bucket",
            FeedbackBucketName = "test-feedback-bucket"
        };
    }

    [Fact]
    public async Task InitializeStorageAsync_ShouldCreateBuckets_WhenTheyDoNotExist()
    {
        // Arrange: buckets do not exist
        _minioClientMock
            .Setup(c => c.BucketExistsAsync(It.IsAny<BucketExistsArgs>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _minioClientMock
            .Setup(c => c.MakeBucketAsync(It.IsAny<MakeBucketArgs>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _minioClientMock
            .Setup(c => c.SetPolicyAsync(It.IsAny<SetPolicyArgs>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var initializer = new MinioBucketInitializer(_settings, _loggerMock.Object, _minioClientMock.Object);

        // Act
        await initializer.InitializeStorageAsync();

        // Assert: BucketExists checked for both, MakeBucket called twice, SetPolicy called twice
        _minioClientMock.Verify(
            c => c.BucketExistsAsync(It.IsAny<BucketExistsArgs>(), It.IsAny<CancellationToken>()),
            Times.Exactly(2));

        _minioClientMock.Verify(
            c => c.MakeBucketAsync(It.IsAny<MakeBucketArgs>(), It.IsAny<CancellationToken>()),
            Times.Exactly(2));

        _minioClientMock.Verify(
            c => c.SetPolicyAsync(It.IsAny<SetPolicyArgs>(), It.IsAny<CancellationToken>()),
            Times.Exactly(2));
    }

    [Fact]
    public async Task InitializeStorageAsync_ShouldNotCreateBucket_WhenItAlreadyExists()
    {
        // Arrange: buckets already exist
        _minioClientMock
            .Setup(c => c.BucketExistsAsync(It.IsAny<BucketExistsArgs>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _minioClientMock
            .Setup(c => c.SetPolicyAsync(It.IsAny<SetPolicyArgs>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var initializer = new MinioBucketInitializer(_settings, _loggerMock.Object, _minioClientMock.Object);

        // Act
        await initializer.InitializeStorageAsync();

        // Assert: MakeBucket should never be called
        _minioClientMock.Verify(
            c => c.BucketExistsAsync(It.IsAny<BucketExistsArgs>(), It.IsAny<CancellationToken>()),
            Times.Exactly(2));

        _minioClientMock.Verify(
            c => c.MakeBucketAsync(It.IsAny<MakeBucketArgs>(), It.IsAny<CancellationToken>()),
            Times.Never);

        // Policy should still be verified/set
        _minioClientMock.Verify(
            c => c.SetPolicyAsync(It.IsAny<SetPolicyArgs>(), It.IsAny<CancellationToken>()),
            Times.Exactly(2));
    }
}
