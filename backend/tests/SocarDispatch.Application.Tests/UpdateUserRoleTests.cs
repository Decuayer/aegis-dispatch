using FluentAssertions;
using FluentValidation.TestHelper;
using Microsoft.EntityFrameworkCore;
using SocarDispatch.Application.Features.Users.Commands.UpdateUserRole;
using SocarDispatch.Domain.Entities;
using SocarDispatch.Domain.Enums;
using SocarDispatch.Domain.Exceptions;
using SocarDispatch.Infrastructure.Persistence;
using Xunit;

namespace SocarDispatch.Application.Tests;

public class UpdateUserRoleTests
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
    public async Task UpdateUserRole_WhenOperatorPromotesEmployeeToTeam_ShouldSucceed()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();

        var operatorUser = new User
        {
            FirstName = "Zeynep",
            LastName = "Operator",
            Email = "operator@socar.com",
            Phone = "+905551112233",
            PasswordHash = "hash",
            Department = "Merkez",
            RoleType = RoleType.Operator
        };

        var employeeUser = new User
        {
            FirstName = "Ahmet",
            LastName = "Yılmaz",
            Email = "ahmet@socar.com",
            Phone = "+905552223344",
            PasswordHash = "hash",
            Department = "İSG",
            RoleType = RoleType.Employee
        };

        context.Users.AddRange(operatorUser, employeeUser);
        await context.SaveChangesAsync();

        var handler = new UpdateUserRoleCommandHandler(context);
        var command = new UpdateUserRoleCommand(employeeUser.Id, RoleType.Team, "İtfaiye Eri", operatorUser.Id);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Data.RoleType.Should().Be(RoleType.Team);
        result.Data.SubRole.Should().Be("İtfaiye Eri");

        var updatedUserInDb = await context.Users.FindAsync(employeeUser.Id);
        updatedUserInDb.Should().NotBeNull();
        updatedUserInDb!.RoleType.Should().Be(RoleType.Team);
        updatedUserInDb.SubRole.Should().Be("İtfaiye Eri");
    }

    [Fact]
    public async Task UpdateUserRole_WhenOperatorTriesSelfDemotion_ShouldThrowDomainException()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();

        var operatorUser = new User
        {
            FirstName = "Operatör",
            LastName = "Self",
            Email = "opself@socar.com",
            Phone = "+905553334455",
            PasswordHash = "hash",
            Department = "Merkez",
            RoleType = RoleType.Operator
        };

        context.Users.Add(operatorUser);
        await context.SaveChangesAsync();

        var handler = new UpdateUserRoleCommandHandler(context);
        var command = new UpdateUserRoleCommand(operatorUser.Id, RoleType.Employee, null, operatorUser.Id);

        // Act & Assert
        var act = async () => await handler.Handle(command, CancellationToken.None);
        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("Operators cannot demote their own account.");
    }

    [Fact]
    public async Task UpdateUserRole_WhenDemotingTeamMemberWithActiveDispatch_ShouldThrowDomainException()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();

        var operatorUser = new User
        {
            FirstName = "Zeynep",
            LastName = "Op",
            Email = "zeynepop@socar.com",
            Phone = "+905554445566",
            PasswordHash = "hash",
            Department = "Merkez",
            RoleType = RoleType.Operator
        };

        var teamUser = new User
        {
            FirstName = "Mehmet",
            LastName = "Demir",
            Email = "mehmet@socar.com",
            Phone = "+905555556677",
            PasswordHash = "hash",
            Department = "Arama Kurtarma",
            RoleType = RoleType.Team
        };

        var reporterUser = new User
        {
            FirstName = "Reporter",
            LastName = "User",
            Email = "reporter@socar.com",
            Phone = "+905556667788",
            PasswordHash = "hash",
            Department = "Saha",
            RoleType = RoleType.Employee
        };

        context.Users.AddRange(operatorUser, teamUser, reporterUser);

        var team = new Team { TeamName = "Kurtarma 1", Status = TeamStatus.Busy, LeaderId = teamUser.Id };
        team.Members.Add(new TeamMember { UserId = teamUser.Id });
        context.Teams.Add(team);

        var incident = new Incident
        {
            Category = "Yangın",
            EmergencyCode = "Kırmızı Kod",
            Description = "A1 Blok yangın",
            Latitude = 40.0m,
            Longitude = 29.0m,
            Location = new NetTopologySuite.Geometries.Point(29.0, 40.0) { SRID = 4326 },
            ReporterId = reporterUser.Id,
            Status = IncidentStatus.Assigned
        };
        context.Incidents.Add(incident);

        var assignment = new Assignment
        {
            Incident = incident,
            Team = team,
            OperatorId = operatorUser.Id,
            AssignedAt = DateTime.UtcNow,
            CompletedAt = null // Aktif görev!
        };
        context.Assignments.Add(assignment);

        await context.SaveChangesAsync();

        var handler = new UpdateUserRoleCommandHandler(context);
        var command = new UpdateUserRoleCommand(teamUser.Id, RoleType.Employee, null, operatorUser.Id);

        // Act & Assert
        var act = async () => await handler.Handle(command, CancellationToken.None);
        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("Cannot demote user from Team role while currently assigned to an active field dispatch.");
    }

    [Fact]
    public async Task UpdateUserRole_WhenDemotingTeamMemberWithCompletedDispatch_ShouldSucceed()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();

        var operatorUser = new User
        {
            FirstName = "Zeynep",
            LastName = "Op",
            Email = "zeynepop2@socar.com",
            Phone = "+905557778899",
            PasswordHash = "hash",
            Department = "Merkez",
            RoleType = RoleType.Operator
        };

        var teamUser = new User
        {
            FirstName = "Hasan",
            LastName = "Kaya",
            Email = "hasan@socar.com",
            Phone = "+905558889900",
            PasswordHash = "hash",
            Department = "İtfaiye",
            RoleType = RoleType.Team
        };

        var reporterUser = new User
        {
            FirstName = "Reporter2",
            LastName = "User2",
            Email = "reporter2@socar.com",
            Phone = "+905559990011",
            PasswordHash = "hash",
            Department = "Saha",
            RoleType = RoleType.Employee
        };

        context.Users.AddRange(operatorUser, teamUser, reporterUser);

        var team = new Team { TeamName = "İtfaiye 2", Status = TeamStatus.Idle, LeaderId = teamUser.Id };
        team.Members.Add(new TeamMember { UserId = teamUser.Id });
        context.Teams.Add(team);

        var incident = new Incident
        {
            Category = "Küçük Yangın",
            EmergencyCode = "Sarı Kod",
            Description = "Kontrol altına alındı",
            Latitude = 40.0m,
            Longitude = 29.0m,
            Location = new NetTopologySuite.Geometries.Point(29.0, 40.0) { SRID = 4326 },
            ReporterId = reporterUser.Id,
            Status = IncidentStatus.Resolved
        };
        context.Incidents.Add(incident);

        var assignment = new Assignment
        {
            Incident = incident,
            Team = team,
            OperatorId = operatorUser.Id,
            AssignedAt = DateTime.UtcNow.AddHours(-2),
            CompletedAt = DateTime.UtcNow.AddHours(-1) // Tamamlanmış görev!
        };
        context.Assignments.Add(assignment);

        await context.SaveChangesAsync();

        var handler = new UpdateUserRoleCommandHandler(context);
        var command = new UpdateUserRoleCommand(teamUser.Id, RoleType.Employee, null, operatorUser.Id);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Data.RoleType.Should().Be(RoleType.Employee);
    }

    [Fact]
    public void UpdateUserRoleCommandValidator_WithInvalidRoleType_ShouldHaveValidationError()
    {
        // Arrange
        var validator = new UpdateUserRoleCommandValidator();
        var command = new UpdateUserRoleCommand(Guid.NewGuid(), (RoleType)999, null, Guid.NewGuid());

        // Act
        var result = validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.RoleType)
            .WithErrorMessage("Invalid role type.");
    }

    [Fact]
    public void UpdateUserRoleCommandValidator_WithEmptyUserId_ShouldHaveValidationError()
    {
        // Arrange
        var validator = new UpdateUserRoleCommandValidator();
        var command = new UpdateUserRoleCommand(Guid.Empty, RoleType.Team, null, Guid.NewGuid());

        // Act
        var result = validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.UserId)
            .WithErrorMessage("Target user ID is required.");
    }
}
