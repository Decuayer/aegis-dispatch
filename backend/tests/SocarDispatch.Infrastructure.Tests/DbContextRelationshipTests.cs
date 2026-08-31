using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SocarDispatch.Domain.Entities;
using SocarDispatch.Domain.Enums;
using Xunit;

namespace SocarDispatch.Infrastructure.Tests;

public class DbContextRelationshipTests
{
    // 1. CASCADE DELETE TEST (Member is deleted when Team is deleted)
    [Fact]
    public async Task DeleteTeam_ShouldCascadeDelete_TeamMembers()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var user = new User { FirstName = "Ali", LastName = "Veli", Email = "ali@socar.com", Phone = "+905001112233", PasswordHash = "p", Department = "D", RoleType = RoleType.Team };
        var team = new Team { TeamName = "Kurtarma Ekibi" };

        context.Users.Add(user);
        context.Teams.Add(team);
        await context.SaveChangesAsync();

        context.TeamMembers.Add(new TeamMember { TeamId = team.Id, UserId = user.Id });
        await context.SaveChangesAsync();

        // Act (Delete Team)
        context.Teams.Remove(team);
        await context.SaveChangesAsync();

        // Assert
        var memberExists = await context.TeamMembers.AnyAsync(tm => tm.TeamId == team.Id);
        memberExists.Should().BeFalse(); // Cascade Delete completed
    }

    // 2. SET NULL TEST (Team.LeaderId becomes null when the leader is deleted)

    [Fact]
    public async Task DeleteLeaderUser_ShouldSetTeamLeaderIdToNull()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var leader = new User { FirstName = "Lider", LastName = "Kaptan", Email = "lider@socar.com", Phone = "+905009998877", PasswordHash = "p", Department = "D", RoleType = RoleType.Team };
        var team = new Team { TeamName = "Fire Müdahale" };

        context.Users.Add(leader);
        await context.SaveChangesAsync();
        team.LeaderId = leader.Id;

        context.Teams.Add(team);
        await context.SaveChangesAsync();

        // Act (Delete Leader User)
        context.Users.Remove(leader);
        await context.SaveChangesAsync();

        // Assert
        var updatedTeam = await context.Teams.FindAsync(team.Id);
        updatedTeam.Should().NotBeNull();
        updatedTeam!.LeaderId.Should().BeNull(); // SetNull completed
    }

    // 3. CASCADE DELETE TEST (IncidentMedia records are deleted when Incident is deleted)
    [Fact]
    public async Task DeleteIncident_ShouldCascadeDelete_IncidentMedia()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var reporter = new User { FirstName = "Mehmet", LastName = "Saha", Email = "m.saha@socar.com", Phone = "+905001119988", PasswordHash = "p", Department = "D", RoleType = RoleType.Employee };
        context.Users.Add(reporter);
        await context.SaveChangesAsync();

        var incident = new Incident
        {
            ReporterId = reporter.Id,
            Category = "Fire",
            EmergencyCode = "Kırmızı Kod",
            Latitude = 40.99m,
            Longitude = 29.02m,
            Location = new NetTopologySuite.Geometries.Point(29.02, 40.99) { SRID = 4326 },
            MediaAttachments = new List<IncidentMedia>
            {
                new IncidentMedia { MediaUrl = "http://minio/fire1.jpg", MediaType = MediaType.Photo },
                new IncidentMedia { MediaUrl = "http://minio/fire2.mp4", MediaType = MediaType.Video }
            }
        };


        context.Incidents.Add(incident);
        await context.SaveChangesAsync();

        // Act (Delete Incident)
        context.Incidents.Remove(incident);
        await context.SaveChangesAsync();

        // Assert
        var mediaExists = await context.IncidentMedia.AnyAsync(m => m.IncidentId == incident.Id);
        mediaExists.Should().BeFalse(); // Cascade Delete verified
    }

    // 4. CASCADE DELETE TEST (FeedbackMedia records are deleted when Feedback is deleted)
    [Fact]
    public async Task DeleteFeedback_ShouldCascadeDelete_FeedbackMedia()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var user = new User
        {
            FirstName = "John",
            LastName = "Doe",
            Email = "john.doe@socar.com",
            Phone = "+905001112244",
            PasswordHash = "hashed_pw",
            Department = "Refinery Operations",
            RoleType = RoleType.Employee
        };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var feedback = new Feedback
        {
            UserId = user.Id,
            Title = "Equipment Inspection Needed",
            Description = "Safety valve pressure gauge inspection is required.",
            Status = FeedbackStatus.Pending,
            MediaAttachments = new List<FeedbackMedia>
            {
                new FeedbackMedia { MediaUrl = "http://minio/feedback1.jpg", MediaType = "image/jpeg" },
                new FeedbackMedia { MediaUrl = "http://minio/feedback2.mp4", MediaType = "video/mp4" }
            }
        };
        context.Feedbacks.Add(feedback);
        await context.SaveChangesAsync();

        // Verify model metadata constraint
        var mediaFk = context.Model.FindEntityType(typeof(FeedbackMedia))!
            .GetForeignKeys()
            .First(fk => fk.PrincipalEntityType.ClrType == typeof(Feedback));
        mediaFk.DeleteBehavior.Should().Be(DeleteBehavior.Cascade);

        // Act (Delete Feedback)
        context.Feedbacks.Remove(feedback);
        await context.SaveChangesAsync();

        // Assert
        var mediaExists = await context.FeedbackMedia.AnyAsync(m => m.FeedbackId == feedback.Id);
        mediaExists.Should().BeFalse();
    }

    // 5. RESTRICT DELETE TEST (Deleting a User with existing Feedback records is restricted)
    [Fact]
    public async Task DeleteUser_WithAssociatedFeedback_ShouldBeRestricted()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var user = new User
        {
            FirstName = "Jane",
            LastName = "Smith",
            Email = "jane.smith@socar.com",
            Phone = "+905001112255",
            PasswordHash = "hashed_pw",
            Department = "HSE",
            RoleType = RoleType.Employee
        };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var feedback = new Feedback
        {
            UserId = user.Id,
            Title = "Hazard Report",
            Description = "Dimmed emergency exit illumination at sector B.",
            Status = FeedbackStatus.Pending
        };
        context.Feedbacks.Add(feedback);
        await context.SaveChangesAsync();

        // Verify model metadata configuration
        var userFk = context.Model.FindEntityType(typeof(Feedback))!
            .GetForeignKeys()
            .First(fk => fk.PrincipalEntityType.ClrType == typeof(User));
        userFk.DeleteBehavior.Should().Be(DeleteBehavior.Restrict);

        // Act & Assert (Attempting to remove the user triggers tracking restriction)
        Action act = () => context.Users.Remove(user);
        act.Should().Throw<InvalidOperationException>();
    }

    // 6. ENUM & METADATA TEST (FeedbackStatus maps to string, length is 20, defaults to Pending)
    [Fact]
    public async Task Feedback_ShouldHaveStringConvertedStatus_AndDefaultToPending()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();

        // Verify EF Core metadata configuration
        var statusProperty = context.Model.FindEntityType(typeof(Feedback))!
            .FindProperty(nameof(Feedback.Status));
        statusProperty.Should().NotBeNull();
        statusProperty!.GetProviderClrType().Should().Be(typeof(string));
        statusProperty.GetMaxLength().Should().Be(20);

        var user = new User
        {
            FirstName = "Alice",
            LastName = "Brown",
            Email = "alice.brown@socar.com",
            Phone = "+905001112266",
            PasswordHash = "hashed_pw",
            Department = "Logistics",
            RoleType = RoleType.Employee
        };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        // Act
        var feedback = new Feedback
        {
            UserId = user.Id,
            Title = "System Usability Feedback",
            Description = "Map marker rendering is clear and responsive."
        };

        // Assert default enum value
        feedback.Status.Should().Be(FeedbackStatus.Pending);

        context.Feedbacks.Add(feedback);
        await context.SaveChangesAsync();

        // Assert persisted entity
        var savedFeedback = await context.Feedbacks.FirstAsync(f => f.Id == feedback.Id);
        savedFeedback.Should().NotBeNull();
        savedFeedback.Status.Should().Be(FeedbackStatus.Pending);
    }

}