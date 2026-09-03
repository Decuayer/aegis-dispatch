using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SocarDispatch.Application.Features.Users.Commands.DeleteUser;
using SocarDispatch.Domain.Entities;
using SocarDispatch.Domain.Enums;
using SocarDispatch.Domain.Exceptions;
using SocarDispatch.Infrastructure.Persistence;
using Xunit;

namespace SocarDispatch.Application.Tests;

public class DeleteUserCommandTests
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
    public async Task DeleteUser_WhenValidUser_ShouldDeleteSuccessfully()
    {
        using var context = CreateInMemoryDbContext();

        var operatorUser = new User
        {
            Id = Guid.NewGuid(),
            FirstName = "Operator",
            LastName = "Admin",
            Email = "op@socar.az",
            Phone = "+994501111111",
            PasswordHash = "hash",
            Department = "Operations",
            RoleType = RoleType.Operator
        };

        var targetUser = new User
        {
            Id = Guid.NewGuid(),
            FirstName = "Ali",
            LastName = "Mammadov",
            Email = "ali@socar.az",
            Phone = "+994502222222",
            PasswordHash = "hash",
            Department = "Security",
            RoleType = RoleType.Employee
        };

        context.Users.AddRange(operatorUser, targetUser);
        await context.SaveChangesAsync();

        var handler = new DeleteUserCommandHandler(context);
        var result = await handler.Handle(new DeleteUserCommand(targetUser.Id, operatorUser.Id), CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Data.Should().BeTrue();

        var deleted = await context.Users.FindAsync(targetUser.Id);
        deleted.Should().BeNull();
    }

    [Fact]
    public async Task DeleteUser_WhenOperatorTriesToDeleteThemselves_ShouldThrowDomainException()
    {
        using var context = CreateInMemoryDbContext();

        var operatorUser = new User
        {
            Id = Guid.NewGuid(),
            FirstName = "Operator",
            LastName = "Admin",
            Email = "op@socar.az",
            Phone = "+994501111111",
            PasswordHash = "hash",
            Department = "Operations",
            RoleType = RoleType.Operator
        };

        context.Users.Add(operatorUser);
        await context.SaveChangesAsync();

        var handler = new DeleteUserCommandHandler(context);
        var act = async () => await handler.Handle(new DeleteUserCommand(operatorUser.Id, operatorUser.Id), CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("*cannot delete their own account*");
    }

    [Fact]
    public async Task DeleteUser_WhenUserHasActiveIncidents_ShouldThrowDomainException()
    {
        using var context = CreateInMemoryDbContext();

        var operatorUser = new User
        {
            Id = Guid.NewGuid(),
            FirstName = "Operator",
            LastName = "Admin",
            Email = "op@socar.az",
            Phone = "+994501111111",
            PasswordHash = "hash",
            Department = "Operations",
            RoleType = RoleType.Operator
        };

        var reporterUser = new User
        {
            Id = Guid.NewGuid(),
            FirstName = "Veli",
            LastName = "Aliyev",
            Email = "veli@socar.az",
            Phone = "+994503333333",
            PasswordHash = "hash",
            Department = "Refining",
            RoleType = RoleType.Employee
        };

        var incident = new Incident
        {
            Id = Guid.NewGuid(),
            Category = "Fire",
            EmergencyCode = "Red",
            Description = "Tank leak test",
            ReporterId = reporterUser.Id,
            Latitude = 40.375m,
            Longitude = 49.832m,
            Location = new NetTopologySuite.Geometries.Point(49.832, 40.375) { SRID = 4326 },
            Status = IncidentStatus.Open
        };

        context.Users.AddRange(operatorUser, reporterUser);
        context.Incidents.Add(incident);
        await context.SaveChangesAsync();

        var handler = new DeleteUserCommandHandler(context);
        var act = async () => await handler.Handle(new DeleteUserCommand(reporterUser.Id, operatorUser.Id), CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("*associated with existing incident reports*");
    }

    [Fact]
    public async Task DeleteUser_WhenUserDoesNotExist_ShouldThrowEntityNotFoundException()
    {
        using var context = CreateInMemoryDbContext();
        var handler = new DeleteUserCommandHandler(context);
        var randomId = Guid.NewGuid();

        var act = async () => await handler.Handle(new DeleteUserCommand(randomId, Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<EntityNotFoundException>();
    }
}
