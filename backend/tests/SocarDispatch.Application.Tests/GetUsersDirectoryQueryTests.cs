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
        result.Data.Items.Should().HaveCount(1);
        result.Data.TotalCount.Should().Be(1);
        result.Data.Items.First().FirstName.Should().Be("Mehmet");
        result.Data.Items.First().RoleType.Should().Be(RoleType.Team);
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
        result.Data.Items.Should().HaveCount(1);
        result.Data.TotalCount.Should().Be(1);
        result.Data.Items.First().FirstName.Should().Be("Can");
        result.Data.Items.First().Department.Should().Be("İSG");
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
        result.Data.Items.Should().HaveCount(1);
        result.Data.TotalCount.Should().Be(1);
        result.Data.Items.First().FirstName.Should().Be("Demir");
    }

    [Fact]
    public async Task GetUsersQuery_FilterByUserId_ShouldReturnExactUser()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();

        var targetUser = new User { FirstName = "Ahmet", LastName = "Kaya", Email = "ahmet@socar.com", Phone = "+905558888888", PasswordHash = "h", Department = "İSG", RoleType = RoleType.Employee };
        var otherUser = new User { FirstName = "Mehmet", LastName = "Kaya", Email = "mehmet.k@socar.com", Phone = "+905559999999", PasswordHash = "h", Department = "İSG", RoleType = RoleType.Employee };

        context.Users.AddRange(targetUser, otherUser);
        await context.SaveChangesAsync();

        var handler = new GetUsersQueryHandler(context);
        var query = new GetUsersQuery { UserId = targetUser.Id };

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Data.Items.Should().HaveCount(1);
        result.Data.TotalCount.Should().Be(1);
        result.Data.Items.First().Id.Should().Be(targetUser.Id);
    }

    [Fact]
    public async Task GetUsersQuery_Pagination_ShouldRespectPageNumberAndPageSize()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();

        for (int i = 1; i <= 30; i++)
        {
            context.Users.Add(new User
            {
                FirstName = $"User{i:D2}",
                LastName = "Test",
                Email = $"user{i}@socar.com",
                Phone = $"+9055500000{i:D2}",
                PasswordHash = "h",
                Department = "IT",
                RoleType = RoleType.Employee
            });
        }
        await context.SaveChangesAsync();

        var handler = new GetUsersQueryHandler(context);
        var query = new GetUsersQuery { PageNumber = 2, PageSize = 10 };

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Data.Items.Should().HaveCount(10);
        result.Data.TotalCount.Should().Be(30);
        result.Data.PageNumber.Should().Be(2);
        result.Data.PageSize.Should().Be(10);
        result.Data.TotalPages.Should().Be(3);
        result.Data.HasPreviousPage.Should().BeTrue();
        result.Data.HasNextPage.Should().BeTrue();
    }

    [Fact]
    public void GetUsersQueryValidator_ValidInputs_ShouldPassValidation()
    {
        // Arrange
        var validator = new GetUsersQueryValidator();
        var validQuery = new GetUsersQuery
        {
            PageNumber = 1,
            PageSize = 25,
            SearchTerm = "Demir",
            Department = "IT"
        };

        // Act
        var result = validator.Validate(validQuery);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void GetUsersQueryValidator_ExcessiveLengths_ShouldFailValidation()
    {
        // Arrange
        var validator = new GetUsersQueryValidator();
        var invalidQuery = new GetUsersQuery
        {
            SearchTerm = new string('a', 101),
            Department = new string('b', 101)
        };

        // Act
        var result = validator.Validate(invalidQuery);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "SearchTerm");
        result.Errors.Should().Contain(e => e.PropertyName == "Department");
    }
}
