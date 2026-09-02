using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SocarDispatch.Application.Features.Users.Queries.GetCurrentUser;
using SocarDispatch.Domain.Entities;
using SocarDispatch.Domain.Enums;
using SocarDispatch.Domain.Exceptions;
using SocarDispatch.Infrastructure.Persistence;
using Xunit;

namespace SocarDispatch.Application.Tests;

public class GetCurrentUserQueryTests
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
    public async Task Handle_ExistingUser_ShouldReturnCurrentUserDtoSuccessfully()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var user = new User
        {
            Id = Guid.NewGuid(),
            FirstName = "Ali",
            LastName = "Kaya",
            Email = "ali.kaya@socar.az",
            Phone = "+905551234567",
            Department = "Fire Safety",
            RoleType = RoleType.Employee,
            SubRole = "Field Safety Specialist",
            AvatarUrl = "https://cdn.socar.az/avatars/ali.png"
        };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var handler = new GetCurrentUserQueryHandler(context);
        var query = new GetCurrentUserQuery(user.Id);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Id.Should().Be(user.Id);
        result.Data.FirstName.Should().Be("Ali");
        result.Data.LastName.Should().Be("Kaya");
        result.Data.Email.Should().Be("ali.kaya@socar.az");
        result.Data.Phone.Should().Be("+905551234567");
        result.Data.PhoneNumber.Should().Be("+905551234567");
        result.Data.Department.Should().Be("Fire Safety");
        result.Data.RoleType.Should().Be(RoleType.Employee);
        result.Data.SubRole.Should().Be("Field Safety Specialist");
        result.Data.AvatarUrl.Should().Be("https://cdn.socar.az/avatars/ali.png");
        result.Data.ActiveTeamId.Should().BeNull();
    }

    [Fact]
    public async Task Handle_UserWithTeamMembership_ShouldPopulateActiveTeamId()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var team = new Team { Id = Guid.NewGuid(), TeamName = "Alpha Rescue" };
        var user = new User
        {
            Id = Guid.NewGuid(),
            FirstName = "Veli",
            LastName = "Demir",
            Email = "veli.demir@socar.az",
            Phone = "+905559876543",
            Department = "Emergency",
            RoleType = RoleType.Team
        };
        var teamMember = new TeamMember
        {
            TeamId = team.Id,
            UserId = user.Id,
            MemberStatus = TeamMemberStatus.Available
        };

        context.Teams.Add(team);
        context.Users.Add(user);
        context.TeamMembers.Add(teamMember);
        await context.SaveChangesAsync();

        var handler = new GetCurrentUserQueryHandler(context);
        var query = new GetCurrentUserQuery(user.Id);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.ActiveTeamId.Should().Be(team.Id);
    }

    [Fact]
    public async Task Handle_UserAsTeamLeader_ShouldPopulateActiveTeamId()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var user = new User
        {
            Id = Guid.NewGuid(),
            FirstName = "Serkan",
            LastName = "Ozturk",
            Email = "serkan.ozturk@socar.az",
            Phone = "+905553334455",
            Department = "Operations",
            RoleType = RoleType.Team
        };
        var team = new Team
        {
            Id = Guid.NewGuid(),
            TeamName = "Bravo Response",
            LeaderId = user.Id
        };

        context.Users.Add(user);
        context.Teams.Add(team);
        await context.SaveChangesAsync();

        var handler = new GetCurrentUserQueryHandler(context);
        var query = new GetCurrentUserQuery(user.Id);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.ActiveTeamId.Should().Be(team.Id);
    }

    [Fact]
    public async Task Handle_NonExistentUser_ShouldThrowEntityNotFoundException()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var handler = new GetCurrentUserQueryHandler(context);
        var nonExistentId = Guid.NewGuid();
        var query = new GetCurrentUserQuery(nonExistentId);

        // Act
        Func<Task> act = async () => await handler.Handle(query, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<EntityNotFoundException>()
            .WithMessage($"*{nonExistentId}*");
    }
}
