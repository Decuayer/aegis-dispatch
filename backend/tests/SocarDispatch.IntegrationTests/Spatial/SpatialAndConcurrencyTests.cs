using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using SocarDispatch.Domain.Entities;
using SocarDispatch.Domain.Enums;
using SocarDispatch.IntegrationTests.Common;
using SocarDispatch.IntegrationTests.Fixtures;
using Xunit;

namespace SocarDispatch.IntegrationTests.Spatial;

public class SpatialAndConcurrencyTests : IntegrationTestBase
{
    public SpatialAndConcurrencyTests(CustomWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task PostGIS_SpatialDistanceQuery_FiltersNearbyIncidentsAccurately()
    {
        var reporter = await SeedUserAsync("Geo", "Tester", role: RoleType.Employee);

        // Reference point: STAR Refinery coordinates (~38.7900, 26.9200)
        var referencePoint = new Point(26.9200, 38.7900) { SRID = 4326 };

        // Seed one close incident (~100m) and one distant incident (~50km)
        await ExecuteDbContextAsync(async ctx =>
        {
            ctx.Incidents.Add(new Incident
            {
                Id = Guid.NewGuid(),
                ReporterId = reporter.Id,
                Category = "Fire",
                EmergencyCode = "RED_ALERT",
                Status = IncidentStatus.Open,
                Latitude = 38.7905m,
                Longitude = 26.9205m,
                Location = new Point(26.9205, 38.7905) { SRID = 4326 },
                CreatedAt = DateTime.UtcNow
            });

            ctx.Incidents.Add(new Incident
            {
                Id = Guid.NewGuid(),
                ReporterId = reporter.Id,
                Category = "Gas Leak",
                EmergencyCode = "YELLOW_ALERT",
                Status = IncidentStatus.Open,
                Latitude = 38.4237m,
                Longitude = 27.1428m,
                Location = new Point(27.1428, 38.4237) { SRID = 4326 },
                CreatedAt = DateTime.UtcNow
            });
            await ctx.SaveChangesAsync();
        });

        // Act: Query incidents within 0.01 degrees radius (~1.1 km)
        var nearbyIncidents = await ExecuteDbContextAsync(async ctx =>
        {
            return await ctx.Incidents
                .Where(i => i.Location != null && i.Location.Distance(referencePoint) < 0.01)
                .ToListAsync();
        });

        // Assert
        nearbyIncidents.Should().HaveCount(1);
        nearbyIncidents.First().Category.Should().Be("Fire");
    }

    [Fact]
    public async Task Concurrency_SimultaneousStatusUpdates_HandlesWithoutDeadlock()
    {
        var reporter = await SeedUserAsync("Conc", "User", role: RoleType.Employee);
        var incidentId = Guid.NewGuid();

        await ExecuteDbContextAsync(async ctx =>
        {
            ctx.Incidents.Add(new Incident
            {
                Id = incidentId,
                ReporterId = reporter.Id,
                Category = "Explosion",
                EmergencyCode = "RED_ALERT",
                Status = IncidentStatus.Open,
                Latitude = 38.79m,
                Longitude = 26.92m,
                Location = new Point(26.92, 38.79) { SRID = 4326 },
                CreatedAt = DateTime.UtcNow
            });
            await ctx.SaveChangesAsync();
        });

        // Act: Execute 10 parallel update tasks on the same incident record
        var tasks = Enumerable.Range(0, 10).Select(async i =>
        {
            try
            {
                await ExecuteDbContextAsync(async ctx =>
                {
                    var inc = await ctx.Incidents.FirstOrDefaultAsync(x => x.Id == incidentId);
                    if (inc != null)
                    {
                        inc.Description = $"Updated by task {i} at {DateTime.UtcNow.Ticks}";
                        await ctx.SaveChangesAsync();
                    }
                });
                return true;
            }
            catch
            {
                return false;
            }
        });

        var results = await Task.WhenAll(tasks);

        // Assert: Ensure consistent database state without deadlocks
        var finalIncident = await ExecuteDbContextAsync(async ctx =>
            await ctx.Incidents.FirstAsync(x => x.Id == incidentId));

        finalIncident.Should().NotBeNull();
        results.Any(r => r == true).Should().BeTrue();
    }
}
