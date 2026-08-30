using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SocarDispatch.Application.Features.Teams;
using SocarDispatch.Application.Features.Teams.Commands.AddTeamMember;
using SocarDispatch.Application.Features.Teams.Commands.CreateTeam;
using SocarDispatch.Application.Features.Teams.Commands.RemoveTeamMember;
using SocarDispatch.Application.Features.Teams.Commands.UpdateTeam;
using SocarDispatch.Application.Features.Teams.Queries.GetAvailableTeams;
using SocarDispatch.Application.Features.Teams.Queries.GetTeamById;
using SocarDispatch.Domain.Entities;
using SocarDispatch.Domain.Enums;
using SocarDispatch.Domain.Exceptions;
using SocarDispatch.Infrastructure.Persistence;
using Xunit;

namespace SocarDispatch.Application.Tests;

public class TeamCrudTests
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
    public async Task CreateTeam_WhenValid_ShouldSucceed()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var leader = new User
        {
            FirstName = "Ahmet",
            LastName = "Yılmaz",
            Email = "ahmet@socar.com",
            Phone = "+905551111111",
            PasswordHash = "hash",
            Department = "Arama Kurtarma",
            RoleType = RoleType.Team
        };
        context.Users.Add(leader);
        await context.SaveChangesAsync();

        var handler = new CreateTeamCommandHandler(context);
        var command = new CreateTeamCommand("A Blok İSG Ekibi", leader.Id, leader.Id);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Data.TeamName.Should().Be("A Blok İSG Ekibi");
        result.Data.LeaderId.Should().Be(leader.Id);
        result.Data.Members.Should().ContainSingle(m => m.UserId == leader.Id);
    }

    [Fact]
    public async Task CreateTeam_WhenNameDuplicate_ShouldThrowDomainException()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        context.Teams.Add(new Team { TeamName = "Mevcut Ekip", Status = TeamStatus.Idle });
        await context.SaveChangesAsync();

        var handler = new CreateTeamCommandHandler(context);
        var command = new CreateTeamCommand("mevcut ekip", null, Guid.NewGuid());

        // Act & Assert
        var act = async () => await handler.Handle(command, CancellationToken.None);
        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("*already exists*");
    }

    [Fact]
    public async Task CreateTeam_WhenLeaderIsNotTeamRole_ShouldThrowDomainException()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var employee = new User
        {
            FirstName = "Mehmet",
            LastName = "Demir",
            Email = "mehmet@socar.com",
            Phone = "+905552222222",
            PasswordHash = "hash",
            Department = "Üretim",
            RoleType = RoleType.Employee // NOT Team!
        };
        context.Users.Add(employee);
        await context.SaveChangesAsync();

        var handler = new CreateTeamCommandHandler(context);
        var command = new CreateTeamCommand("Yeni Ekip", employee.Id, employee.Id);

        // Act & Assert
        var act = async () => await handler.Handle(command, CancellationToken.None);
        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("*Designated team leader must have RoleType 'Team'*");
    }

    [Fact]
    public async Task GetTeamById_WhenTeamExists_ShouldReturnDetails()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var team = new Team { TeamName = "Fire Müdahale Ekibi", Status = TeamStatus.Idle };
        context.Teams.Add(team);
        await context.SaveChangesAsync();

        var handler = new GetTeamByIdQueryHandler(context);
        var query = new GetTeamByIdQuery(team.Id);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Data.TeamName.Should().Be("Fire Müdahale Ekibi");
    }

    [Fact]
    public async Task AddTeamMember_WhenUserAlreadyInAnotherTeam_ShouldThrowDomainException()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var user = new User
        {
            FirstName = "Can",
            LastName = "Kaya",
            Email = "can@socar.com",
            Phone = "+905553333333",
            PasswordHash = "hash",
            Department = "İSG",
            RoleType = RoleType.Team
        };
        context.Users.Add(user);

        var team1 = new Team { TeamName = "1. Ekip", Status = TeamStatus.Idle };
        var team2 = new Team { TeamName = "2. Ekip", Status = TeamStatus.Idle };
        team1.Members.Add(new TeamMember { UserId = user.Id });
        context.Teams.AddRange(team1, team2);

        var operatorUser = new User { RoleType = RoleType.Operator, Email = "op@socar.com" };
        context.Users.Add(operatorUser);
        await context.SaveChangesAsync();

        var handler = new AddTeamMemberCommandHandler(context);
        var command = new AddTeamMemberCommand(team2.Id, operatorUser.Id, user.Id);

        // Act & Assert
        var act = async () => await handler.Handle(command, CancellationToken.None);
        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("*already an active member*");
    }

    [Fact]
    public async Task RemoveTeamMember_WhenMemberIsLeader_ShouldSucceedAndResetLeaderIdToNull()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var leader = new User
        {
            FirstName = "Serkan",
            LastName = "Kaya",
            Email = "serkan@socar.com",
            Phone = "+905554444444",
            PasswordHash = "hash",
            Department = "İSG",
            RoleType = RoleType.Team
        };
        context.Users.Add(leader);

        var team = new Team { TeamName = "Liderli Ekip", LeaderId = leader.Id, Status = TeamStatus.Idle };
        team.Members.Add(new TeamMember { UserId = leader.Id });
        context.Teams.Add(team);

        var operatorUser = new User { RoleType = RoleType.Operator, Email = "op2@socar.com" };
        context.Users.Add(operatorUser);
        await context.SaveChangesAsync();

        var handler = new RemoveTeamMemberCommandHandler(context);
        var command = new RemoveTeamMemberCommand(team.Id, operatorUser.Id, leader.Id);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Data.LeaderId.Should().BeNull();
        result.Data.Members.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAvailableTeams_ShouldReturnOnlyIdleTeamsWithOpenCapacity()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var idleTeamWithSpace = new Team { TeamName = "Müsait Ekip", Status = TeamStatus.Idle };
        var busyTeam = new Team { TeamName = "Meşgul Ekip", Status = TeamStatus.Busy };
        var fullTeam = new Team { TeamName = "Dolu Ekip", Status = TeamStatus.Idle };

        for (int i = 0; i < TeamConstants.MaxOperationalCapacity; i++)
        {
            var u = new User { Email = $"full_{i}@socar.com", RoleType = RoleType.Team };
            context.Users.Add(u);
            fullTeam.Members.Add(new TeamMember { UserId = u.Id });
        }

        context.Teams.AddRange(idleTeamWithSpace, busyTeam, fullTeam);
        await context.SaveChangesAsync();

        var handler = new GetAvailableTeamsQueryHandler(context);

        // Act
        var result = await handler.Handle(new GetAvailableTeamsQuery(), CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Data.Should().ContainSingle(t => t.Id == idleTeamWithSpace.Id);
        result.Data.Should().NotContain(t => t.Id == busyTeam.Id);
        result.Data.Should().NotContain(t => t.Id == fullTeam.Id);
    }

    [Fact]
    public async Task AddTeamMember_SelfJoin_WhenValid_ShouldSucceed()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var responder = new User
        {
            FirstName = "Ali",
            LastName = "Veli",
            Email = "ali@socar.com",
            Phone = "+905559998877",
            PasswordHash = "hash",
            Department = "Arama Kurtarma",
            RoleType = RoleType.Team
        };
        context.Users.Add(responder);

        var team = new Team { TeamName = "Kurtarma Ekibi", Status = TeamStatus.Idle };
        context.Teams.Add(team);
        await context.SaveChangesAsync();

        var handler = new AddTeamMemberCommandHandler(context);
        var command = new AddTeamMemberCommand(team.Id, responder.Id, responder.Id);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Data.Members.Should().ContainSingle(m => m.UserId == responder.Id);
    }

    [Fact]
    public async Task AddTeamMember_SelfJoin_WhenTeamNotIdle_ShouldThrowDomainException()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var responder = new User
        {
            FirstName = "Ali",
            LastName = "Veli",
            Email = "ali2@socar.com",
            Phone = "+905559998876",
            PasswordHash = "hash",
            RoleType = RoleType.Team
        };
        context.Users.Add(responder);

        var team = new Team { TeamName = "Görevdeki Ekip", Status = TeamStatus.Busy };
        context.Teams.Add(team);
        await context.SaveChangesAsync();

        var handler = new AddTeamMemberCommandHandler(context);
        var command = new AddTeamMemberCommand(team.Id, responder.Id, responder.Id);

        // Act & Assert
        var act = async () => await handler.Handle(command, CancellationToken.None);
        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("*Cannot join a team that is not in Idle status*");
    }

    [Fact]
    public async Task AddTeamMember_SelfJoin_WhenUserNotTeamRole_ShouldThrowDomainException()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var employee = new User
        {
            FirstName = "Hasan",
            LastName = "Kurt",
            Email = "hasan@socar.com",
            Phone = "+905559998875",
            PasswordHash = "hash",
            RoleType = RoleType.Employee
        };
        context.Users.Add(employee);

        var team = new Team { TeamName = "Boş Ekip", Status = TeamStatus.Idle };
        context.Teams.Add(team);
        await context.SaveChangesAsync();

        var handler = new AddTeamMemberCommandHandler(context);
        var command = new AddTeamMemberCommand(team.Id, employee.Id, employee.Id);

        // Act & Assert
        var act = async () => await handler.Handle(command, CancellationToken.None);
        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("*User must have RoleType 'Team'*");
    }

    [Fact]
    public async Task AddTeamMember_WhenTeamAtCapacity_ShouldThrowDomainException()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var team = new Team { TeamName = "Kapasitesi Dolu Ekip", Status = TeamStatus.Idle };

        for (int i = 0; i < TeamConstants.MaxOperationalCapacity; i++)
        {
            var existingUser = new User { Email = $"member_{i}@socar.com", RoleType = RoleType.Team };
            context.Users.Add(existingUser);
            team.Members.Add(new TeamMember { UserId = existingUser.Id });
        }

        var candidate = new User { Email = "candidate@socar.com", RoleType = RoleType.Team };
        context.Users.Add(candidate);
        context.Teams.Add(team);
        await context.SaveChangesAsync();

        var handler = new AddTeamMemberCommandHandler(context);
        var command = new AddTeamMemberCommand(team.Id, candidate.Id, candidate.Id);

        // Act & Assert
        var act = async () => await handler.Handle(command, CancellationToken.None);
        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("*maximum operational capacity*");
    }

    [Fact]
    public async Task RemoveTeamMember_SelfLeave_WhenValid_ShouldSucceed()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var member = new User { Email = "self_leave@socar.com", RoleType = RoleType.Team };
        context.Users.Add(member);

        var team = new Team { TeamName = "Ayrılınacak Ekip", Status = TeamStatus.Idle };
        team.Members.Add(new TeamMember { UserId = member.Id });
        context.Teams.Add(team);
        await context.SaveChangesAsync();

        var handler = new RemoveTeamMemberCommandHandler(context);
        var command = new RemoveTeamMemberCommand(team.Id, member.Id, member.Id);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Data.Members.Should().BeEmpty();
    }

    [Fact]
    public async Task RemoveTeamMember_SelfLeave_WhenTeamHasActiveAssignment_ShouldThrowDomainException()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var member = new User { Email = "active_leave@socar.com", RoleType = RoleType.Team };
        var reporter = new User { Email = "reporter_test@socar.com", RoleType = RoleType.Employee };
        var op = new User { Email = "op_test@socar.com", RoleType = RoleType.Operator };
        context.Users.AddRange(member, reporter, op);

        var team = new Team { TeamName = "Görevli Ekip", Status = TeamStatus.Forwarded };
        team.Members.Add(new TeamMember { UserId = member.Id });
        context.Teams.Add(team);

        var incident = new Incident
        {
            ReporterId = reporter.Id,
            Category = "Fire",
            EmergencyCode = "Kırmızı Kod",
            Latitude = 40.0m,
            Longitude = 29.0m,
            Location = new NetTopologySuite.Geometries.Point(29.0, 40.0) { SRID = 4326 },
            Status = IncidentStatus.Assigned
        };
        context.Incidents.Add(incident);

        var assignment = new Assignment
        {
            TeamId = team.Id,
            Incident = incident,
            OperatorId = op.Id,
            CompletedAt = null
        };
        context.Assignments.Add(assignment);
        await context.SaveChangesAsync();

        var handler = new RemoveTeamMemberCommandHandler(context);
        var command = new RemoveTeamMemberCommand(team.Id, member.Id, member.Id);

        // Act & Assert
        var act = async () => await handler.Handle(command, CancellationToken.None);
        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("*Cannot remove member while the team is involved in an active emergency response*");
    }

    [Fact]
    public async Task RemoveTeamMember_LeaderRemovesMember_WhenValid_ShouldSucceed()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var leader = new User { Email = "leader_rm@socar.com", RoleType = RoleType.Team };
        var member = new User { Email = "member_rm@socar.com", RoleType = RoleType.Team };
        context.Users.AddRange(leader, member);

        var team = new Team { TeamName = "Lider Ekip", LeaderId = leader.Id, Status = TeamStatus.Idle };
        team.Members.Add(new TeamMember { UserId = leader.Id });
        team.Members.Add(new TeamMember { UserId = member.Id });
        context.Teams.Add(team);
        await context.SaveChangesAsync();

        var handler = new RemoveTeamMemberCommandHandler(context);
        var command = new RemoveTeamMemberCommand(team.Id, leader.Id, member.Id);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Data.Members.Should().ContainSingle(m => m.UserId == leader.Id);
    }

    [Fact]
    public async Task RemoveTeamMember_NonLeaderCannotRemoveOtherMember_ShouldThrowForbiddenAccessException()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var member1 = new User { Email = "member1@socar.com", RoleType = RoleType.Team };
        var member2 = new User { Email = "member2@socar.com", RoleType = RoleType.Team };
        context.Users.AddRange(member1, member2);

        var team = new Team { TeamName = "Ekip", LeaderId = Guid.NewGuid(), Status = TeamStatus.Idle };
        team.Members.Add(new TeamMember { UserId = member1.Id });
        team.Members.Add(new TeamMember { UserId = member2.Id });
        context.Teams.Add(team);
        await context.SaveChangesAsync();

        var handler = new RemoveTeamMemberCommandHandler(context);
        var command = new RemoveTeamMemberCommand(team.Id, member1.Id, member2.Id);

        // Act & Assert
        var act = async () => await handler.Handle(command, CancellationToken.None);
        await act.Should().ThrowAsync<ForbiddenAccessException>()
            .WithMessage("*Only team leader, operator, or the member themselves can remove this team member*");
    }

    [Fact]
    public async Task UpdateTeam_VacantLeadership_ActiveMemberCanClaimLeadership_ShouldSucceed()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var member = new User { Email = "successor@socar.com", RoleType = RoleType.Team };
        context.Users.Add(member);

        var team = new Team { TeamName = "Lidersiz Ekip", LeaderId = null, Status = TeamStatus.Idle };
        team.Members.Add(new TeamMember { UserId = member.Id });
        context.Teams.Add(team);
        await context.SaveChangesAsync();

        var handler = new UpdateTeamCommandHandler(context);
        var command = new UpdateTeamCommand(team.Id, member.Id, team.TeamName, member.Id);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Data.LeaderId.Should().Be(member.Id);
    }

    [Fact]
    public async Task UpdateTeam_VacantLeadership_NonMemberCannotClaimLeadership_ShouldThrowForbiddenAccessException()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var nonMember = new User { Email = "outsider@socar.com", RoleType = RoleType.Team };
        context.Users.Add(nonMember);

        var team = new Team { TeamName = "Lidersiz Ekip 2", LeaderId = null, Status = TeamStatus.Idle };
        context.Teams.Add(team);
        await context.SaveChangesAsync();

        var handler = new UpdateTeamCommandHandler(context);
        var command = new UpdateTeamCommand(team.Id, nonMember.Id, team.TeamName, nonMember.Id);

        // Act & Assert
        var act = async () => await handler.Handle(command, CancellationToken.None);
        await act.Should().ThrowAsync<ForbiddenAccessException>()
            .WithMessage("*Only an active member of this team can claim vacant leadership*");
    }

    [Fact]
    public async Task UpdateTeam_VacantLeadership_MemberCannotAssignAnotherUserAsLeader_ShouldThrowForbiddenAccessException()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var member1 = new User { Email = "m1@socar.com", RoleType = RoleType.Team };
        var member2 = new User { Email = "m2@socar.com", RoleType = RoleType.Team };
        context.Users.AddRange(member1, member2);

        var team = new Team { TeamName = "Lidersiz Ekip 3", LeaderId = null, Status = TeamStatus.Idle };
        team.Members.Add(new TeamMember { UserId = member1.Id });
        team.Members.Add(new TeamMember { UserId = member2.Id });
        context.Teams.Add(team);
        await context.SaveChangesAsync();

        var handler = new UpdateTeamCommandHandler(context);
        // member1 tries to appoint member2 as leader
        var command = new UpdateTeamCommand(team.Id, member1.Id, team.TeamName, member2.Id);

        // Act & Assert
        var act = async () => await handler.Handle(command, CancellationToken.None);
        await act.Should().ThrowAsync<ForbiddenAccessException>()
            .WithMessage("*Only an active member of this team can claim vacant leadership for themselves*");
    }

    [Fact]
    public async Task UpdateTeam_WhenLeaderAssigned_NonLeaderMemberCannotUpdate_ShouldThrowForbiddenAccessException()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var currentLeader = new User { Email = "current_leader@socar.com", RoleType = RoleType.Team };
        var otherMember = new User { Email = "other_member@socar.com", RoleType = RoleType.Team };
        context.Users.AddRange(currentLeader, otherMember);

        var team = new Team { TeamName = "Liderli Ekip", LeaderId = currentLeader.Id, Status = TeamStatus.Idle };
        team.Members.Add(new TeamMember { UserId = currentLeader.Id });
        team.Members.Add(new TeamMember { UserId = otherMember.Id });
        context.Teams.Add(team);
        await context.SaveChangesAsync();

        var handler = new UpdateTeamCommandHandler(context);
        var command = new UpdateTeamCommand(team.Id, otherMember.Id, "Yeni Ekip Adı", otherMember.Id);

        // Act & Assert
        var act = async () => await handler.Handle(command, CancellationToken.None);
        await act.Should().ThrowAsync<ForbiddenAccessException>()
            .WithMessage("*Only the team leader or operator can update it*");
    }
}
