using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using SocarDispatch.Application.Features.Incidents.Queries.GetRecentIncidents;
using SocarDispatch.Domain.Entities;
using SocarDispatch.Domain.Enums;
using SocarDispatch.Infrastructure.Persistence;
using Xunit;

namespace SocarDispatch.Application.Tests;

public class GetRecentIncidentsQueryTests
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
            FirstName = "Ali",
            LastName = "Hasanov",
            Email = "ali.hasanov@socar.az",
            Phone = "+994501234567",
            Department = "Safety Unit",
            RoleType = RoleType.Employee,
            PasswordHash = "hash"
        };
        context.Users.Add(user);
        await context.SaveChangesAsync();
        return user;
    }

    [Fact]
    public async Task Handle_ReturnsOnlyActiveAndUnresolvedIncidents()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var reporter = await SeedReporterAsync(context);

        context.Incidents.AddRange(
            new Incident { ReporterId = reporter.Id, Category = "Fire", EmergencyCode = "F-01", Status = IncidentStatus.Open, Location = new Point(49.8, 40.4) { SRID = 4326 } },
            new Incident { ReporterId = reporter.Id, Category = "Gas", EmergencyCode = "G-01", Status = IncidentStatus.Assigned, Location = new Point(49.8, 40.4) { SRID = 4326 } },
            new Incident { ReporterId = reporter.Id, Category = "Chemical", EmergencyCode = "C-01", Status = IncidentStatus.Resolved, Location = new Point(49.8, 40.4) { SRID = 4326 } },
            new Incident { ReporterId = reporter.Id, Category = "Medical", EmergencyCode = "M-01", Status = IncidentStatus.Canceled, Location = new Point(49.8, 40.4) { SRID = 4326 } }
        );
        await context.SaveChangesAsync();

        var handler = new GetRecentIncidentsQueryHandler(context);
        var query = new GetRecentIncidentsQuery(Limit: 10);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Data.Should().HaveCount(2);
        result.Data.Should().OnlyContain(i => i.Status == IncidentStatus.Open.ToString() || i.Status == IncidentStatus.Assigned.ToString());
    }

    [Fact]
    public async Task Handle_ReturnsOrderedByCreatedAtDescending()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var reporter = await SeedReporterAsync(context);
        var now = DateTime.UtcNow;

        context.Incidents.AddRange(
            new Incident { ReporterId = reporter.Id, Category = "Fire", EmergencyCode = "OLD", Status = IncidentStatus.Open, CreatedAt = now.AddMinutes(-30), Location = new Point(49.8, 40.4) { SRID = 4326 } },
            new Incident { ReporterId = reporter.Id, Category = "Fire", EmergencyCode = "NEWEST", Status = IncidentStatus.Open, CreatedAt = now, Location = new Point(49.8, 40.4) { SRID = 4326 } },
            new Incident { ReporterId = reporter.Id, Category = "Fire", EmergencyCode = "MID", Status = IncidentStatus.Open, CreatedAt = now.AddMinutes(-10), Location = new Point(49.8, 40.4) { SRID = 4326 } }
        );
        await context.SaveChangesAsync();

        var handler = new GetRecentIncidentsQueryHandler(context);
        var query = new GetRecentIncidentsQuery(Limit: 10);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Data.Should().HaveCount(3);
        result.Data[0].EmergencyCode.Should().Be("NEWEST");
        result.Data[1].EmergencyCode.Should().Be("MID");
        result.Data[2].EmergencyCode.Should().Be("OLD");
    }

    [Fact]
    public async Task Handle_WithLimit_CapsResultsCorrectly()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var reporter = await SeedReporterAsync(context);

        for (int i = 0; i < 5; i++)
        {
            context.Incidents.Add(new Incident
            {
                ReporterId = reporter.Id,
                Category = "Fire",
                EmergencyCode = $"CODE-{i}",
                Status = IncidentStatus.Open,
                Location = new Point(49.8, 40.4) { SRID = 4326 }
            });
        }
        await context.SaveChangesAsync();

        var handler = new GetRecentIncidentsQueryHandler(context);
        var query = new GetRecentIncidentsQuery(Limit: 2);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Data.Should().HaveCount(2);
    }

    [Fact]
    public void Validator_WithValidLimit_ShouldPass()
    {
        var validator = new GetRecentIncidentsQueryValidator();
        var query = new GetRecentIncidentsQuery(Limit: 10);

        var result = validator.Validate(query);

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    [InlineData(51)]
    [InlineData(100)]
    public void Validator_WithLimitOutOfRange_ShouldHaveValidationError(int limit)
    {
        var validator = new GetRecentIncidentsQueryValidator();
        var query = new GetRecentIncidentsQuery(Limit: limit);

        var result = validator.Validate(query);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(GetRecentIncidentsQuery.Limit));
    }
}
