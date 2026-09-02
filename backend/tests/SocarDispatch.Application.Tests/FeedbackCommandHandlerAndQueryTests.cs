using System.Text;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Moq;
using SocarDispatch.Application.Common.Interfaces;
using SocarDispatch.Application.Common.Models;
using SocarDispatch.Application.Features.Feedbacks.Commands.CreateFeedback;
using SocarDispatch.Application.Features.Feedbacks.Queries.GetFeedbackById;
using SocarDispatch.Application.Features.Feedbacks.Queries.GetFeedbacks;
using SocarDispatch.Domain.Entities;
using SocarDispatch.Domain.Enums;
using SocarDispatch.Domain.Exceptions;
using SocarDispatch.Infrastructure.Persistence;
using Xunit;

namespace SocarDispatch.Application.Tests;

public class FeedbackCommandHandlerAndQueryTests
{
    private static ApplicationDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task CreateFeedbackCommandHandler_ShouldPersistFeedbackAndUploadMedia()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var user = new User
        {
            Id = Guid.NewGuid(),
            FirstName = "Ali",
            LastName = "Veli",
            Email = "ali.veli@socar.local",
            Phone = "+905551112233",
            Department = "Refinery",
            PasswordHash = "hash"
        };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var storageMock = new Mock<IMediaStorageService>();
        storageMock.Setup(s => s.UploadFeedbackMediaAsync(
                It.IsAny<Stream>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MediaUploadResult("feedbacks/img.jpg", "https://minio.socar.local/feedbacks/img.jpg", 1024));

        var fileMock = new Mock<IFormFile>();
        var stream = new MemoryStream(Encoding.UTF8.GetBytes("test-content"));
        fileMock.Setup(f => f.OpenReadStream()).Returns(stream);
        fileMock.Setup(f => f.FileName).Returns("photo.jpg");
        fileMock.Setup(f => f.ContentType).Returns("image/jpeg");
        fileMock.Setup(f => f.Length).Returns(stream.Length);

        var handler = new CreateFeedbackCommandHandler(context, storageMock.Object);
        var command = new CreateFeedbackCommand(user.Id, "Tank 4 Leak Report", "Inspection notes", new List<IFormFile> { fileMock.Object });

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Title.Should().Be("Tank 4 Leak Report");
        result.Data.Status.Should().Be(FeedbackStatus.Pending);
        result.Data.MediaAttachments.Should().HaveCount(1);
        result.Data.MediaAttachments[0].MediaUrl.Should().Be("https://minio.socar.local/feedbacks/img.jpg");

        var savedFeedback = await context.Feedbacks.Include(f => f.MediaAttachments).FirstOrDefaultAsync(f => f.Id == result.Data.Id);
        savedFeedback.Should().NotBeNull();
        savedFeedback!.MediaAttachments.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetFeedbacksQueryHandler_ShouldFilterAndPaginateCorrectly()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var user = new User
        {
            Id = Guid.NewGuid(),
            FirstName = "Operator",
            LastName = "User",
            Email = "operator@socar.local",
            Phone = "+905550001122",
            Department = "Dispatch",
            PasswordHash = "hash"
        };
        context.Users.Add(user);

        context.Feedbacks.AddRange(
            new Feedback { Id = Guid.NewGuid(), UserId = user.Id, Title = "Pending 1", Description = "Desc", Status = FeedbackStatus.Pending, CreatedAt = DateTime.UtcNow.AddMinutes(-10) },
            new Feedback { Id = Guid.NewGuid(), UserId = user.Id, Title = "Resolved 1", Description = "Desc", Status = FeedbackStatus.Resolved, CreatedAt = DateTime.UtcNow.AddMinutes(-5) },
            new Feedback { Id = Guid.NewGuid(), UserId = user.Id, Title = "Pending 2", Description = "Desc", Status = FeedbackStatus.Pending, CreatedAt = DateTime.UtcNow }
        );
        await context.SaveChangesAsync();

        var handler = new GetFeedbacksQueryHandler(context);
        var query = new GetFeedbacksQuery { Status = FeedbackStatus.Pending, PageNumber = 1, PageSize = 10 };

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        result.Data!.TotalCount.Should().Be(2);
        result.Data.Items.Should().OnlyContain(f => f.Status == FeedbackStatus.Pending);
    }

    [Fact]
    public async Task GetFeedbackByIdQueryHandler_ShouldEnforceAuthorization()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var ownerId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();

        var owner = new User { Id = ownerId, FirstName = "Owner", LastName = "User", Email = "owner@socar.local", Phone = "1", Department = "A", PasswordHash = "h" };
        context.Users.Add(owner);

        var feedback = new Feedback
        {
            Id = Guid.NewGuid(),
            UserId = ownerId,
            Title = "Valve Fault",
            Description = "Valve 12 issue",
            Status = FeedbackStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };
        context.Feedbacks.Add(feedback);
        await context.SaveChangesAsync();

        var handler = new GetFeedbackByIdQueryHandler(context);

        // Act & Assert 1: Other Employee receives 403 Forbidden
        var unauthorizedAct = () => handler.Handle(new GetFeedbackByIdQuery(feedback.Id, otherUserId, "Employee"), CancellationToken.None);
        await unauthorizedAct.Should().ThrowAsync<ForbiddenAccessException>();

        // Act & Assert 2: Feedback Owner succeeds
        var ownerResult = await handler.Handle(new GetFeedbackByIdQuery(feedback.Id, ownerId, "Employee"), CancellationToken.None);
        ownerResult.Success.Should().BeTrue();
        ownerResult.Data!.Id.Should().Be(feedback.Id);

        // Act & Assert 3: Operator succeeds even if not the owner
        var operatorResult = await handler.Handle(new GetFeedbackByIdQuery(feedback.Id, otherUserId, "Operator"), CancellationToken.None);
        operatorResult.Success.Should().BeTrue();
        operatorResult.Data!.Id.Should().Be(feedback.Id);
    }
}
