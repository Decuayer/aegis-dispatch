using FluentAssertions;
using FluentValidation.TestHelper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Moq;
using SocarDispatch.Application.Features.Incidents.Commands.CreateIncident;
using SocarDispatch.Domain.Entities;
using SocarDispatch.Domain.Enums;
using SocarDispatch.Domain.Events;
using SocarDispatch.Domain.Exceptions;
using SocarDispatch.Infrastructure.Persistence;
using Xunit;

namespace SocarDispatch.UnitTests.Application.Incidents;

public class CreateIncidentCommandHandlerTests
{
    private readonly Mock<IPublisher> _publisherMock;
    private readonly CreateIncidentCommandValidator _validator;

    public CreateIncidentCommandHandlerTests()
    {
        _publisherMock = new Mock<IPublisher>();
        _validator = new CreateIncidentCommandValidator();
    }

    private static ApplicationDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task Handle_ValidRequest_ShouldCreateIncidentAndPublishEvent()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var reporter = new User
        {
            Id = Guid.NewGuid(),
            FirstName = "Hasan",
            LastName = "Ali",
            Email = "hasan@socar.az",
            Phone = "+905551112233",
            PasswordHash = "hash123",
            Department = "Refinery Operations",
            RoleType = RoleType.Employee
        };

        var emergencyCode = new EmergencyCodeDefinition { Code = "KOD-RED", Description = "Fire Emergency", IsActive = true };
        var category = new IncidentCategory { Code = "FIRE", Name = "Fire", IsActive = true };

        context.Users.Add(reporter);
        context.EmergencyCodes.Add(emergencyCode);
        context.IncidentCategories.Add(category);
        await context.SaveChangesAsync();

        var handler = new CreateIncidentCommandHandler(context, _publisherMock.Object);
        var command = new CreateIncidentCommand(
            ReporterId: reporter.Id,
            Category: "FIRE",
            EmergencyCode: "KOD-RED",
            Description: "Flames detected in crude unit",
            MediaAttachments: new(),
            Latitude: 38.7915m,
            Longitude: 26.9212m
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Category.Should().Be("FIRE");
        result.Data.EmergencyCode.Should().Be("KOD-RED");
        result.Data.Status.Should().Be(IncidentStatus.Open.ToString());

        _publisherMock.Verify(p => p.Publish(It.IsAny<IncidentCreatedEvent>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_NonExistentReporter_ShouldThrowEntityNotFoundException()
    {
        using var context = CreateInMemoryDbContext();
        var handler = new CreateIncidentCommandHandler(context, _publisherMock.Object);
        var command = new CreateIncidentCommand(
            ReporterId: Guid.NewGuid(),
            Category: "FIRE",
            EmergencyCode: "KOD-RED",
            Description: "Unknown reporter test",
            MediaAttachments: new(),
            Latitude: 38.7915m,
            Longitude: 26.9212m
        );

        Func<Task> act = async () => await handler.Handle(command, CancellationToken.None);
        await act.Should().ThrowAsync<EntityNotFoundException>();
    }

    [Fact]
    public void Validator_InvalidCoordinates_ShouldHaveValidationErrors()
    {
        var command = new CreateIncidentCommand(
            ReporterId: Guid.NewGuid(),
            Category: "FIRE",
            EmergencyCode: "RED",
            Description: "Invalid coords",
            MediaAttachments: new(),
            Latitude: 95.0m,  // Exceeds latitude range [-90, 90]
            Longitude: 200.0m // Exceeds longitude range [-180, 180]
        );

        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Latitude);
        result.ShouldHaveValidationErrorFor(x => x.Longitude);
    }
}
