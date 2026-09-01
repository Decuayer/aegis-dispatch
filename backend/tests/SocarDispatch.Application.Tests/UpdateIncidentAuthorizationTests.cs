using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SocarDispatch.Application.Features.Incidents.Commands.UpdateIncident;
using SocarDispatch.Application.Features.Incidents.DTOs;
using SocarDispatch.Domain.Entities;
using SocarDispatch.Domain.Enums;
using SocarDispatch.Domain.Exceptions;
using SocarDispatch.Infrastructure.Persistence;
using Xunit;

namespace SocarDispatch.Application.Tests;

public class UpdateIncidentAuthorizationTests
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
    public async Task UpdateIncident_WhenRequesterIsReporter_ShouldUpdateSuccessfully()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();

        var reporter = new User
        {
            FirstName = "Ahmet",
            LastName = "Yılmaz",
            Email = "ahmet@socar.com",
            Phone = "+905550001122",
            PasswordHash = "hash",
            Department = "Saha",
            RoleType = RoleType.Employee
        };
        context.Users.Add(reporter);
        await context.SaveChangesAsync();

        var incident = new Incident
        {
            ReporterId = reporter.Id,
            Category = "Fire",
            EmergencyCode = "Kırmızı Kod",
            Description = "Eski Açıklama",
            Latitude = 40.99m,
            Longitude = 29.02m,
            Location = new NetTopologySuite.Geometries.Point(29.02, 40.99) { SRID = 4326 },
            Status = IncidentStatus.Open
        };
        context.Incidents.Add(incident);
        await context.SaveChangesAsync();

        var handler = new UpdateIncidentCommandHandler(context);
        var command = new UpdateIncidentCommand(
            incident.Id,
            reporter.Id, // Requester = Reporter
            "Fire",
            "Sarı Kod",
            "Güncellenmiş Açıklama",
            new List<CreateIncidentMediaRequestDto>(),
            41.00m,
            29.10m
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Data.Category.Should().Be("Fire");
        result.Data.Description.Should().Be("Güncellenmiş Açıklama");

        var updated = await context.Incidents.FindAsync(incident.Id);
        updated!.Category.Should().Be("Fire");
    }

    [Fact]
    public async Task UpdateIncident_WhenRequesterIsOperator_ShouldUpdateSuccessfully()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();

        var reporter = new User
        {
            FirstName = "Ahmet",
            LastName = "Yılmaz",
            Email = "ahmet@socar.com",
            Phone = "+905550001122",
            PasswordHash = "hash",
            Department = "Saha",
            RoleType = RoleType.Employee
        };

        var operatorUser = new User
        {
            FirstName = "Mehmet",
            LastName = "Demir",
            Email = "mehmet@socar.com",
            Phone = "+905550003344",
            PasswordHash = "hash",
            Department = "Dispatch",
            RoleType = RoleType.Operator
        };

        context.Users.AddRange(reporter, operatorUser);
        await context.SaveChangesAsync();

        var incident = new Incident
        {
            ReporterId = reporter.Id,
            Category = "Fire",
            EmergencyCode = "Kırmızı Kod",
            Description = "Orijinal Açıklama",
            Latitude = 40.99m,
            Longitude = 29.02m,
            Location = new NetTopologySuite.Geometries.Point(29.02, 40.99) { SRID = 4326 },
            Status = IncidentStatus.Open
        };
        context.Incidents.Add(incident);
        await context.SaveChangesAsync();

        var handler = new UpdateIncidentCommandHandler(context);
        var command = new UpdateIncidentCommand(
            incident.Id,
            operatorUser.Id, // Requester = Operator (Reporter değil)
            "Security",
            "Siyah Kod",
            "Operator Güncelledi",
            new List<CreateIncidentMediaRequestDto>(),
            40.99m,
            29.02m
        );
        // Act
        var result = await handler.Handle(command, CancellationToken.None);
        // Assert
        result.Should().NotBeNull();
        result.Data.Category.Should().Be("Security");
    }

    [Fact]
    public async Task UpdateIncident_WhenRequesterIsOtherEmployee_ShouldThrowForbiddenAccessException()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();

        var reporter = new User
        {
            FirstName = "Ahmet",
            LastName = "Yılmaz",
            Email = "ahmet@socar.com",
            Phone = "+905550001122",
            PasswordHash = "hash",
            Department = "Saha",
            RoleType = RoleType.Employee
        };

        var otherEmployee = new User
        {
            FirstName = "Ayşe",
            LastName = "Kaya",
            Email = "ayse@socar.com",
            Phone = "+905550005566",
            PasswordHash = "hash",
            Department = "Saha",
            RoleType = RoleType.Employee
        };

        context.Users.AddRange(reporter, otherEmployee);
        await context.SaveChangesAsync();

        var incident = new Incident
        {
            ReporterId = reporter.Id,
            Category = "Fire",
            EmergencyCode = "Kırmızı Kod",
            Latitude = 40.99m,
            Longitude = 29.02m,
            Location = new NetTopologySuite.Geometries.Point(29.02, 40.99) { SRID = 4326 },
            Status = IncidentStatus.Open
        };
        context.Incidents.Add(incident);
        await context.SaveChangesAsync();

        var handler = new UpdateIncidentCommandHandler(context);
        var command = new UpdateIncidentCommand(
            incident.Id,
            otherEmployee.Id, // Yetkisi olmayan başka çalışan
            "Yetkisiz Güncelleme",
            "Mavi Kod",
            "Açıklama",
            new List<CreateIncidentMediaRequestDto>(),
            40.99m,
            29.02m
        );

        // Act & Assert
        var act = async () => await handler.Handle(command, CancellationToken.None);
        await act.Should().ThrowAsync<ForbiddenAccessException>()
            .WithMessage("You do not have permission to update this incident.");
    }

    [Fact]
    public async Task UpdateIncident_WhenIncidentNotFound_ShouldThrowEntityNotFoundException()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var handler = new UpdateIncidentCommandHandler(context);
        var command = new UpdateIncidentCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Kategori",
            "Kod",
            "Açıklama",
            new List<CreateIncidentMediaRequestDto>(),
            40.0m,
            29.0m
        );

        // Act & Assert
        var act = async () => await handler.Handle(command, CancellationToken.None);
        await act.Should().ThrowAsync<EntityNotFoundException>();
    }

    [Fact]
    public async Task UpdateIncident_WhenIncidentHasExistingMedia_ShouldReplaceMediaWithoutConcurrencyError()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();

        var reporter = new User
        {
            FirstName = "Ahmet",
            LastName = "Yılmaz",
            Email = "ahmet@socar.com",
            Phone = "+905550001122",
            PasswordHash = "hash",
            Department = "Saha",
            RoleType = RoleType.Employee
        };
        context.Users.Add(reporter);

        var category = new IncidentCategory
        {
            Code = "Fire",
            Name = "Yangın",
            IsActive = true
        };
        context.IncidentCategories.Add(category);

        var incident = new Incident
        {
            ReporterId = reporter.Id,
            Category = "Fire",
            EmergencyCode = "Kırmızı Kod",
            Description = "Orijinal Açıklama",
            Latitude = 40.99m,
            Longitude = 29.02m,
            Location = new NetTopologySuite.Geometries.Point(29.02, 40.99) { SRID = 4326 },
            Status = IncidentStatus.Open,
            MediaAttachments = new List<IncidentMedia>
            {
                new()
                {
                    MediaUrl = "http://old-media.jpg",
                    MediaType = MediaType.Photo,
                    CreatedAt = DateTime.UtcNow
                }
            }
        };
        context.Incidents.Add(incident);
        await context.SaveChangesAsync();

        var handler = new UpdateIncidentCommandHandler(context);
        var command = new UpdateIncidentCommand(
            incident.Id,
            reporter.Id,
            "Fire",
            "Kırmızı Kod",
            "Güncellenmiş Açıklama",
            new List<CreateIncidentMediaRequestDto>
            {
                new() { MediaUrl = "http://new-media.jpg", MediaType = MediaType.Photo }
            },
            40.99m,
            29.02m
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Data.MediaAttachments.Should().HaveCount(1);
        result.Data.MediaAttachments[0].MediaUrl.Should().Be("http://new-media.jpg");
    }

    [Fact]
    public async Task UpdateIncident_WhenIncidentIsResolved_ShouldThrowDomainException()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var reporter = new User
        {
            FirstName = "Murad",
            LastName = "Gasimov",
            Email = "murad@socar.az",
            PasswordHash = "hash",
            Department = "Operations",
            RoleType = RoleType.Employee
        };
        context.Users.Add(reporter);

        var category = new IncidentCategory
        {
            Code = "Fire",
            Name = "Fire Emergency",
            IsActive = true
        };
        context.IncidentCategories.Add(category);
        await context.SaveChangesAsync();

        var incident = new Incident
        {
            ReporterId = reporter.Id,
            Category = "Fire",
            EmergencyCode = "RED-1",
            Status = IncidentStatus.Resolved,
            Latitude = 40.40m,
            Longitude = 49.86m,
            Location = new NetTopologySuite.Geometries.Point(49.86, 40.40) { SRID = 4326 }
        };
        context.Incidents.Add(incident);
        await context.SaveChangesAsync();

        var handler = new UpdateIncidentCommandHandler(context);
        var command = new UpdateIncidentCommand(
            incident.Id,
            reporter.Id,
            "Fire",
            "RED-1",
            "Attempted edit after resolution",
            new List<CreateIncidentMediaRequestDto>(),
            40.40m,
            49.86m
        );

        // Act & Assert
        var act = async () => await handler.Handle(command, CancellationToken.None);
        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("Cannot edit an incident that has already been resolved or canceled.");
    }

}
