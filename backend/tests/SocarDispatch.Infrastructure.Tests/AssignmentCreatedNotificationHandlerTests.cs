using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NetTopologySuite.Geometries;
using SocarDispatch.Application.Common.Interfaces;
using SocarDispatch.Domain.Entities;
using SocarDispatch.Domain.Events;
using SocarDispatch.Infrastructure.Notifications;
using SocarDispatch.Domain.Enums;
using Moq;
using Microsoft.AspNetCore.SignalR;
using SocarDispatch.Infrastructure.Hubs;

namespace SocarDispatch.Infrastructure.Tests;

public class AssignmentCreatedNotificationHandlerTests
{
    private class FakePushNotificationService : IPushNotificationService
    {
        public List<string> SentTokens { get; } = new();
        public string? SentTitle { get; private set; }
        public string? SentBody { get; private set; }
        public Dictionary<string, string>? SentData { get; private set; }
        public bool SendMulticastCalled { get; private set; }

        public Task SendAsync(string deviceToken, string title, string body, Dictionary<string, string>? data = null, CancellationToken ct = default)
        {
            return Task.CompletedTask;
        }

        public Task SendMulticastAsync(IEnumerable<string> deviceTokens, string title, string body, Dictionary<string, string>? data = null, CancellationToken ct = default)
        {
            SendMulticastCalled = true;
            SentTokens.AddRange(deviceTokens);
            SentTitle = title;
            SentBody = body;
            SentData = data;
            return Task.CompletedTask;
        }
    }

    [Fact]
    public async Task Handle_ShouldSendMulticastNotification_ToTeamMembersWithDeviceTokens()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var fakePushService = new FakePushNotificationService();
        var logger = NullLogger<AssignmentCreatedNotificationHandler>.Instance;
        var hubContextMock = new Mock<IHubContext<IncidentsHub>>();
        var clientsMock = new Mock<IHubClients>();
        var clientProxyMock = new Mock<IClientProxy>();
        clientsMock.Setup(c => c.All).Returns(clientProxyMock.Object);
        hubContextMock.Setup(h => h.Clients).Returns(clientsMock.Object);
        var handler = new AssignmentCreatedNotificationHandler(context, fakePushService, hubContextMock.Object, logger);


        // 1. Kullanıcılar ve Ekip Üyeleri Hazırla
        var userWithToken1 = new User
        {
            Id = Guid.NewGuid(),
            FirstName = "Ali",
            LastName = "Yılmaz",
            Email = "ali@socar.az",
            Phone = "+994501112233",
            Department = "Fire Safety",
            DeviceToken = "token-device-111"
        };

        var userWithToken2 = new User
        {
            Id = Guid.NewGuid(),
            FirstName = "Veli",
            LastName = "Kaya",
            Email = "veli@socar.az",
            Phone = "+994501112244",
            Department = "Fire Safety",
            DeviceToken = "token-device-222"
        };

        var userWithoutToken = new User
        {
            Id = Guid.NewGuid(),
            FirstName = "Ahmet",
            LastName = "Demir",
            Email = "ahmet@socar.az",
            Phone = "+994501112255",
            Department = "Fire Safety",
            DeviceToken = null
        };

        var team = new Team { Id = Guid.NewGuid(), TeamName = "Alpha Response Team", Status = TeamStatus.Idle };
        var incident = new Incident
        {
            Id = Guid.NewGuid(),
            EmergencyCode = "FIRE-RED-01",
            Category = "Fire",
            Status = IncidentStatus.Open,
            Latitude = 40.3776m,
            Longitude = 49.8920m,
            Location = new Point(49.8920, 40.3776) { SRID = 4326 }
        };

        context.Users.AddRange(userWithToken1, userWithToken2, userWithoutToken);
        context.Teams.Add(team);
        context.Incidents.Add(incident);

        context.TeamMembers.Add(new TeamMember { TeamId = team.Id, UserId = userWithToken1.Id });
        context.TeamMembers.Add(new TeamMember { TeamId = team.Id, UserId = userWithoutToken.Id });
        context.TeamMembers.Add(new TeamMember { TeamId = team.Id, UserId = userWithToken2.Id });

        await context.SaveChangesAsync();

        var notificationEvent = new AssignmentCreatedEvent(
            AssignmentId: Guid.NewGuid(),
            IncidentId: incident.Id,
            TeamId: team.Id,
            OperatorId: Guid.NewGuid(),
            AssignedAt: DateTime.UtcNow
        );

        // Act
        await handler.Handle(notificationEvent, CancellationToken.None);

        // Assert
        fakePushService.SendMulticastCalled.Should().BeTrue();
        fakePushService.SentTokens.Should().HaveCount(2);
        fakePushService.SentTokens.Should().Contain(new[] { "token-device-111", "token-device-222" });
        fakePushService.SentTokens.Should().NotContain("null");

        fakePushService.SentTitle.Should().Contain("FIRE-RED-01");
        fakePushService.SentData.Should().ContainKey("incidentId").WhoseValue.Should().Be(incident.Id.ToString());
        fakePushService.SentData.Should().ContainKey("emergencyCode").WhoseValue.Should().Be("FIRE-RED-01");
        fakePushService.SentData.Should().ContainKey("category").WhoseValue.Should().Be("Fire");
        fakePushService.SentData.Should().ContainKey("type").WhoseValue.Should().Be("DISPATCH_ALERT");
    }
}
