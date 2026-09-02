using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using SocarDispatch.Application.Common.Models;
using SocarDispatch.Application.Features.Teams.DTOs;
using SocarDispatch.Domain.Enums;
using SocarDispatch.IntegrationTests.Common;
using SocarDispatch.IntegrationTests.Fixtures;

namespace SocarDispatch.IntegrationTests.Features.Pagination;

public class PaginationQueryTests : IntegrationTestBase
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public PaginationQueryTests(CustomWebApplicationFactory factory) : base(factory)
    {
    }

    private async Task SeedSampleTeamsAsync(int count = 5)
    {
        for (var i = 1; i <= count; i++)
        {
            await SeedTeamAsync($"Unit-{i:D2}", TeamStatus.Idle);
        }
    }

    [Fact]
    public async Task GetTeams_WithDefaultPagination_ReturnsCorrectMetadata()
    {
        // Arrange
        await SeedSampleTeamsAsync(5);
        var client = Factory.CreateAuthenticatedClient(RoleType.Team);

        // Act
        var response = await client.GetAsync("/api/v1/teams?pageNumber=1&pageSize=25");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<PagedResult<TeamDto>>>(JsonOptions);
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();

        var page = result.Data;
        page.Should().NotBeNull();
        page.PageNumber.Should().Be(1);
        page.PageSize.Should().Be(25);
        page.TotalCount.Should().Be(5);
        page.TotalPages.Should().Be(1);
        page.HasPreviousPage.Should().BeFalse();
        page.HasNextPage.Should().BeFalse();
        page.Items.Should().HaveCount(5);
    }

    [Fact]
    public async Task GetTeams_WithPageOverflow_ReturnsEmptyItemsAndValidMetadata()
    {
        // Arrange
        await SeedSampleTeamsAsync(5);
        var client = Factory.CreateAuthenticatedClient(RoleType.Team);

        // Act
        var response = await client.GetAsync("/api/v1/teams?pageNumber=10&pageSize=10");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<PagedResult<TeamDto>>>(JsonOptions);
        result.Should().NotBeNull();

        var page = result!.Data;
        page.PageNumber.Should().Be(10);
        page.PageSize.Should().Be(10);
        page.TotalCount.Should().Be(5);
        page.TotalPages.Should().Be(1);
        page.HasPreviousPage.Should().BeTrue();
        page.HasNextPage.Should().BeFalse();
        page.Items.Should().BeEmpty();
    }

    [Theory]
    [InlineData(-1, 0)]
    [InlineData(0, -5)]
    [InlineData(-10, -10)]
    public async Task GetTeams_WithNegativeOrZeroPagination_FallsBackToDefaults(int pageNumber, int pageSize)
    {
        // Arrange
        await SeedSampleTeamsAsync(3);
        var client = Factory.CreateAuthenticatedClient(RoleType.Team);

        // Act
        var response = await client.GetAsync($"/api/v1/teams?pageNumber={pageNumber}&pageSize={pageSize}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<PagedResult<TeamDto>>>(JsonOptions);
        result.Should().NotBeNull();

        var page = result!.Data;
        page.PageNumber.Should().Be(1);
        page.PageSize.Should().Be(25);
        page.TotalCount.Should().Be(3);
        page.Items.Should().HaveCount(3);
    }

    [Fact]
    public async Task GetTeams_WithPageSizeExceedingMaxLimit_ClampsTo100()
    {
        // Arrange
        await SeedSampleTeamsAsync(2);
        var client = Factory.CreateAuthenticatedClient(RoleType.Team);

        // Act
        var response = await client.GetAsync("/api/v1/teams?pageNumber=1&pageSize=250");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<PagedResult<TeamDto>>>(JsonOptions);
        result.Should().NotBeNull();

        var page = result!.Data;
        page.PageSize.Should().Be(100);
        page.PageNumber.Should().Be(1);
        page.TotalCount.Should().Be(2);
    }
}
