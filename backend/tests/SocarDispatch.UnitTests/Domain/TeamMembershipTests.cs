using FluentAssertions;
using SocarDispatch.Domain.Entities;
using SocarDispatch.Domain.Enums;
using Xunit;

namespace SocarDispatch.UnitTests.Domain;

public class TeamMembershipTests
{
    [Fact]
    public void Team_OnCreation_ShouldDefaultToIdleStatus()
    {
        var team = new Team
        {
            TeamName = "Star Alpha Unit"
        };

        team.Status.Should().Be(TeamStatus.Idle);
        team.Members.Should().BeEmpty();
        team.Assignments.Should().BeEmpty();
    }

    [Fact]
    public void TeamMember_OnCreation_ShouldDefaultToAvailable()
    {
        var member = new TeamMember
        {
            TeamId = Guid.NewGuid(),
            UserId = Guid.NewGuid()
        };

        member.MemberStatus.Should().Be(TeamMemberStatus.Available);
        member.JoinedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
    }

    [Fact]
    public void TeamMemberStatus_Transitions_ShouldBePreserved()
    {
        var member = new TeamMember();

        member.MemberStatus = TeamMemberStatus.EnRoute;
        member.MemberStatus.Should().Be(TeamMemberStatus.EnRoute);

        member.MemberStatus = TeamMemberStatus.OnScene;
        member.MemberStatus.Should().Be(TeamMemberStatus.OnScene);

        member.MemberStatus = TeamMemberStatus.Unavailable;
        member.MemberStatus.Should().Be(TeamMemberStatus.Unavailable);
    }
}
