using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using SocarDispatch.Application.Features.Incidents.Commands.DeleteIncident;
using SocarDispatch.Domain.Entities;
using SocarDispatch.Domain.Enums;
using SocarDispatch.Domain.Exceptions;
using SocarDispatch.Infrastructure.Persistence;
using Xunit;

namespace SocarDispatch.Application.Tests;

public class DeleteIncidentCommandTests
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
    public async Task Handle_ExistingIncident_SetsIsDeletedAndDeletedAtWithoutPhysicalDeletion()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();

        var reporter = new User
        {
            FirstName = "Ahmet",
            LastName = "Yilmaz",
            Email = "ahmet@socar.az",
            Phone = "+994501112233",
            Department = "Fire Safety",
            PasswordHash = "hash",
            RoleType = RoleType.Employee
        };
        context.Users.Add(reporter);
        await context.SaveChangesAsync();

        var incident = new Incident
        {
            ReporterId = reporter.Id,
            Category = "Fire",
            EmergencyCode = "RED-1",
            Status = IncidentStatus.Open,
            Location = new Point(49.8671, 40.4093) { SRID = 4326 }
        };
        context.Incidents.Add(incident);
        await context.SaveChangesAsync();

        var handler = new DeleteIncidentCommandHandler(context);
        var command = new DeleteIncidentCommand(incident.Id, reporter.Id);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().BeTrue();

        // Verify entity is NOT physically deleted, but soft-deleted
        var rawIncident = await context.Incidents
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(i => i.Id == incident.Id);

        rawIncident.Should().NotBeNull();
        rawIncident!.IsDeleted.Should().BeTrue();
        rawIncident.DeletedAt.Should().NotBeNull();
        rawIncident.DeletedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));

        // Verify standard query filter excludes it
        var queryFilteredIncident = await context.Incidents
            .FirstOrDefaultAsync(i => i.Id == incident.Id);

        queryFilteredIncident.Should().BeNull();
    }

    [Fact]
    public async Task Handle_NonExistentIncident_ThrowsEntityNotFoundException()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var handler = new DeleteIncidentCommandHandler(context);
        var command = new DeleteIncidentCommand(Guid.NewGuid(), Guid.NewGuid());

        // Act & Assert
        await FluentActions.Invoking(() => handler.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<EntityNotFoundException>();
    }

    [Fact]
    public async Task Handle_UnauthorizedUser_ThrowsForbiddenAccessException()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();

        var reporter = new User
        {
            FirstName = "Orhan",
            LastName = "Aliyev",
            Email = "orhan@socar.az",
            Phone = "+994501112233",
            Department = "Fire Safety",
            PasswordHash = "hash",
            RoleType = RoleType.Employee
        };
        var otherUser = new User
        {
            FirstName = "Elmir",
            LastName = "Hasanov",
            Email = "elmir@socar.az",
            Phone = "+994509998877",
            Department = "Security",
            PasswordHash = "hash",
            RoleType = RoleType.Employee
        };
        context.Users.AddRange(reporter, otherUser);
        await context.SaveChangesAsync();

        var incident = new Incident
        {
            ReporterId = reporter.Id,
            Category = "Gas",
            EmergencyCode = "YELLOW-1",
            Status = IncidentStatus.Open,
            Location = new Point(49.8671, 40.4093) { SRID = 4326 }
        };
        context.Incidents.Add(incident);
        await context.SaveChangesAsync();

        var handler = new DeleteIncidentCommandHandler(context);
        var command = new DeleteIncidentCommand(incident.Id, otherUser.Id);

        // Act & Assert
        await FluentActions.Invoking(() => handler.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<ForbiddenAccessException>();
    }

    [Fact]
    public async Task Handle_OperatorUser_DeletesSuccessfullyEvenIfNotReporter()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();

        var reporter = new User
        {
            FirstName = "Orhan",
            LastName = "Aliyev",
            Email = "orhan@socar.az",
            Phone = "+994501112233",
            Department = "Fire Safety",
            PasswordHash = "hash",
            RoleType = RoleType.Employee
        };
        var operatorUser = new User
        {
            FirstName = "Operator",
            LastName = "Admin",
            Email = "operator@socar.az",
            Phone = "+994505554433",
            Department = "Control Center",
            PasswordHash = "hash",
            RoleType = RoleType.Operator
        };
        context.Users.AddRange(reporter, operatorUser);
        await context.SaveChangesAsync();

        var incident = new Incident
        {
            ReporterId = reporter.Id,
            Category = "Fire",
            EmergencyCode = "RED-1",
            Status = IncidentStatus.Open,
            Location = new Point(49.8671, 40.4093) { SRID = 4326 }
        };
        context.Incidents.Add(incident);
        await context.SaveChangesAsync();

        var handler = new DeleteIncidentCommandHandler(context);
        var command = new DeleteIncidentCommand(incident.Id, operatorUser.Id, "Operator");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().BeTrue();

        var rawIncident = await context.Incidents
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(i => i.Id == incident.Id);

        rawIncident.Should().NotBeNull();
        rawIncident!.IsDeleted.Should().BeTrue();
    }
}
