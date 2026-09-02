using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using MediatR;
using Moq;
using SocarDispatch.Application.Common.Behaviors;
using Xunit;
using ValidationException = SocarDispatch.Application.Common.Exceptions.ValidationException;

namespace SocarDispatch.UnitTests.Application.Pipeline;

public class ValidationBehaviorTests
{
    public record TestCommand(string Name) : IRequest<string>;

    [Fact]
    public async Task Handle_WhenValidationFails_ShouldThrowCustomValidationException()
    {
        // Arrange
        var validatorMock = new Mock<IValidator<TestCommand>>();
        validatorMock
            .Setup(v => v.ValidateAsync(It.IsAny<ValidationContext<TestCommand>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult(new[] { new ValidationFailure("Name", "Name is required") }));

        var behavior = new ValidationBehavior<TestCommand, string>(new[] { validatorMock.Object });
        RequestHandlerDelegate<string> next = _ => Task.FromResult("Success");

        // Act
        Func<Task> act = async () => await behavior.Handle(new TestCommand(""), next, CancellationToken.None);

        // Assert
        var ex = await act.Should().ThrowAsync<ValidationException>();
        ex.Which.Errors.Should().ContainKey("Name");
    }

    [Fact]
    public async Task Handle_WhenValidationSucceeds_ShouldCallNextDelegate()
    {
        // Arrange
        var validatorMock = new Mock<IValidator<TestCommand>>();
        validatorMock
            .Setup(v => v.ValidateAsync(It.IsAny<ValidationContext<TestCommand>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        var behavior = new ValidationBehavior<TestCommand, string>(new[] { validatorMock.Object });
        bool nextCalled = false;
        RequestHandlerDelegate<string> next = _ =>
        {
            nextCalled = true;
            return Task.FromResult("Success");
        };

        // Act
        var result = await behavior.Handle(new TestCommand("ValidName"), next, CancellationToken.None);

        // Assert
        nextCalled.Should().BeTrue();
        result.Should().Be("Success");
    }
}
