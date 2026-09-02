using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using SocarDispatch.Application.Common.Models;
using SocarDispatch.Application.Features.Users.DTOs;
using SocarDispatch.Domain.Entities;
using SocarDispatch.Domain.Enums;
using SocarDispatch.IntegrationTests.Common;
using SocarDispatch.IntegrationTests.Fixtures;
using Xunit;

namespace SocarDispatch.IntegrationTests.Features.Users;

public class GetCurrentUserEndpointTests : IntegrationTestBase
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public GetCurrentUserEndpointTests(CustomWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task GetCurrentUser_AuthenticatedUser_ReturnsOkWithCurrentUserDto()
    {
        // Arrange
        var user = await SeedUserAsync("Murat", "Yilmaz", role: RoleType.Employee, department: "HSE");
        var client = Factory.CreateAuthenticatedClient(user);

        // Act
        var response = await client.GetAsync("/api/v1/users/me");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<CurrentUserDto>>(JsonOptions);
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Id.Should().Be(user.Id);
        result.Data.FirstName.Should().Be("Murat");
        result.Data.LastName.Should().Be("Yilmaz");
        result.Data.Department.Should().Be("HSE");
        result.Data.RoleType.Should().Be(RoleType.Employee);
    }

    [Fact]
    public async Task GetCurrentUser_Unauthenticated_ReturnsUnauthorized()
    {
        // Arrange
        var client = Factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/v1/users/me");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetCurrentUser_UserWithTeam_ReturnsOkWithActiveTeamId()
    {
        // Arrange
        var user = await SeedUserAsync("Serkan", "Demir", role: RoleType.Team, department: "Fire Safety");
        var team = new Team { Id = Guid.NewGuid(), TeamName = "Alpha Rescue Unit" };
        var teamMember = new TeamMember
        {
            TeamId = team.Id,
            UserId = user.Id,
            MemberStatus = TeamMemberStatus.Available
        };

        await ExecuteDbContextAsync(async context =>
        {
            context.Teams.Add(team);
            context.TeamMembers.Add(teamMember);
            await context.SaveChangesAsync();
        });

        var client = Factory.CreateAuthenticatedClient(user);

        // Act
        var response = await client.GetAsync("/api/v1/users/me");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<CurrentUserDto>>(JsonOptions);
        result.Should().NotBeNull();
        result!.Data.Should().NotBeNull();
        result.Data!.ActiveTeamId.Should().Be(team.Id);
    }
}
