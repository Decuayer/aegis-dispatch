using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using NetTopologySuite.Geometries;
using SocarDispatch.Application.Common.Models;
using SocarDispatch.Application.Features.Incidents.DTOs;
using SocarDispatch.Domain.Entities;
using SocarDispatch.Domain.Enums;
using SocarDispatch.IntegrationTests.Fixtures;
using Xunit;
using SocarDispatch.IntegrationTests.Common;


namespace SocarDispatch.IntegrationTests.Features.Incidents;

public class IncidentFilterAndSortIntegrationTests : IntegrationTestBase
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public IncidentFilterAndSortIntegrationTests(CustomWebApplicationFactory factory) : base(factory)
    {
    }

    private async Task<Incident> CreateTestIncidentAsync(
        User reporter,
        string category = "Fire",
        string emergencyCode = "F-01",
        IncidentStatus status = IncidentStatus.Open,
        DateTime? createdAt = null)
    {
        return await ExecuteDbContextAsync(async context =>
        {
            var incident = new Incident
            {
                Id = Guid.NewGuid(),
                ReporterId = reporter.Id,
                Category = category,
                EmergencyCode = emergencyCode,
                Description = "Refinery unit incident description",
                Status = status,
                Latitude = 38.7915m,
                Longitude = 26.9212m,
                Location = new Point(26.9212, 38.7915) { SRID = 4326 },
                CreatedAt = createdAt ?? DateTime.UtcNow
            };
            context.Incidents.Add(incident);
            await context.SaveChangesAsync();
            return incident;
        });
    }

    [Fact]
    public async Task GetIncidents_WithFiltersAndDynamicSorting_ReturnsFilteredAndSortedResults()
    {
        // Arrange
        var reporter = await SeedUserAsync("Tural", "Mammadov", role: RoleType.Employee);
        var now = DateTime.UtcNow;

        var inc1 = await CreateTestIncidentAsync(reporter, category: "Fire", emergencyCode: "F-OLD", status: IncidentStatus.Open, createdAt: now.AddMinutes(-20));
        var inc2 = await CreateTestIncidentAsync(reporter, category: "Fire", emergencyCode: "F-NEW", status: IncidentStatus.Open, createdAt: now.AddMinutes(-5));
        await CreateTestIncidentAsync(reporter, category: "Gas", emergencyCode: "G-01", status: IncidentStatus.Open, createdAt: now.AddMinutes(-1));

        var client = Factory.CreateAuthenticatedClient(RoleType.Operator);

        // Act
        var response = await client.GetAsync("/api/v1/incidents?status=Open&category=Fire&page=1&pageSize=20&sortBy=createdAt&sortDir=desc");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<PagedResult<IncidentDto>>>(JsonOptions);
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();

        var page = result.Data;
        page.Should().NotBeNull();
        page!.Items.Should().HaveCount(2);
        page.Items.Should().OnlyContain(i => i.Category == "Fire" && i.Status == "Open");
        page.Items[0].Id.Should().Be(inc2.Id);
        page.Items[1].Id.Should().Be(inc1.Id);
    }

    [Fact]
    public async Task GetRecentIncidents_WithLimitParameter_ReturnsBoundedRecentActiveIncidents()
    {
        // Arrange
        var reporter = await SeedUserAsync("Rauf", "Huseynov", role: RoleType.Employee);
        var now = DateTime.UtcNow;

        for (int i = 1; i <= 5; i++)
        {
            await CreateTestIncidentAsync(reporter, category: "Fire", emergencyCode: $"F-0{i}", status: IncidentStatus.Open, createdAt: now.AddMinutes(i));
        }

        // Add a resolved incident (should not be returned)
        await CreateTestIncidentAsync(reporter, category: "Gas", emergencyCode: "RESOLVED-01", status: IncidentStatus.Resolved, createdAt: now.AddHours(1));

        var client = Factory.CreateAuthenticatedClient(RoleType.Operator);

        // Act
        var response = await client.GetAsync("/api/v1/incidents/recent?limit=3");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<List<IncidentDto>>>(JsonOptions);
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();

        var items = result.Data;
        items.Should().NotBeNull();
        items!.Should().HaveCount(3);
        items.Should().OnlyContain(i => i.Status == "Open" || i.Status == "Assigned");
        items.Should().NotContain(i => i.EmergencyCode == "RESOLVED-01");
        items[0].CreatedAt.Should().BeOnOrAfter(items[1].CreatedAt);
    }

    [Fact]
    public async Task GetRecentIncidents_WithDefaultAndMaxLimit_ConstrainsCorrectly()
    {
        // Arrange
        var client = Factory.CreateAuthenticatedClient(RoleType.Operator);

        // Act - Default limit
        var defaultResponse = await client.GetAsync("/api/v1/incidents/recent");
        defaultResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Act - High limit (should be clamped to max 50)
        var clampedResponse = await client.GetAsync("/api/v1/incidents/recent?limit=100");
        clampedResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var clampedResult = await clampedResponse.Content.ReadFromJsonAsync<ApiResponse<List<IncidentDto>>>(JsonOptions);
        clampedResult.Should().NotBeNull();
        clampedResult!.Data.Should().NotBeNull();
        clampedResult.Data!.Count.Should().BeLessThanOrEqualTo(50);
    }
}
