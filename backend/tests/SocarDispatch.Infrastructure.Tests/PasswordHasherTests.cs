using SocarDispatch.Infrastructure.Services;
using Xunit;
using Xunit.Abstractions;

namespace SocarDispatch.Infrastructure.Tests;

public class PasswordHasherTests
{
    private readonly ITestOutputHelper _output;

    public PasswordHasherTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void HashAndVerifyPassword_ShouldSucceed()
    {
        var hasher = new PasswordHasher();
        var password = "Operator123!";
        var hash = hasher.HashPassword(password);

        _output.WriteLine($"GENERATED_HASH:{hash}");

        Assert.True(hasher.VerifyPassword(password, hash));
        Assert.False(hasher.VerifyPassword("WrongPassword", hash));
    }
}
