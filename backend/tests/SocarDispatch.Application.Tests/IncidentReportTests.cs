using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using SocarDispatch.Application.Features.Reports.Commands.CreateReport;
using SocarDispatch.Application.Features.Reports.Queries.GetReportsByIncident;
using SocarDispatch.Domain.Entities;
using SocarDispatch.Domain.Enums;
using SocarDispatch.Domain.Exceptions;
using SocarDispatch.Infrastructure.Persistence;
using Xunit;

namespace SocarDispatch.Application.Tests;

public class IncidentReportTests
{
    private static ApplicationDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var context = new ApplicationDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    [Fact]
    public async Task CreateIncidentReport_WhenAssignedTeamMember_ShouldSucceed()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();

        var reporterUser = new User
        {
            FirstName = "Ali",
            LastName = "Kaya",
            Email = "ali.kaya@socar.az",
            Phone = "+994501234567",
            PasswordHash = "hash",
            Department = "Arama Kurtarma",
            RoleType = RoleType.Team
        };
        context.Users.Add(reporterUser);

        var operatorUser = new User
        {
            FirstName = "Operator",
            LastName = "User",
            Email = "operator@socar.az",
            Phone = "+994507654321",
            PasswordHash = "hash",
            Department = "Komuta Merkezi",
            RoleType = RoleType.Operator
        };
        context.Users.Add(operatorUser);

        var team = new Team { TeamName = "Fire Müdahale Ekibi 1", LeaderId = reporterUser.Id, Status = TeamStatus.Forwarded };
        context.Teams.Add(team);

        var incident = new Incident
        {
            ReporterId = reporterUser.Id,
            Category = "Fire",
            EmergencyCode = "RED-1",
            Description = "A Blok Fire",
            Status = IncidentStatus.Assigned,
            Latitude = 40.4093m,
            Longitude = 49.8671m,
            Location = new Point(49.8671, 40.4093) { SRID = 4326 }
        };
        context.Incidents.Add(incident);

        var assignment = new Assignment
        {
            IncidentId = incident.Id,
            TeamId = team.Id,
            OperatorId = operatorUser.Id,
            AssignedAt = DateTime.UtcNow,
            CompletedAt = null
        };
        context.Assignments.Add(assignment);

        await context.SaveChangesAsync();

        var handler = new CreateIncidentReportCommandHandler(context);
        var command = new CreateIncidentReportCommand(incident.Id, reporterUser.Id, "Saha müdahalesi başladı. Alevler kontrol altına alınıyor.", "https://minio.socar.az/reports/photo1.jpg", team.Id);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Data.Content.Should().Be("Saha müdahalesi başladı. Alevler kontrol altına alınıyor.");
        result.Data.MediaUrl.Should().Be("https://minio.socar.az/reports/photo1.jpg");
        result.Data.TeamName.Should().Be("Fire Müdahale Ekibi 1");
        result.Data.ReportedByFullName.Should().Be("Ali Kaya");
    }

    [Fact]
    public async Task CreateIncidentReport_WhenTeamNotAssigned_ShouldThrowForbiddenAccessException()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();

        var reporterUser = new User
        {
            FirstName = "Veli",
            LastName = "Demir",
            Email = "veli.demir@socar.az",
            Phone = "+994501112233",
            PasswordHash = "hash",
            Department = "Sağlık",
            RoleType = RoleType.Team
        };
        context.Users.Add(reporterUser);

        var team = new Team { TeamName = "Sağlık Ekibi 2", LeaderId = reporterUser.Id, Status = TeamStatus.Idle };
        context.Teams.Add(team);

        var incident = new Incident
        {
            ReporterId = reporterUser.Id,
            Category = "Tıbbi Acil",
            EmergencyCode = "BLUE-2",
            Status = IncidentStatus.Open,
            Latitude = 40.4093m,
            Longitude = 49.8671m,
            Location = new Point(49.8671, 40.4093) { SRID = 4326 }
        };
        context.Incidents.Add(incident);
        await context.SaveChangesAsync();

        var handler = new CreateIncidentReportCommandHandler(context);
        var command = new CreateIncidentReportCommand(incident.Id, reporterUser.Id, "Yetkisiz rapor denemesi", null, team.Id);

        // Act & Assert
        var act = async () => await handler.Handle(command, CancellationToken.None);
        await act.Should().ThrowAsync<ForbiddenAccessException>()
            .WithMessage("*Your team is not actively assigned to this incident.*");
    }

    [Fact]
    public async Task GetReportsByIncident_WhenReportsExist_ShouldReturnInChronologicalOrder()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();

        var user = new User { FirstName = "Can", LastName = "Yılmaz", Email = "can@socar.az", RoleType = RoleType.Team };
        context.Users.Add(user);

        var team = new Team { TeamName = "Arama Kurtarma", LeaderId = user.Id };
        context.Teams.Add(team);

        var incident = new Incident
        {
            ReporterId = user.Id,
            Category = "Deprem",
            EmergencyCode = "RED-3",
            Latitude = 40.4m,
            Longitude = 49.8m,
            Location = new Point(49.8, 40.4) { SRID = 4326 }
        };
        context.Incidents.Add(incident);

        var report1 = new IncidentReport { IncidentId = incident.Id, TeamId = team.Id, ReportedByUserId = user.Id, Content = "İlk Not", ReportedAt = DateTime.UtcNow.AddMinutes(-10) };
        var report2 = new IncidentReport { IncidentId = incident.Id, TeamId = team.Id, ReportedByUserId = user.Id, Content = "İkinci Not", ReportedAt = DateTime.UtcNow.AddMinutes(-2) };
        context.IncidentReports.AddRange(report1, report2);
        await context.SaveChangesAsync();

        var handler = new GetReportsByIncidentQueryHandler(context);
        var query = new GetReportsByIncidentQuery(incident.Id);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Data.Should().HaveCount(2);
        result.Data[0].Content.Should().Be("İlk Not");
        result.Data[1].Content.Should().Be("İkinci Not");
    }
}
