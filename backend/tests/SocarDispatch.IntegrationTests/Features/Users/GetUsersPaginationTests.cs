using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using SocarDispatch.Application.Common.Models;
using SocarDispatch.Application.Features.Auth.DTOs;
using SocarDispatch.Domain.Enums;
using SocarDispatch.IntegrationTests.Common;
using SocarDispatch.IntegrationTests.Fixtures;

namespace SocarDispatch.IntegrationTests.Features.Users;

public class GetUsersPaginationTests : IntegrationTestBase
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public GetUsersPaginationTests(CustomWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task GetUsers_ExactUserIdLookup_ReturnsTargetUserOnly()
    {
        // Arrange
        var targetUser = await SeedUserAsync("Murat", "Yilmaz", role: RoleType.Employee);
        await SeedUserAsync("Kemal", "Sunal", role: RoleType.Team);

        var client = Factory.CreateAuthenticatedClient(targetUser);

        // Act
        var response = await client.GetAsync($"/api/v1/users?userId={targetUser.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<PagedResult<UserDto>>>(JsonOptions);
        result.Should().NotBeNull();
        result!.Data.TotalCount.Should().Be(1);
        result.Data.Items.Should().ContainSingle();
        result.Data.Items.First().Id.Should().Be(targetUser.Id);
    }

    [Fact]
    public async Task GetUsers_DepartmentFilter_ReturnsMatchingUsersOnly()
    {
        // Arrange
        var requester = await SeedUserAsync("Operator", "SOCAR", role: RoleType.Operator);
        var fireUser = await SeedUserAsync("Tarik", "Akan", role: RoleType.Team, department: "Fire Safety");
        await SeedUserAsync("Sener", "Sen", role: RoleType.Employee, department: "Logistics");

        var client = Factory.CreateAuthenticatedClient(requester);

        // Act
        var response = await client.GetAsync("/api/v1/users?department=Fire Safety");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<PagedResult<UserDto>>>(JsonOptions);
        result.Should().NotBeNull();
        result!.Data.Items.Should().NotBeEmpty();
        result.Data.Items.Should().OnlyContain(u => u.Department == "Fire Safety");
        result.Data.Items.Should().Contain(u => u.Id == fireUser.Id);
    }

    [Fact]
    public async Task GetUsers_RoleFilter_ReturnsMatchingRoleOnly()
    {
        // Arrange
        var requester = await SeedUserAsync("Sistem", "Yoneticisi", role: RoleType.Operator);
        var responder = await SeedUserAsync("Halit", "Akcatepe", role: RoleType.Team);
        await SeedUserAsync("Zeki", "Alasya", role: RoleType.Employee);

        var client = Factory.CreateAuthenticatedClient(requester);

        // Act
        var response = await client.GetAsync("/api/v1/users?role=Team");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<PagedResult<UserDto>>>(JsonOptions);
        result.Should().NotBeNull();
        result!.Data.Items.Should().NotBeEmpty();
        result.Data.Items.Should().OnlyContain(u => u.RoleType == RoleType.Team);
        result.Data.Items.Should().Contain(u => u.Id == responder.Id);
    }

    [Fact]
    public async Task GetUsers_SearchTermByPhoneOrEmail_ReturnsMatchedUser()
    {
        // Arrange
        var uniquePhone = "+905559876543";
        var uniqueEmail = "cavit.ozgur@socar.az";
        var specialUser = await SeedUserAsync("Cavit", "Ozgur", email: uniqueEmail, phone: uniquePhone);
        await SeedUserAsync("Metin", "Akpinar");

        var client = Factory.CreateAuthenticatedClient(specialUser);

        // Act - Search by phone snippet
        var phoneResponse = await client.GetAsync("/api/v1/users?searchTerm=9876543");
        var emailResponse = await client.GetAsync("/api/v1/users?searchTerm=cavit.ozgur");

        // Assert
        phoneResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        emailResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var phoneResult = await phoneResponse.Content.ReadFromJsonAsync<ApiResponse<PagedResult<UserDto>>>(JsonOptions);
        var emailResult = await emailResponse.Content.ReadFromJsonAsync<ApiResponse<PagedResult<UserDto>>>(JsonOptions);

        phoneResult!.Data.Items.Should().Contain(u => u.Id == specialUser.Id);
        emailResult!.Data.Items.Should().Contain(u => u.Id == specialUser.Id);
    }

    [Fact]
    public async Task GetUsers_DeterministicOrdering_OrdersByRoleThenDepartmentThenName()
    {
        // Arrange
        var requester = await SeedUserAsync("Denetmen", "Kullanici", role: RoleType.Operator);

        await SeedUserAsync("Bulent", "Ersoy", role: RoleType.Team, department: "Safety");
        await SeedUserAsync("Ajda", "Pekkan", role: RoleType.Employee, department: "Finance");
        await SeedUserAsync("Sezen", "Aksu", role: RoleType.Employee, department: "Administration");

        var client = Factory.CreateAuthenticatedClient(requester);

        // Act
        var response = await client.GetAsync("/api/v1/users?pageSize=50");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<PagedResult<UserDto>>>(JsonOptions);
        result.Should().NotBeNull();

        var items = result!.Data.Items;
        items.Should().BeInAscendingOrder(u => u.RoleType)
            .And.ThenBeInAscendingOrder(u => u.Department)
            .And.ThenBeInAscendingOrder(u => u.FirstName)
            .And.ThenBeInAscendingOrder(u => u.LastName);
    }
}
