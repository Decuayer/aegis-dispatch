using FluentAssertions;
using FluentValidation.TestHelper;
using Microsoft.EntityFrameworkCore;
using SocarDispatch.Application.Features.Teams.Commands.AddTeamMember;
using SocarDispatch.Domain.Entities;
using SocarDispatch.Domain.Enums;
using SocarDispatch.Domain.Exceptions;
using SocarDispatch.Infrastructure.Persistence;
using Xunit;

namespace SocarDispatch.UnitTests.Application.Teams;

public class AddTeamMemberCommandHandlerTests
{
    private readonly AddTeamMemberCommandValidator _validator = new();

    private static ApplicationDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task Handle_WhenTeamAtMaxCapacity_ShouldThrowDomainException()
    {
        using var context = CreateInMemoryDbContext();
        var leader = new User { Id = Guid.NewGuid(), FirstName = "Team", LastName = "Leader", Email = "leader@socar.az", RoleType = RoleType.Team, PasswordHash = "hash", Department = "HSE" };
        var team = new Team { Id = Guid.NewGuid(), TeamName = "Alpha Unit", LeaderId = leader.Id, Status = TeamStatus.Idle };

        context.Users.Add(leader);
        context.Teams.Add(team);

        // Populate team to max capacity (10 members)
        for (int i = 0; i < 10; i++)
        {
            var user = new User { Id = Guid.NewGuid(), FirstName = $"Member{i}", LastName = "Unit", Email = $"m{i}@socar.az", RoleType = RoleType.Team, PasswordHash = "hash", Department = "HSE" };
            context.Users.Add(user);
            team.Members.Add(new TeamMember { TeamId = team.Id, UserId = user.Id });
        }
        await context.SaveChangesAsync();

        var extraUser = new User { Id = Guid.NewGuid(), FirstName = "Extra", LastName = "User", Email = "extra@socar.az", RoleType = RoleType.Team, PasswordHash = "hash", Department = "HSE" };
        context.Users.Add(extraUser);
        await context.SaveChangesAsync();

        var handler = new AddTeamMemberCommandHandler(context);
        var command = new AddTeamMemberCommand(
            TeamId: team.Id,
            RequesterId: leader.Id,
            UserId: extraUser.Id
        );

        Func<Task> act = async () => await handler.Handle(command, CancellationToken.None);
        await act.Should().ThrowAsync<DomainException>().WithMessage("*maximum operational capacity*");
    }

    [Fact]
    public async Task Handle_UnauthorizedUserAddingMember_ShouldThrowForbiddenAccessException()
    {
        using var context = CreateInMemoryDbContext();
        var team = new Team { Id = Guid.NewGuid(), TeamName = "Bravo Unit", LeaderId = Guid.NewGuid(), Status = TeamStatus.Idle };
        var unauthorizedRequester = new User { Id = Guid.NewGuid(), FirstName = "Employee", LastName = "One", Email = "emp1@socar.az", RoleType = RoleType.Employee, PasswordHash = "hash", Department = "Plant" };
        var targetUser = new User { Id = Guid.NewGuid(), FirstName = "Responder", LastName = "Two", Email = "r2@socar.az", RoleType = RoleType.Team, PasswordHash = "hash", Department = "HSE" };

        context.Teams.Add(team);
        context.Users.AddRange(unauthorizedRequester, targetUser);
        await context.SaveChangesAsync();

        var handler = new AddTeamMemberCommandHandler(context);
        var command = new AddTeamMemberCommand(
            TeamId: team.Id,
            RequesterId: unauthorizedRequester.Id,
            UserId: targetUser.Id
        );

        Func<Task> act = async () => await handler.Handle(command, CancellationToken.None);
        await act.Should().ThrowAsync<ForbiddenAccessException>();
    }

    [Fact]
    public void Validator_EmptyFields_ShouldFailValidation()
    {
        var command = new AddTeamMemberCommand(
            TeamId: Guid.Empty,
            RequesterId: Guid.Empty,
            UserId: Guid.Empty
        );

        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.TeamId);
        result.ShouldHaveValidationErrorFor(x => x.RequesterId);
        result.ShouldHaveValidationErrorFor(x => x.UserId);
    }
}
