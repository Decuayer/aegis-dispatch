using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using SocarDispatch.Application.Common.Models;
using SocarDispatch.Application.Features.Incidents.DTOs;
using SocarDispatch.Domain.Entities;
using SocarDispatch.Domain.Enums;
using SocarDispatch.IntegrationTests.Common;
using SocarDispatch.IntegrationTests.Fixtures;
using NetTopologySuite.Geometries;


namespace SocarDispatch.IntegrationTests.Features.Incidents;

public class GetIncidentsPaginationTests : IntegrationTestBase
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public GetIncidentsPaginationTests(CustomWebApplicationFactory factory) : base(factory)
    {
    }

    private async Task<Incident> CreateTestIncidentAsync(
        User reporter,
        string category = "Yangın",
        string emergencyCode = "Kırmızı Kod",
        string description = "Ham petrol ünitesinde alevlenme tespit edildi.",
        IncidentStatus status = IncidentStatus.Open,
        Guid? customId = null)
    {
        return await ExecuteDbContextAsync(async context =>
        {
            var incident = new Incident
            {
                Id = customId ?? Guid.NewGuid(),
                ReporterId = reporter.Id,
                Category = category,
                EmergencyCode = emergencyCode,
                Description = description,
                Status = status,
                Latitude = 38.7915m,
                Longitude = 26.9212m,
                Location = new Point(26.9212, 38.7915) { SRID = 4326 },
                CreatedAt = DateTime.UtcNow
            };
            context.Incidents.Add(incident);
            await context.SaveChangesAsync();
            return incident;
        });
    }


    [Fact]
    public async Task GetIncidents_WithExactGuid_ReturnsExclusivelyMatchingRecord()
    {
        // Arrange
        var reporter = await SeedUserAsync("Kemal", "Yilmaz", role: RoleType.Employee);
        var targetIncident = await CreateTestIncidentAsync(reporter, category: "Gaz Sızıntısı");
        await CreateTestIncidentAsync(reporter, category: "Yangın");
        await CreateTestIncidentAsync(reporter, category: "Yaralanma");

        var client = Factory.CreateAuthenticatedClient(RoleType.Operator);

        // Act
        var response = await client.GetAsync($"/api/v1/incidents?incidentId={targetIncident.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<PagedResult<IncidentDto>>>(JsonOptions);
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();

        var page = result.Data;
        page.Should().NotBeNull();
        page.TotalCount.Should().Be(1);
        page.Items.Should().ContainSingle();
        page.Items[0].Id.Should().Be(targetIncident.Id);
        page.Items[0].Category.Should().Be("Gaz Sızıntısı");
    }

    [Theory]
    [InlineData("a1b2c3d4")]
    [InlineData("#INC-a1b2c3d4")]
    [InlineData("INC-a1b2c3d4")]
    public async Task GetIncidents_WithIdSnippetOrPrefix_ReturnsMatchingRecord(string searchTerm)
    {
        // Arrange
        var reporter = await SeedUserAsync("Fatma", "Kaya", role: RoleType.Employee);
        var customId = Guid.Parse("a1b2c3d4-0000-0000-0000-000000000001");
        var targetIncident = await CreateTestIncidentAsync(reporter, customId: customId);
        await CreateTestIncidentAsync(reporter, customId: Guid.Parse("f9e8d7c6-0000-0000-0000-000000000002"));

        var client = Factory.CreateAuthenticatedClient(RoleType.Operator);

        // Act
        var encodedSearch = Uri.EscapeDataString(searchTerm);
        var response = await client.GetAsync($"/api/v1/incidents?searchTerm={encodedSearch}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<PagedResult<IncidentDto>>>(JsonOptions);
        result.Should().NotBeNull();

        var page = result!.Data;
        page.TotalCount.Should().Be(1);
        page.Items.Should().ContainSingle();
        page.Items[0].Id.Should().Be(targetIncident.Id);
    }

    [Fact]
    public async Task GetIncidents_WithStatusAndCategoryFilters_FiltersAccurately()
    {
        // Arrange
        var reporter = await SeedUserAsync("Murat", "Ozturk", role: RoleType.Employee);
        await CreateTestIncidentAsync(reporter, category: "Yangın", emergencyCode: "Kırmızı Kod", status: IncidentStatus.Open);
        await CreateTestIncidentAsync(reporter, category: "Yangın", emergencyCode: "Sarı Kod", status: IncidentStatus.Assigned);
        await CreateTestIncidentAsync(reporter, category: "Kimyasal", emergencyCode: "Kırmızı Kod", status: IncidentStatus.Resolved);

        var client = Factory.CreateAuthenticatedClient(RoleType.Operator);

        // Act - Active status filter (Open + Assigned)
        var response = await client.GetAsync("/api/v1/incidents?status=Active&category=Yangın");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<PagedResult<IncidentDto>>>(JsonOptions);
        result.Should().NotBeNull();

        var page = result!.Data;
        page.TotalCount.Should().Be(2);
        page.Items.Should().HaveCount(2);
        page.Items.Should().OnlyContain(i => i.Category == "Yangın");
        page.Items.Should().OnlyContain(i => i.Status == "Open" || i.Status == "Assigned");
    }

    [Fact]
    public async Task GetIncidents_WithPaginationSlicing_ReturnsBoundedItemsAndMetadata()
    {
        // Arrange
        var reporter = await SeedUserAsync("Burak", "Cetin", role: RoleType.Employee);
        for (var i = 1; i <= 6; i++)
        {
            await CreateTestIncidentAsync(reporter, description: $"Incident report description #{i}");
        }

        var client = Factory.CreateAuthenticatedClient(RoleType.Operator);

        // Act - Request Page 1 of size 2
        var responsePage1 = await client.GetAsync("/api/v1/incidents?pageNumber=1&pageSize=2");
        var resultPage1 = await responsePage1.Content.ReadFromJsonAsync<ApiResponse<PagedResult<IncidentDto>>>(JsonOptions);

        // Act - Request Page 4 of size 2 (Overflow)
        var responsePage4 = await client.GetAsync("/api/v1/incidents?pageNumber=4&pageSize=2");
        var resultPage4 = await responsePage4.Content.ReadFromJsonAsync<ApiResponse<PagedResult<IncidentDto>>>(JsonOptions);

        // Assert
        responsePage1.StatusCode.Should().Be(HttpStatusCode.OK);
        resultPage1!.Data.TotalCount.Should().Be(6);
        resultPage1.Data.TotalPages.Should().Be(3);
        resultPage1.Data.PageNumber.Should().Be(1);
        resultPage1.Data.PageSize.Should().Be(2);
        resultPage1.Data.HasNextPage.Should().BeTrue();
        resultPage1.Data.HasPreviousPage.Should().BeFalse();
        resultPage1.Data.Items.Should().HaveCount(2);

        responsePage4.StatusCode.Should().Be(HttpStatusCode.OK);
        resultPage4!.Data.TotalCount.Should().Be(6);
        resultPage4.Data.TotalPages.Should().Be(3);
        resultPage4.Data.PageNumber.Should().Be(4);
        resultPage4.Data.HasNextPage.Should().BeFalse();
        resultPage4.Data.HasPreviousPage.Should().BeTrue();
        resultPage4.Data.Items.Should().BeEmpty();
    }
}
