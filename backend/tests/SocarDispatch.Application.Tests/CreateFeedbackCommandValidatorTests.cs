using FluentValidation.TestHelper;
using Microsoft.AspNetCore.Http;
using Moq;
using SocarDispatch.Application.Features.Feedbacks.Commands.CreateFeedback;
using Xunit;

namespace SocarDispatch.Application.Tests;

public class CreateFeedbackCommandValidatorTests
{
    private readonly CreateFeedbackCommandValidator _validator = new();

    [Fact]
    public void Should_Fail_When_UserId_Is_Empty()
    {
        var command = new CreateFeedbackCommand(Guid.Empty, "Valid Title", "Valid Description");
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.UserId);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Should_Fail_When_Title_Is_Empty(string? title)
    {
        var command = new CreateFeedbackCommand(Guid.NewGuid(), title!, "Valid Description");
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Title);
    }

    [Fact]
    public void Should_Fail_When_Title_Exceeds_200_Characters()
    {
        var longTitle = new string('A', 201);
        var command = new CreateFeedbackCommand(Guid.NewGuid(), longTitle, "Valid Description");
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Title);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Should_Fail_When_Description_Is_Empty(string? description)
    {
        var command = new CreateFeedbackCommand(Guid.NewGuid(), "Valid Title", description!);
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Description);
    }

    [Fact]
    public void Should_Fail_When_Description_Exceeds_4000_Characters()
    {
        var longDescription = new string('B', 4001);
        var command = new CreateFeedbackCommand(Guid.NewGuid(), "Valid Title", longDescription);
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Description);
    }

    [Fact]
    public void Should_Fail_When_Attachments_Count_Exceeds_5()
    {
        var files = new List<IFormFile>();
        for (int i = 0; i < 6; i++)
        {
            var mock = new Mock<IFormFile>();
            mock.Setup(f => f.Length).Returns(1024);
            mock.Setup(f => f.ContentType).Returns("image/jpeg");
            files.Add(mock.Object);
        }

        var command = new CreateFeedbackCommand(Guid.NewGuid(), "Title", "Description", files);
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Attachments);
    }

    [Theory]
    [InlineData("application/pdf")]
    [InlineData("text/plain")]
    [InlineData("application/octet-stream")]
    public void Should_Fail_When_Attachment_Has_Unsupported_MimeType(string contentType)
    {
        var mock = new Mock<IFormFile>();
        mock.Setup(f => f.Length).Returns(1024);
        mock.Setup(f => f.ContentType).Returns(contentType);

        var command = new CreateFeedbackCommand(Guid.NewGuid(), "Title", "Description", new List<IFormFile> { mock.Object });
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Attachments);
    }

    [Fact]
    public void Should_Fail_When_Image_Exceeds_10MB()
    {
        var mock = new Mock<IFormFile>();
        mock.Setup(f => f.Length).Returns(10 * 1024 * 1024 + 1);
        mock.Setup(f => f.ContentType).Returns("image/jpeg");

        var command = new CreateFeedbackCommand(Guid.NewGuid(), "Title", "Description", new List<IFormFile> { mock.Object });
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Attachments);
    }

    [Fact]
    public void Should_Fail_When_Video_Exceeds_50MB()
    {
        var mock = new Mock<IFormFile>();
        mock.Setup(f => f.Length).Returns(50 * 1024 * 1024 + 1);
        mock.Setup(f => f.ContentType).Returns("video/mp4");

        var command = new CreateFeedbackCommand(Guid.NewGuid(), "Title", "Description", new List<IFormFile> { mock.Object });
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Attachments);
    }

    [Theory]
    [InlineData("image/jpeg", 5 * 1024 * 1024)]
    [InlineData("image/png", 8 * 1024 * 1024)]
    [InlineData("image/webp", 2 * 1024 * 1024)]
    [InlineData("video/mp4", 45 * 1024 * 1024)]
    [InlineData("video/quicktime", 30 * 1024 * 1024)]
    public void Should_Pass_When_Attachments_Are_Valid(string contentType, long sizeBytes)
    {
        var mock = new Mock<IFormFile>();
        mock.Setup(f => f.Length).Returns(sizeBytes);
        mock.Setup(f => f.ContentType).Returns(contentType);

        var command = new CreateFeedbackCommand(Guid.NewGuid(), "Valid Title", "Valid Description", new List<IFormFile> { mock.Object });
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveAnyValidationErrors();
    }
}
