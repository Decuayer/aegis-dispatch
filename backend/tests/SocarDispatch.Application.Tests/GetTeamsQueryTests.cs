using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SocarDispatch.Application.Features.Teams.Queries.GetTeams;
using SocarDispatch.Domain.Entities;
using SocarDispatch.Domain.Enums;
using SocarDispatch.Infrastructure.Persistence;
using Xunit;

namespace SocarDispatch.Application.Tests;

public class GetTeamsQueryTests
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
    public async Task GetTeamsQuery_DefaultPagination_ShouldReturnFirstPageWithDefaultSize()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();

        for (int i = 1; i <= 30; i++)
        {
            context.Teams.Add(new Team
            {
                TeamName = $"Team {i:D2}",
                Status = TeamStatus.Idle
            });
        }
        await context.SaveChangesAsync();

        var handler = new GetTeamsQueryHandler(context);
        var query = new GetTeamsQuery(); // Defaults: PageNumber = 1, PageSize = 25

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Data.Items.Should().HaveCount(25);
        result.Data.TotalCount.Should().Be(30);
        result.Data.PageNumber.Should().Be(1);
        result.Data.PageSize.Should().Be(25);
        result.Data.TotalPages.Should().Be(2);
        result.Data.HasNextPage.Should().BeTrue();
    }

    [Fact]
    public async Task GetTeamsQuery_FilterByStatus_ShouldReturnMatchingTeams()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();

        var idleTeam = new Team { TeamName = "Idle Team", Status = TeamStatus.Idle };
        var busyTeam = new Team { TeamName = "Busy Team", Status = TeamStatus.Busy };
        var onSceneTeam = new Team { TeamName = "OnScene Team", Status = TeamStatus.OnScene };

        context.Teams.AddRange(idleTeam, busyTeam, onSceneTeam);
        await context.SaveChangesAsync();

        var handler = new GetTeamsQueryHandler(context);
        var query = new GetTeamsQuery { Status = TeamStatus.Idle };

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Data.Items.Should().HaveCount(1);
        result.Data.TotalCount.Should().Be(1);
        result.Data.Items.First().TeamName.Should().Be("Idle Team");
    }

    [Fact]
    public async Task GetTeamsQuery_FilterByTeamId_ShouldReturnExactTeamDirectly()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();

        var targetTeam = new Team { TeamName = "Target Alpha Team", Status = TeamStatus.Idle };
        var otherTeam = new Team { TeamName = "Other Beta Team", Status = TeamStatus.Idle };

        context.Teams.AddRange(targetTeam, otherTeam);
        await context.SaveChangesAsync();

        var handler = new GetTeamsQueryHandler(context);
        var query = new GetTeamsQuery { TeamId = targetTeam.Id };

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Data.Items.Should().HaveCount(1);
        result.Data.TotalCount.Should().Be(1);
        result.Data.Items.First().Id.Should().Be(targetTeam.Id);
        result.Data.Items.First().TeamName.Should().Be("Target Alpha Team");
    }

    [Fact]
    public async Task GetTeamsQuery_SearchByTerm_ShouldReturnMatchingTeamsCaseInsensitively()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();

        var team1 = new Team { TeamName = "Kocaeli Kurtarma Ekibi", Status = TeamStatus.Idle };
        var team2 = new Team { TeamName = "İzmir Yangın Müdahale", Status = TeamStatus.Idle };

        context.Teams.AddRange(team1, team2);
        await context.SaveChangesAsync();

        var handler = new GetTeamsQueryHandler(context);
        var query = new GetTeamsQuery { SearchTerm = "kurtarma" };

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Data.Items.Should().HaveCount(1);
        result.Data.TotalCount.Should().Be(1);
        result.Data.Items.First().TeamName.Should().Be("Kocaeli Kurtarma Ekibi");
    }

    [Fact]
    public void GetTeamsQueryValidator_ValidInputs_ShouldPassValidation()
    {
        // Arrange
        var validator = new GetTeamsQueryValidator();
        var validQuery = new GetTeamsQuery
        {
            PageNumber = 1,
            PageSize = 25,
            SearchTerm = "Alpha"
        };

        // Act
        var result = validator.Validate(validQuery);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void GetTeamsQueryValidator_ExcessiveSearchTerm_ShouldFailValidation()
    {
        // Arrange
        var validator = new GetTeamsQueryValidator();
        var invalidQuery = new GetTeamsQuery
        {
            SearchTerm = new string('x', 105)
        };

        // Act
        var result = validator.Validate(invalidQuery);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "SearchTerm");
    }
}
