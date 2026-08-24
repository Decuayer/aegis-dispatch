using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SocarDispatch.Application.Features.Users.Queries.GetUsers;
using SocarDispatch.Domain.Entities;
using SocarDispatch.Domain.Enums;
using SocarDispatch.Infrastructure.Persistence;
using Xunit;

namespace SocarDispatch.Application.Tests;

public class GetUsersDirectoryQueryTests
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
    public async Task GetUsersQuery_FilterByRoleType_ShouldReturnOnlyMatchingUsers()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();

        var emp1 = new User { FirstName = "Ali", LastName = "Yılmaz", Email = "ali@socar.com", Phone = "+905551111111", PasswordHash = "h", Department = "İSG", RoleType = RoleType.Employee };
        var team1 = new User { FirstName = "Mehmet", LastName = "Demir", Email = "mehmet@socar.com", Phone = "+905552222222", PasswordHash = "h", Department = "İSG", RoleType = RoleType.Team };
        var op1 = new User { FirstName = "Zeynep", LastName = "Kaya", Email = "zeynep@socar.com", Phone = "+905553333333", PasswordHash = "h", Department = "Merkez", RoleType = RoleType.Operator };

        context.Users.AddRange(emp1, team1, op1);
        await context.SaveChangesAsync();

        var handler = new GetUsersQueryHandler(context);
        var query = new GetUsersQuery(null, null, RoleType.Team);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Data.Should().HaveCount(1);
        result.Data.First().FirstName.Should().Be("Mehmet");
        result.Data.First().RoleType.Should().Be(RoleType.Team);
    }

    [Fact]
    public async Task GetUsersQuery_FilterByDepartment_ShouldReturnOnlyMatchingUsers()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();

        var user1 = new User { FirstName = "Can", LastName = "Tekin", Email = "can@socar.com", Phone = "+905554444444", PasswordHash = "h", Department = "İSG", RoleType = RoleType.Employee };
        var user2 = new User { FirstName = "Ayşe", LastName = "Şahin", Email = "ayse@socar.com", Phone = "+905555555555", PasswordHash = "h", Department = "İtfaiye", RoleType = RoleType.Team };

        context.Users.AddRange(user1, user2);
        await context.SaveChangesAsync();

        var handler = new GetUsersQueryHandler(context);
        var query = new GetUsersQuery(null, "İSG", null);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Data.Should().HaveCount(1);
        result.Data.First().FirstName.Should().Be("Can");
        result.Data.First().Department.Should().Be("İSG");
    }

    [Fact]
    public async Task GetUsersQuery_SearchByNameOrEmail_ShouldReturnMatchingUsers()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();

        var user1 = new User { FirstName = "Demir", LastName = "Cucu", Email = "demir@socar.com", Phone = "+905556666666", PasswordHash = "h", Department = "Bilgi Teknolojileri", RoleType = RoleType.Employee };
        var user2 = new User { FirstName = "Fatma", LastName = "Öztürk", Email = "fatma@socar.com", Phone = "+905557777777", PasswordHash = "h", Department = "İSG", RoleType = RoleType.Team };

        context.Users.AddRange(user1, user2);
        await context.SaveChangesAsync();

        var handler = new GetUsersQueryHandler(context);
        var query = new GetUsersQuery("Demir", null, null);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Data.Should().HaveCount(1);
        result.Data.First().FirstName.Should().Be("Demir");
    }
}
