using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using SocarDispatch.Application.Features.Incidents.Queries.GetIncidents;
using SocarDispatch.Domain.Entities;
using SocarDispatch.Domain.Enums;
using SocarDispatch.Infrastructure.Persistence;
using Xunit;

namespace SocarDispatch.Application.Tests;

public class GetIncidentsQueryTests
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

    private static async Task<User> SeedReporterAsync(ApplicationDbContext context)
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            FirstName = "Orhan",
            LastName = "Aliyev",
            Email = "orhan.aliyev@socar.az",
            Phone = "+994501112233",
            Department = "Fire Safety",
            RoleType = RoleType.Employee,
            PasswordHash = "hash"
        };
        context.Users.Add(user);
        await context.SaveChangesAsync();
        return user;
    }

    [Fact]
    public async Task Handle_WithDefaultQuery_ReturnsPagedResultOrderedByCreatedAtDesc()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var reporter = await SeedReporterAsync(context);

        var now = DateTime.UtcNow;
        var inc1 = new Incident
        {
            ReporterId = reporter.Id,
            Category = "Fire",
            EmergencyCode = "FIRE-01",
            Status = IncidentStatus.Open,
            CreatedAt = now.AddMinutes(-20),
            Location = new Point(49.8671, 40.4093) { SRID = 4326 }
        };
        var inc2 = new Incident
        {
            ReporterId = reporter.Id,
            Category = "GasLeak",
            EmergencyCode = "GAS-02",
            Status = IncidentStatus.Open,
            CreatedAt = now.AddMinutes(-5),
            Location = new Point(49.8671, 40.4093) { SRID = 4326 }
        };
        context.Incidents.AddRange(inc1, inc2);
        await context.SaveChangesAsync();

        var handler = new GetIncidentsQueryHandler(context);
        var query = new GetIncidentsQuery();

        // Act
        var response = await handler.Handle(query, CancellationToken.None);

        // Assert
        response.Success.Should().BeTrue();
        response.Data.Should().NotBeNull();
        response.Data!.TotalCount.Should().Be(2);
        response.Data.Items.Should().HaveCount(2);
        response.Data.Items.First().Category.Should().Be("GasLeak");
        response.Data.Items.Last().Category.Should().Be("Fire");
    }

    [Fact]
    public async Task Handle_WithStatusFilter_FiltersCorrectly()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var reporter = await SeedReporterAsync(context);

        context.Incidents.AddRange(
            new Incident
            {
                ReporterId = reporter.Id,
                Category = "Fire",
                EmergencyCode = "F-1",
                Status = IncidentStatus.Open,
                Location = new Point(49.8671, 40.4093) { SRID = 4326 }
            },
            new Incident
            {
                ReporterId = reporter.Id,
                Category = "Injury",
                EmergencyCode = "MED-1",
                Status = IncidentStatus.Resolved,
                Location = new Point(49.8671, 40.4093) { SRID = 4326 }
            }
        );
        await context.SaveChangesAsync();

        var handler = new GetIncidentsQueryHandler(context);
        var query = new GetIncidentsQuery { Status = "Resolved" };

        // Act
        var response = await handler.Handle(query, CancellationToken.None);

        // Assert
        response.Data!.Items.Should().HaveCount(1);
        response.Data.Items.First().Status.Should().Be("Resolved");
    }

    [Fact]
    public async Task Handle_WithCategoryFilter_FiltersCorrectly()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var reporter = await SeedReporterAsync(context);

        context.Incidents.AddRange(
            new Incident
            {
                ReporterId = reporter.Id,
                Category = "HazardousChemical",
                EmergencyCode = "CHEM-1",
                Status = IncidentStatus.Open,
                Location = new Point(49.8671, 40.4093) { SRID = 4326 }
            },
            new Incident
            {
                ReporterId = reporter.Id,
                Category = "Electrical",
                EmergencyCode = "ELEC-1",
                Status = IncidentStatus.Open,
                Location = new Point(49.8671, 40.4093) { SRID = 4326 }
            }
        );
        await context.SaveChangesAsync();

        var handler = new GetIncidentsQueryHandler(context);
        var query = new GetIncidentsQuery { Category = "hazardouschemical" };

        // Act
        var response = await handler.Handle(query, CancellationToken.None);

        // Assert
        response.Data!.Items.Should().HaveCount(1);
        response.Data.Items.First().Category.Should().Be("HazardousChemical");
    }

    [Fact]
    public async Task Handle_WithEmergencyCodeFilter_FiltersCorrectly()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var reporter = await SeedReporterAsync(context);

        context.Incidents.AddRange(
            new Incident
            {
                ReporterId = reporter.Id,
                Category = "Fire",
                EmergencyCode = "ALPHA-1",
                Status = IncidentStatus.Open,
                Location = new Point(49.8671, 40.4093) { SRID = 4326 }
            },
            new Incident
            {
                ReporterId = reporter.Id,
                Category = "Fire",
                EmergencyCode = "BETA-2",
                Status = IncidentStatus.Open,
                Location = new Point(49.8671, 40.4093) { SRID = 4326 }
            }
        );
        await context.SaveChangesAsync();

        var handler = new GetIncidentsQueryHandler(context);
        var query = new GetIncidentsQuery { EmergencyCode = "beta-2" };

        // Act
        var response = await handler.Handle(query, CancellationToken.None);

        // Assert
        response.Data!.Items.Should().HaveCount(1);
        response.Data.Items.First().EmergencyCode.Should().Be("BETA-2");
    }

    [Fact]
    public async Task Handle_WithIncidentId_ReturnsExactIncident()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var reporter = await SeedReporterAsync(context);

        var targetId = Guid.NewGuid();
        context.Incidents.AddRange(
            new Incident
            {
                Id = targetId,
                ReporterId = reporter.Id,
                Category = "Structural",
                EmergencyCode = "STR-1",
                Status = IncidentStatus.Open,
                Location = new Point(49.8671, 40.4093) { SRID = 4326 }
            },
            new Incident
            {
                Id = Guid.NewGuid(),
                ReporterId = reporter.Id,
                Category = "Structural",
                EmergencyCode = "STR-2",
                Status = IncidentStatus.Open,
                Location = new Point(49.8671, 40.4093) { SRID = 4326 }
            }
        );
        await context.SaveChangesAsync();

        var handler = new GetIncidentsQueryHandler(context);
        var query = new GetIncidentsQuery { IncidentId = targetId };

        // Act
        var response = await handler.Handle(query, CancellationToken.None);

        // Assert
        response.Data!.Items.Should().HaveCount(1);
        response.Data.Items.First().Id.Should().Be(targetId);
    }

    [Fact]
    public async Task Handle_WithSearchTerm_MatchingGuidSnippet_ReturnsMatchedIncident()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var reporter = await SeedReporterAsync(context);

        var knownId = Guid.Parse("11111111-2222-3333-4444-555555555555");
        context.Incidents.AddRange(
            new Incident
            {
                Id = knownId,
                ReporterId = reporter.Id,
                Category = "Leak",
                EmergencyCode = "L-1",
                Status = IncidentStatus.Open,
                Location = new Point(49.8671, 40.4093) { SRID = 4326 }
            },
            new Incident
            {
                Id = Guid.Parse("99999999-8888-7777-6666-555555555555"),
                ReporterId = reporter.Id,
                Category = "Security",
                EmergencyCode = "S-1",
                Status = IncidentStatus.Open,
                Location = new Point(49.8671, 40.4093) { SRID = 4326 }
            }
        );
        await context.SaveChangesAsync();

        var handler = new GetIncidentsQueryHandler(context);
        var query = new GetIncidentsQuery { Search = "11111111-2222" };

        // Act
        var response = await handler.Handle(query, CancellationToken.None);

        // Assert
        response.Data!.Items.Should().HaveCount(1);
        response.Data.Items.First().Id.Should().Be(knownId);
    }

    [Fact]
    public async Task Handle_WithSearchTerm_PrefixedWithIncTag_ReturnsMatchedIncident()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var reporter = await SeedReporterAsync(context);

        var knownId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        context.Incidents.Add(new Incident
        {
            Id = knownId,
            ReporterId = reporter.Id,
            Category = "Security",
            EmergencyCode = "SEC-01",
            Status = IncidentStatus.Open,
            Location = new Point(49.8671, 40.4093) { SRID = 4326 }
        });
        await context.SaveChangesAsync();

        var handler = new GetIncidentsQueryHandler(context);
        var query = new GetIncidentsQuery { SearchTerm = "#INC-aaaaaaaa" };

        // Act
        var response = await handler.Handle(query, CancellationToken.None);

        // Assert
        response.Data!.Items.Should().HaveCount(1);
        response.Data.Items.First().Id.Should().Be(knownId);
    }


    [Fact]
    public async Task Handle_WithSearchTerm_MatchingDescriptionOrCode_ReturnsMatches()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var reporter = await SeedReporterAsync(context);

        context.Incidents.AddRange(
            new Incident
            {
                ReporterId = reporter.Id,
                Category = "Other",
                EmergencyCode = "CODE-RED",
                Description = "Major pipeline rupture in Sector 4",
                Status = IncidentStatus.Open,
                Location = new Point(49.8671, 40.4093) { SRID = 4326 }
            },
            new Incident
            {
                ReporterId = reporter.Id,
                Category = "Routine",
                EmergencyCode = "ROUT-1",
                Description = "Minor inspection routine",
                Status = IncidentStatus.Open,
                Location = new Point(49.8671, 40.4093) { SRID = 4326 }
            }
        );
        await context.SaveChangesAsync();

        var handler = new GetIncidentsQueryHandler(context);
        var query = new GetIncidentsQuery { SearchTerm = "pipeline" };

        // Act
        var response = await handler.Handle(query, CancellationToken.None);

        // Assert
        response.Data!.Items.Should().HaveCount(1);
        response.Data.Items.First().EmergencyCode.Should().Be("CODE-RED");
    }

    [Fact]
    public async Task Handle_WithPagination_SlicesPagesAndSetsMetadataCorrectly()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var reporter = await SeedReporterAsync(context);

        var now = DateTime.UtcNow;
        for (int i = 1; i <= 30; i++)
        {
            context.Incidents.Add(new Incident
            {
                ReporterId = reporter.Id,
                Category = "Fire",
                EmergencyCode = $"F-{i:D2}",
                Status = IncidentStatus.Open,
                CreatedAt = now.AddMinutes(i),
                Location = new Point(49.8671, 40.4093) { SRID = 4326 }
            });
        }
        await context.SaveChangesAsync();

        var handler = new GetIncidentsQueryHandler(context);

        // Act - Page 1 with size 10
        var page1 = await handler.Handle(new GetIncidentsQuery { PageNumber = 1, PageSize = 10 }, CancellationToken.None);

        // Assert Page 1
        page1.Data!.TotalCount.Should().Be(30);
        page1.Data.TotalPages.Should().Be(3);
        page1.Data.Items.Should().HaveCount(10);
        page1.Data.HasPreviousPage.Should().BeFalse();
        page1.Data.HasNextPage.Should().BeTrue();
        page1.Data.Items.First().EmergencyCode.Should().Be("F-30");

        // Act - Page 3 with size 10
        var page3 = await handler.Handle(new GetIncidentsQuery { PageNumber = 3, PageSize = 10 }, CancellationToken.None);

        // Assert Page 3
        page3.Data!.Items.Should().HaveCount(10);
        page3.Data.HasPreviousPage.Should().BeTrue();
        page3.Data.HasNextPage.Should().BeFalse();
        page3.Data.Items.Last().EmergencyCode.Should().Be("F-01");
    }

    [Fact]
    public void Validator_WithValidQuery_ShouldPass()
    {
        var validator = new GetIncidentsQueryValidator();
        var query = new GetIncidentsQuery
        {
            PageNumber = 1,
            PageSize = 25,
            SearchTerm = "Fire",
            Category = "Fire",
            EmergencyCode = "F-01",
            FromDate = DateTime.UtcNow.AddDays(-1),
            ToDate = DateTime.UtcNow
        };

        var result = validator.Validate(query);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validator_WhenToDateEarlierThanFromDate_ShouldHaveValidationError()
    {
        var validator = new GetIncidentsQueryValidator();
        var query = new GetIncidentsQuery
        {
            FromDate = DateTime.UtcNow,
            ToDate = DateTime.UtcNow.AddDays(-1)
        };

        var result = validator.Validate(query);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(GetIncidentsQuery.ToDate));
    }
}
