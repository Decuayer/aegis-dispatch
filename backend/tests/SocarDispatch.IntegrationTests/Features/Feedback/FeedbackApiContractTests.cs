using System.Net;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SocarDispatch.Domain.Entities;
using SocarDispatch.Domain.Enums;
using SocarDispatch.IntegrationTests.Common;
using SocarDispatch.IntegrationTests.Fixtures;

namespace SocarDispatch.IntegrationTests.Features.FeedbackTests;

public class FeedbackApiContractTests : IntegrationTestBase
{
    public FeedbackApiContractTests(CustomWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task GetFeedbacks_EndpointNotYetPublished_ReturnsNotFound()
    {
        // Arrange
        var user = await SeedUserAsync("Test", "User", role: RoleType.Employee);
        var client = Factory.CreateAuthenticatedClient(user);

        // Act
        var response = await client.GetAsync("/api/v1/feedbacks");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task FeedbackEntity_CreationAndPersistence_PersistsPendingStatusAndMedia()
    {
        // Arrange
        var user = await SeedUserAsync("Engin", "Akyurek", role: RoleType.Employee);

        // Act
        var feedbackId = await ExecuteDbContextAsync(async context =>
        {
            var feedback = new Feedback
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                Title = "Saha aydınlatması yetersiz",
                Description = "Tank sahası 3 numaralı bölgede gece aydınlatması çalışmıyor.",
                Status = FeedbackStatus.Pending,
                CreatedAt = DateTime.UtcNow,
                MediaAttachments = new List<FeedbackMedia>
                {
                    new()
                    {
                        Id = Guid.NewGuid(),
                        MediaUrl = "https://minio.socar.local/feedback-attachments/tank3.jpg",
                        MediaType = "image/jpeg",
                        CreatedAt = DateTime.UtcNow
                    }
                }
            };
            context.Feedbacks.Add(feedback);
            await context.SaveChangesAsync();
            return feedback.Id;
        });

        // Assert
        await ExecuteDbContextAsync(async context =>
        {
            var saved = await context.Feedbacks
                .Include(f => f.MediaAttachments)
                .FirstOrDefaultAsync(f => f.Id == feedbackId);

            saved.Should().NotBeNull();
            saved!.Status.Should().Be(FeedbackStatus.Pending);
            saved.MediaAttachments.Should().ContainSingle();
            saved.MediaAttachments.First().MediaType.Should().Be("image/jpeg");
        });
    }

    [Fact]
    public async Task FeedbackEntity_UserDeletion_RestrictedByForeignKeyConstraint()
    {
        // Arrange
        var user = await SeedUserAsync("Kenan", "Imirzalioglu", role: RoleType.Employee);

        await ExecuteDbContextAsync(async context =>
        {
            context.Feedbacks.Add(new Feedback
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                Title = "Yol guzergahi engeli",
                Description = "Gecis guzergahinda malzeme yıgını var.",
                Status = FeedbackStatus.Pending,
                CreatedAt = DateTime.UtcNow
            });
            await context.SaveChangesAsync();
        });

        // Act & Assert
        var deleteAction = async () =>
        {
            await ExecuteDbContextAsync(async context =>
            {
                var userToDelete = await context.Users.FindAsync(user.Id);
                context.Users.Remove(userToDelete!);
                await context.SaveChangesAsync();
            });
        };

        await deleteAction.Should().ThrowAsync<DbUpdateException>();
    }

    [Fact]
    public async Task FeedbackEntity_FeedbackDeletion_CascadesToFeedbackMedia()
    {
        // Arrange
        var user = await SeedUserAsync("Kivanc", "Tatlitug", role: RoleType.Employee);
        var mediaId = Guid.NewGuid();
        var feedbackId = Guid.NewGuid();

        await ExecuteDbContextAsync(async context =>
        {
            var feedback = new Feedback
            {
                Id = feedbackId,
                UserId = user.Id,
                Title = "Acil cıkıs kapısı",
                Description = "Kilit mekanizması zorlanıyor.",
                Status = FeedbackStatus.Pending,
                CreatedAt = DateTime.UtcNow,
                MediaAttachments = new List<FeedbackMedia>
                {
                    new()
                    {
                        Id = mediaId,
                        MediaUrl = "https://minio.socar.local/feedback-attachments/kapi.jpg",
                        MediaType = "image/jpeg",
                        CreatedAt = DateTime.UtcNow
                    }
                }
            };
            context.Feedbacks.Add(feedback);
            await context.SaveChangesAsync();
        });

        // Act
        await ExecuteDbContextAsync(async context =>
        {
            var feedbackToDelete = await context.Feedbacks.FindAsync(feedbackId);
            context.Feedbacks.Remove(feedbackToDelete!);
            await context.SaveChangesAsync();
        });

        // Assert
        await ExecuteDbContextAsync(async context =>
        {
            var remainingMedia = await context.FeedbackMedia.FirstOrDefaultAsync(m => m.Id == mediaId);
            remainingMedia.Should().BeNull();
        });
    }
}
