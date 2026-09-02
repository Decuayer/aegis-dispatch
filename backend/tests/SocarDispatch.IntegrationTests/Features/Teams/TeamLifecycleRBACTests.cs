using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using NetTopologySuite.Geometries;
using SocarDispatch.Application.Common.Models;
using SocarDispatch.Application.Features.Teams.DTOs;
using SocarDispatch.Domain.Entities;
using SocarDispatch.Domain.Enums;
using SocarDispatch.IntegrationTests.Common;
using SocarDispatch.IntegrationTests.Fixtures;

namespace SocarDispatch.IntegrationTests.Features.Teams;

public class TeamLifecycleRBACTests : IntegrationTestBase
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public TeamLifecycleRBACTests(CustomWebApplicationFactory factory) : base(factory)
    {
    }

    private async Task AddMemberToTeamAsync(Guid teamId, Guid userId)
    {
        await ExecuteDbContextAsync(async context =>
        {
            context.TeamMembers.Add(new TeamMember
            {
                TeamId = teamId,
                UserId = userId,
                MemberStatus = TeamMemberStatus.Available,
                JoinedAt = DateTime.UtcNow
            });
            await context.SaveChangesAsync();
        });
    }

    private async Task CreateActiveAssignmentAsync(Guid teamId, Guid operatorId)
    {
        await ExecuteDbContextAsync(async context =>
        {
            var incident = new Incident
            {
                Id = Guid.NewGuid(),
                ReporterId = operatorId,
                Category = "Yangın",
                EmergencyCode = "Kırmızı Kod",
                Status = IncidentStatus.Assigned,
                Latitude = 38.7900m,
                Longitude = 26.9200m,
                Location = new Point(26.9200, 38.7900) { SRID = 4326 },
                CreatedAt = DateTime.UtcNow
            };
            context.Incidents.Add(incident);

            context.Assignments.Add(new Assignment
            {
                Id = Guid.NewGuid(),
                IncidentId = incident.Id,
                TeamId = teamId,
                OperatorId = operatorId,
                AssignedAt = DateTime.UtcNow,
                CompletedAt = null
            });

            await context.SaveChangesAsync();
        });
    }

    // --- 1. Self-Join Rules ---

    [Fact]
    public async Task SelfJoin_UnassignedResponder_SuccessfullyJoinsIdleTeam()
    {
        // Arrange
        var responder = await SeedUserAsync("Serkan", "Aydin", role: RoleType.Team);
        var team = await SeedTeamAsync("A Blok Yangın Ekibi", TeamStatus.Idle);
        var client = Factory.CreateAuthenticatedClient(responder);

        var request = new AddTeamMemberRequestDto(responder.Id);

        // Act
        var response = await client.PostAsJsonAsync($"/api/v1/teams/{team.Id}/members", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<TeamDto>>(JsonOptions);
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Data.Members.Should().Contain(m => m.UserId == responder.Id);
    }

    [Fact]
    public async Task SelfJoin_ResponderAlreadyInAnotherTeam_ReturnsBadRequest()
    {
        // Arrange
        var responder = await SeedUserAsync("Deniz", "Celik", role: RoleType.Team);
        var primaryTeam = await SeedTeamAsync("Birincil Ekip", TeamStatus.Idle);
        await AddMemberToTeamAsync(primaryTeam.Id, responder.Id);

        var targetTeam = await SeedTeamAsync("Hedef Ekip", TeamStatus.Idle);
        var client = Factory.CreateAuthenticatedClient(responder);

        var request = new AddTeamMemberRequestDto(responder.Id);

        // Act
        var response = await client.PostAsJsonAsync($"/api/v1/teams/{targetTeam.Id}/members", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task SelfJoin_EmployeeRole_ReturnsForbidden()
    {
        // Arrange
        var employee = await SeedUserAsync("Ahmet", "Koc", role: RoleType.Employee);
        var team = await SeedTeamAsync("Guvenlik Ekibi", TeamStatus.Idle);
        var client = Factory.CreateAuthenticatedClient(employee);

        var request = new AddTeamMemberRequestDto(employee.Id);

        // Act
        var response = await client.PostAsJsonAsync($"/api/v1/teams/{team.Id}/members", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task SelfJoin_NonIdleTeam_ReturnsBadRequest()
    {
        // Arrange
        var responder = await SeedUserAsync("Mehmet", "Demir", role: RoleType.Team);
        var team = await SeedTeamAsync("Operasyondaki Ekip", TeamStatus.OnScene);
        var client = Factory.CreateAuthenticatedClient(responder);

        var request = new AddTeamMemberRequestDto(responder.Id);

        // Act
        var response = await client.PostAsJsonAsync($"/api/v1/teams/{team.Id}/members", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // --- 2. Self-Leave Rules ---

    [Fact]
    public async Task SelfLeave_ActiveMemberFromIdleTeam_SuccessfullyLeaves()
    {
        // Arrange
        var member = await SeedUserAsync("Caner", "Erkin", role: RoleType.Team);
        var team = await SeedTeamAsync("Kurtarma Timi", TeamStatus.Idle);
        await AddMemberToTeamAsync(team.Id, member.Id);

        var client = Factory.CreateAuthenticatedClient(member);

        // Act
        var response = await client.DeleteAsync($"/api/v1/teams/{team.Id}/members/{member.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<TeamDto>>(JsonOptions);
        result.Should().NotBeNull();
        result!.Data.Members.Should().NotContain(m => m.UserId == member.Id);
    }

    [Fact]
    public async Task SelfLeave_TeamWithActiveEmergencyAssignment_ReturnsBadRequest()
    {
        // Arrange
        var operatorUser = await SeedUserAsync("Kriz", "Operatoru", role: RoleType.Operator);
        var member = await SeedUserAsync("Volkan", "Demirel", role: RoleType.Team);
        var team = await SeedTeamAsync("Mudahale Timi", TeamStatus.Forwarded);
        await AddMemberToTeamAsync(team.Id, member.Id);
        await CreateActiveAssignmentAsync(team.Id, operatorUser.Id);

        var client = Factory.CreateAuthenticatedClient(member);

        // Act
        var response = await client.DeleteAsync($"/api/v1/teams/{team.Id}/members/{member.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task SelfLeave_DepartingLeader_ClearsLeaderIdToNull()
    {
        // Arrange
        var leader = await SeedUserAsync("Hakan", "Sukur", role: RoleType.Team);
        var team = await SeedTeamAsync("Oncu Ekip", TeamStatus.Idle, leaderId: leader.Id);
        await AddMemberToTeamAsync(team.Id, leader.Id);

        var client = Factory.CreateAuthenticatedClient(leader);

        // Act
        var response = await client.DeleteAsync($"/api/v1/teams/{team.Id}/members/{leader.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<TeamDto>>(JsonOptions);
        result.Should().NotBeNull();
        result!.Data.LeaderId.Should().BeNull();
    }

    // --- 3. Leadership Succession & Member Removal ---

    [Fact]
    public async Task ClaimLeadership_ActiveMemberOnVacantTeam_SuccessfullyClaimsLeadership()
    {
        // Arrange
        var member = await SeedUserAsync("Arda", "Turan", role: RoleType.Team);
        var team = await SeedTeamAsync("Lidersiz Ekip", TeamStatus.Idle, leaderId: null);
        await AddMemberToTeamAsync(team.Id, member.Id);

        var client = Factory.CreateAuthenticatedClient(member);
        var request = new UpdateTeamRequestDto(team.TeamName, member.Id);

        // Act
        var response = await client.PutAsJsonAsync($"/api/v1/teams/{team.Id}", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<TeamDto>>(JsonOptions);
        result.Should().NotBeNull();
        result!.Data.LeaderId.Should().Be(member.Id);
    }

    [Fact]
    public async Task ClaimLeadership_NonMemberOnVacantTeam_ReturnsForbidden()
    {
        // Arrange
        var outsider = await SeedUserAsync("Yabanci", "Uye", role: RoleType.Team);
        var team = await SeedTeamAsync("Kapali Ekip", TeamStatus.Idle, leaderId: null);

        var client = Factory.CreateAuthenticatedClient(outsider);
        var request = new UpdateTeamRequestDto(team.TeamName, outsider.Id);

        // Act
        var response = await client.PutAsJsonAsync($"/api/v1/teams/{team.Id}", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task RemoveMember_DesignatedLeaderRemovesMember_SuccessfullyRemoves()
    {
        // Arrange
        var leader = await SeedUserAsync("Ekip", "Lideri", role: RoleType.Team);
        var member = await SeedUserAsync("Ekip", "Uyesi", role: RoleType.Team);
        var team = await SeedTeamAsync("Takim Birimi", TeamStatus.Idle, leaderId: leader.Id);
        await AddMemberToTeamAsync(team.Id, leader.Id);
        await AddMemberToTeamAsync(team.Id, member.Id);

        var client = Factory.CreateAuthenticatedClient(leader);

        // Act
        var response = await client.DeleteAsync($"/api/v1/teams/{team.Id}/members/{member.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<TeamDto>>(JsonOptions);
        result.Should().NotBeNull();
        result!.Data.Members.Should().NotContain(m => m.UserId == member.Id);
    }

    [Fact]
    public async Task RemoveMember_NonLeaderMemberAttemptsRemoval_ReturnsForbidden()
    {
        // Arrange
        var leader = await SeedUserAsync("Asil", "Lider", role: RoleType.Team);
        var regularMember = await SeedUserAsync("Siradan", "Uye", role: RoleType.Team);
        var targetMember = await SeedUserAsync("Hedef", "Uye", role: RoleType.Team);

        var team = await SeedTeamAsync("Mevcut Takim", TeamStatus.Idle, leaderId: leader.Id);
        await AddMemberToTeamAsync(team.Id, leader.Id);
        await AddMemberToTeamAsync(team.Id, regularMember.Id);
        await AddMemberToTeamAsync(team.Id, targetMember.Id);

        var client = Factory.CreateAuthenticatedClient(regularMember);

        // Act
        var response = await client.DeleteAsync($"/api/v1/teams/{team.Id}/members/{targetMember.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
