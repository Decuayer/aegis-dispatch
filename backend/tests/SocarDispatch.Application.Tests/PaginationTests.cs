using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SocarDispatch.Application.Common.Extensions;
using SocarDispatch.Application.Common.Models;
using SocarDispatch.Domain.Entities;
using SocarDispatch.Domain.Enums;
using SocarDispatch.Infrastructure.Persistence;
using Xunit;

namespace SocarDispatch.Application.Tests;

public class PaginationTests
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
    public void PaginationFilter_DefaultValues_ShouldBePage1AndSize25()
    {
        var filter = new PaginationFilter();

        filter.PageNumber.Should().Be(1);
        filter.PageSize.Should().Be(25);
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(-5, 1)]
    public void PaginationFilter_InvalidPageNumber_ShouldDefaultToOne(int input, int expected)
    {
        var filter = new PaginationFilter { PageNumber = input };

        filter.PageNumber.Should().Be(expected);
    }

    [Theory]
    [InlineData(0, 25)]
    [InlineData(-10, 25)]
    [InlineData(150, 100)]
    [InlineData(50, 50)]
    public void PaginationFilter_PageSize_ShouldBeConstrained(int input, int expected)
    {
        var filter = new PaginationFilter { PageSize = input };

        filter.PageSize.Should().Be(expected);
    }

    [Fact]
    public void PagedResult_MetadataCalculations_ShouldBeAccurate()
    {
        var items = new List<string> { "Item1", "Item2" };
        var result = new PagedResult<string>(items, 50, 2, 10);

        result.TotalCount.Should().Be(50);
        result.PageNumber.Should().Be(2);
        result.PageSize.Should().Be(10);
        result.TotalPages.Should().Be(5);
        result.HasPreviousPage.Should().BeTrue();
        result.HasNextPage.Should().BeTrue();
    }

    [Fact]
    public void PagedResult_EmptyList_ShouldHaveZeroPagesAndNoNavigation()
    {
        var result = new PagedResult<string>(new List<string>(), 0, 1, 25);

        result.TotalCount.Should().Be(0);
        result.TotalPages.Should().Be(0);
        result.HasPreviousPage.Should().BeFalse();
        result.HasNextPage.Should().BeFalse();
    }

    [Fact]
    public void PagedResult_ZeroPageSize_ShouldSafelyYieldZeroPages()
    {
        var result = new PagedResult<string>(new List<string>(), 10, 1, 0);

        result.TotalPages.Should().Be(0);
    }

    [Fact]
    public async Task ToPagedResultAsync_EmptyTable_ShouldReturnZeroTotalCountAndEmptyItems()
    {
        using var context = CreateInMemoryDbContext();

        var result = await context.Users.ToPagedResultAsync(1, 25);

        result.TotalCount.Should().Be(0);
        result.Items.Should().BeEmpty();
        result.TotalPages.Should().Be(0);
        result.HasPreviousPage.Should().BeFalse();
        result.HasNextPage.Should().BeFalse();
    }

    [Fact]
    public async Task ToPagedResultAsync_MultiplePages_ShouldSliceAndSetFlagsCorrectly()
    {
        using var context = CreateInMemoryDbContext();

        // Seed 65 users
        for (int i = 1; i <= 65; i++)
        {
            context.Users.Add(new User
            {
                FirstName = $"User{i:D2}",
                LastName = "Test",
                Email = $"user{i}@socar.com",
                Phone = $"+90555000{i:D4}",
                PasswordHash = "hash",
                Department = "İSG",
                RoleType = RoleType.Employee
            });
        }
        await context.SaveChangesAsync();

        // Page 1: 25 items expected
        var page1 = await context.Users.OrderBy(u => u.FirstName).ToPagedResultAsync(1, 25);
        page1.TotalCount.Should().Be(65);
        page1.TotalPages.Should().Be(3);
        page1.Items.Should().HaveCount(25);
        page1.HasPreviousPage.Should().BeFalse();
        page1.HasNextPage.Should().BeTrue();
        page1.Items.First().FirstName.Should().Be("User01");

        // Page 2: 25 items expected
        var page2 = await context.Users.OrderBy(u => u.FirstName).ToPagedResultAsync(2, 25);
        page2.Items.Should().HaveCount(25);
        page2.HasPreviousPage.Should().BeTrue();
        page2.HasNextPage.Should().BeTrue();
        page2.Items.First().FirstName.Should().Be("User26");

        // Page 3: 15 remaining items expected
        var page3 = await context.Users.OrderBy(u => u.FirstName).ToPagedResultAsync(3, 25);
        page3.Items.Should().HaveCount(15);
        page3.HasPreviousPage.Should().BeTrue();
        page3.HasNextPage.Should().BeFalse();
        page3.Items.First().FirstName.Should().Be("User51");
    }
}
