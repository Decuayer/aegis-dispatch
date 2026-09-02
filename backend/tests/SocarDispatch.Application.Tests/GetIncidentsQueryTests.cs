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

    [Fact]
    public async Task Handle_WithReporterId_ReturnsOnlyIncidentsReportedBySpecifiedUser()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var reporter1 = await SeedReporterAsync(context);

        var reporter2 = new User
        {
            Id = Guid.NewGuid(),
            FirstName = "Leyla",
            LastName = "Hasanova",
            Email = "leyla@socar.az",
            Department = "HSE",
            RoleType = RoleType.Employee,
            PasswordHash = "hash"
        };
        context.Users.Add(reporter2);
        await context.SaveChangesAsync();

        context.Incidents.AddRange(
            new Incident
            {
                ReporterId = reporter1.Id,
                Category = "Fire",
                EmergencyCode = "RED-1",
                Status = IncidentStatus.Open,
                CreatedAt = DateTime.UtcNow,
                Location = new Point(49.8671, 40.4093) { SRID = 4326 }
            },
            new Incident
            {
                ReporterId = reporter2.Id,
                Category = "Medical",
                EmergencyCode = "YELLOW-1",
                Status = IncidentStatus.Open,
                CreatedAt = DateTime.UtcNow,
                Location = new Point(49.8671, 40.4093) { SRID = 4326 }
            }
        );
        await context.SaveChangesAsync();

        var handler = new GetIncidentsQueryHandler(context);
        var query = new GetIncidentsQuery { ReporterId = reporter1.Id };

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Items.Should().HaveCount(1);
        result.Data.Items.First().ReporterId.Should().Be(reporter1.Id);
    }

    [Fact]
    public async Task Handle_WithSoftDeletedIncidents_ExcludesThemFromResults()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var reporter = await SeedReporterAsync(context);

        var activeIncident = new Incident
        {
            ReporterId = reporter.Id,
            Category = "Fire",
            EmergencyCode = "RED-1",
            Status = IncidentStatus.Open,
            Location = new Point(49.8671, 40.4093) { SRID = 4326 },
            IsDeleted = false
        };
        var deletedIncident = new Incident
        {
            ReporterId = reporter.Id,
            Category = "Gas",
            EmergencyCode = "YELLOW-1",
            Status = IncidentStatus.Open,
            Location = new Point(49.8671, 40.4093) { SRID = 4326 },
            IsDeleted = true,
            DeletedAt = DateTime.UtcNow
        };
        context.Incidents.AddRange(activeIncident, deletedIncident);
        await context.SaveChangesAsync();

        var handler = new GetIncidentsQueryHandler(context);
        var query = new GetIncidentsQuery();

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.TotalCount.Should().Be(1);
        result.Data.Items.Should().ContainSingle();
        result.Data.Items.First().Id.Should().Be(activeIncident.Id);
    }

    [Fact]
    public void Handle_DefaultPagination_DefaultsToPage1AndSize20()
    {
        // Act
        var query = new GetIncidentsQuery();

        // Assert
        query.PageNumber.Should().Be(1);
        query.Page.Should().Be(1);
        query.PageSize.Should().Be(20);
    }

    [Fact]
    public async Task Handle_PaginationEdgeCases_ConstrainsGracefully()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var reporter = await SeedReporterAsync(context);

        for (int i = 0; i < 25; i++)
        {
            context.Incidents.Add(new Incident
            {
                ReporterId = reporter.Id,
                Category = "Fire",
                EmergencyCode = $"CODE-{i}",
                Status = IncidentStatus.Open,
                Location = new Point(49.8671, 40.4093) { SRID = 4326 }
            });
        }
        await context.SaveChangesAsync();

        var handler = new GetIncidentsQueryHandler(context);

        // Negative page number should be constrained to 1
        var negativePageQuery = new GetIncidentsQuery { PageNumber = -5, PageSize = 10 };
        var negativeResult = await handler.Handle(negativePageQuery, CancellationToken.None);
        negativeResult.Data.PageNumber.Should().Be(1);
        negativeResult.Data.Items.Should().HaveCount(10);

        // PageSize over 100 should be constrained to 100
        var largePageSizeQuery = new GetIncidentsQuery { PageSize = 500 };
        var largeResult = await handler.Handle(largePageSizeQuery, CancellationToken.None);
        largeResult.Data.PageSize.Should().Be(100);
        largeResult.Data.Items.Should().HaveCount(25);
    }

    [Fact]
    public async Task Handle_WithSortByCategoryAsc_ReturnsAlphabeticallyOrdered()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var reporter = await SeedReporterAsync(context);

        context.Incidents.AddRange(
            new Incident { ReporterId = reporter.Id, Category = "Medical", EmergencyCode = "M-01", Location = new Point(49.8, 40.4) { SRID = 4326 } },
            new Incident { ReporterId = reporter.Id, Category = "Fire", EmergencyCode = "F-01", Location = new Point(49.8, 40.4) { SRID = 4326 } },
            new Incident { ReporterId = reporter.Id, Category = "Chemical", EmergencyCode = "C-01", Location = new Point(49.8, 40.4) { SRID = 4326 } }
        );
        await context.SaveChangesAsync();

        var handler = new GetIncidentsQueryHandler(context);
        var query = new GetIncidentsQuery { SortBy = "category", SortDir = "asc" };

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Data.Items.Should().HaveCount(3);
        result.Data.Items.Select(i => i.Category).Should().ContainInConsecutiveOrder("Chemical", "Fire", "Medical");
    }

    [Fact]
    public async Task Handle_WithSortByStatusDesc_ReturnsOrderedByStatus()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var reporter = await SeedReporterAsync(context);

        context.Incidents.AddRange(
            new Incident { ReporterId = reporter.Id, Category = "Fire", EmergencyCode = "F-01", Status = IncidentStatus.Open, Location = new Point(49.8, 40.4) { SRID = 4326 } },
            new Incident { ReporterId = reporter.Id, Category = "Fire", EmergencyCode = "F-02", Status = IncidentStatus.Assigned, Location = new Point(49.8, 40.4) { SRID = 4326 } },
            new Incident { ReporterId = reporter.Id, Category = "Fire", EmergencyCode = "F-03", Status = IncidentStatus.Resolved, Location = new Point(49.8, 40.4) { SRID = 4326 } }
        );
        await context.SaveChangesAsync();

        var handler = new GetIncidentsQueryHandler(context);
        var query = new GetIncidentsQuery { SortBy = "status", SortDir = "desc" };

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Data.Items.Should().HaveCount(3);
        result.Data.Items[0].Status.Should().Be(IncidentStatus.Resolved.ToString());
    }

    [Fact]
    public async Task Handle_WithDateFromAndDateTo_FiltersDateRangeAccurately()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var reporter = await SeedReporterAsync(context);
        var baseDate = DateTime.UtcNow;

        context.Incidents.AddRange(
            new Incident { ReporterId = reporter.Id, Category = "Fire", EmergencyCode = "F-01", CreatedAt = baseDate.AddDays(-5), Location = new Point(49.8, 40.4) { SRID = 4326 } },
            new Incident { ReporterId = reporter.Id, Category = "Fire", EmergencyCode = "F-02", CreatedAt = baseDate.AddDays(-2), Location = new Point(49.8, 40.4) { SRID = 4326 } },
            new Incident { ReporterId = reporter.Id, Category = "Fire", EmergencyCode = "F-03", CreatedAt = baseDate.AddDays(-1), Location = new Point(49.8, 40.4) { SRID = 4326 } },
            new Incident { ReporterId = reporter.Id, Category = "Fire", EmergencyCode = "F-04", CreatedAt = baseDate.AddHours(2), Location = new Point(49.8, 40.4) { SRID = 4326 } }
        );
        await context.SaveChangesAsync();

        var handler = new GetIncidentsQueryHandler(context);
        var query = new GetIncidentsQuery
        {
            DateFrom = baseDate.AddDays(-3),
            DateTo = baseDate
        };

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Data.Items.Should().HaveCount(2);
        result.Data.Items.Select(i => i.EmergencyCode).Should().Contain(["F-02", "F-03"]);
    }

    [Fact]
    public void Validator_WithInvalidSortBy_ShouldHaveValidationError()
    {
        var validator = new GetIncidentsQueryValidator();
        var query = new GetIncidentsQuery { SortBy = "invalidField" };

        var result = validator.Validate(query);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(GetIncidentsQuery.SortBy));
    }

    [Fact]
    public void Validator_WithInvalidSortDir_ShouldHaveValidationError()
    {
        var validator = new GetIncidentsQueryValidator();
        var query = new GetIncidentsQuery { SortDir = "sideways" };

        var result = validator.Validate(query);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(GetIncidentsQuery.SortDir));
    }


}
