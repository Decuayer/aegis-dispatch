using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.SignalR.Client;
using SocarDispatch.Domain.Enums;
using SocarDispatch.IntegrationTests.Common;
using SocarDispatch.IntegrationTests.Fixtures;
using Xunit;

namespace SocarDispatch.IntegrationTests.Hubs;

public class LocationHubIntegrationTests : IntegrationTestBase
{
    public LocationHubIntegrationTests(CustomWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task LocationHub_StreamTeamLocation_BroadcastsAndRespectsThrottling()
    {
        // Arrange
        var senderConnection = Factory.CreateHubConnection("/hubs/location", RoleType.Team);
        var listenerConnection = Factory.CreateHubConnection("/hubs/location", RoleType.Operator);

        var receivedList = new List<JsonElement>();
        var tcs = new TaskCompletionSource<bool>();

        listenerConnection.On<JsonElement>("ReceiveTeamLocationUpdated", payload =>
        {
            receivedList.Add(payload);
            tcs.TrySetResult(true);
        });

        await senderConnection.StartAsync();
        await listenerConnection.StartAsync();

        var teamId = Guid.NewGuid();

        // Act 1: Initial location streaming
        await senderConnection.InvokeAsync("StreamTeamLocation", teamId, 38.7915, 26.9212);
        await Task.WhenAny(tcs.Task, Task.Delay(3000));

        // Act 2: Immediate consecutive location streaming within 1000ms window (must be throttled)
        await senderConnection.InvokeAsync("StreamTeamLocation", teamId, 38.7920, 26.9220);
        await Task.Delay(500);

        // Cleanup
        await senderConnection.StopAsync();
        await listenerConnection.StopAsync();
        await senderConnection.DisposeAsync();
        await listenerConnection.DisposeAsync();

        // Assert: Throttling allows only 1 broadcast within the 1-second window
        receivedList.Count.Should().Be(1);
    }
}
