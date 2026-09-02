using FluentAssertions;
using NetTopologySuite.Geometries;
using SocarDispatch.Domain.Entities;
using SocarDispatch.Domain.Enums;
using Xunit;

namespace SocarDispatch.UnitTests.Domain;

public class IncidentLifecycleTests
{
    [Fact]
    public void Incident_OnCreation_ShouldHaveDefaultOpenStatusAndValidSpatialPoint()
    {
        // Arrange & Act
        var incident = new Incident
        {
            Category = "Fire",
            EmergencyCode = "RED_ALERT",
            Latitude = 38.7915m,
            Longitude = 26.9212m,
            Location = new Point(26.9212, 38.7915) { SRID = 4326 }
        };

        // Assert
        incident.Status.Should().Be(IncidentStatus.Open);
        incident.IsDeleted.Should().BeFalse();
        incident.DeletedAt.Should().BeNull();
        incident.Location.Should().NotBeNull();
        incident.Location!.SRID.Should().Be(4326);
        incident.Location.X.Should().Be(26.9212);
        incident.Location.Y.Should().Be(38.7915);
    }

    [Theory]
    [InlineData(IncidentStatus.Open, IncidentStatus.Assigned)]
    [InlineData(IncidentStatus.Assigned, IncidentStatus.Resolved)]
    [InlineData(IncidentStatus.Open, IncidentStatus.Canceled)]
    public void IncidentStatus_ValidTransitions_ShouldUpdateCorrectly(IncidentStatus initial, IncidentStatus next)
    {
        var incident = new Incident { Status = initial };
        incident.Status = next;
        incident.Status.Should().Be(next);
    }

    [Fact]
    public void Incident_SoftDelete_ShouldSetIsDeletedAndTimestamp()
    {
        var incident = new Incident();
        var deletedTime = DateTime.UtcNow;

        incident.IsDeleted = true;
        incident.DeletedAt = deletedTime;

        incident.IsDeleted.Should().BeTrue();
        incident.DeletedAt.Should().Be(deletedTime);
    }
}
