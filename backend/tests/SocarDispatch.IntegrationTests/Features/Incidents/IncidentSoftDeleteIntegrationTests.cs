using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using SocarDispatch.Application.Common.Models;
using SocarDispatch.Application.Features.Incidents.DTOs;
using SocarDispatch.Domain.Entities;
using SocarDispatch.Domain.Enums;
using SocarDispatch.IntegrationTests.Common;
using SocarDispatch.IntegrationTests.Fixtures;
using Xunit;

namespace SocarDispatch.IntegrationTests.Features.Incidents;

public class IncidentSoftDeleteIntegrationTests : IntegrationTestBase
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public IncidentSoftDeleteIntegrationTests(CustomWebApplicationFactory factory) : base(factory)
    {
    }

    private async Task<Incident> CreateTestIncidentAsync(User reporter)
    {
        return await ExecuteDbContextAsync(async context =>
        {
            var incident = new Incident
            {
                Id = Guid.NewGuid(),
                ReporterId = reporter.Id,
                Category = "Yangın",
                EmergencyCode = "Kırmızı Kod",
                Description = "Ham petrol ünitesinde alevlenme tespit edildi.",
                Status = IncidentStatus.Open,
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
    public async Task Delete_ExistingIncident_ReturnsOkAndExcludesFromGet()
    {
        // Arrange
        var reporter = await SeedUserAsync("Kemal", "Yilmaz", role: RoleType.Employee);
        var incident = await CreateTestIncidentAsync(reporter);

        var operatorClient = Factory.CreateAuthenticatedClient(RoleType.Operator);

        // Act 1: Soft-delete the incident via HTTP DELETE
        var deleteResponse = await operatorClient.DeleteAsync($"/api/v1/incidents/{incident.Id}");

        // Assert 1: Delete returned 200 OK
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var deleteResult = await deleteResponse.Content.ReadFromJsonAsync<ApiResponse<bool>>(JsonOptions);
        deleteResult.Should().NotBeNull();
        deleteResult!.Success.Should().BeTrue();

        // Assert 2: Verify in database that entity physically exists with IsDeleted = true
        await ExecuteDbContextAsync(async context =>
        {
            var rawIncident = await context.Incidents
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(i => i.Id == incident.Id);

            rawIncident.Should().NotBeNull();
            rawIncident!.IsDeleted.Should().BeTrue();
            rawIncident.DeletedAt.Should().NotBeNull();
        });

        // Act 2: Attempt to retrieve the deleted incident via GET by Id
        var getByIdResponse = await operatorClient.GetAsync($"/api/v1/incidents/{incident.Id}");

        // Assert 3: Query filter prevents retrieval -> 404 Not Found
        getByIdResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);

        // Act 3: Retrieve incidents list
        var listResponse = await operatorClient.GetAsync("/api/v1/incidents");

        // Assert 4: Soft-deleted incident is excluded from list
        listResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var listResult = await listResponse.Content.ReadFromJsonAsync<ApiResponse<PagedResult<IncidentDto>>>(JsonOptions);
        listResult.Should().NotBeNull();
        listResult!.Data.Items.Should().NotContain(i => i.Id == incident.Id);
    }
}
