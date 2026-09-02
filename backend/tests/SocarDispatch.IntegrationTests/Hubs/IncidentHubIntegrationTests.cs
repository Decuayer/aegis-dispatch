using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.SignalR.Client;
using NetTopologySuite.Geometries;
using SocarDispatch.Domain.Entities;
using SocarDispatch.Domain.Enums;
using SocarDispatch.IntegrationTests.Common;
using SocarDispatch.IntegrationTests.Fixtures;
using Xunit;

namespace SocarDispatch.IntegrationTests.Hubs;

public class IncidentHubIntegrationTests : IntegrationTestBase
{
    public IncidentHubIntegrationTests(CustomWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task IncidentHub_OperatorClient_CanConnectAndReceiveStatusBroadcast()
    {
        // Arrange
        var reporter = await SeedUserAsync("Test", "Reporter", role: RoleType.Employee);
        var operatorUser = await SeedUserAsync("Operator", "User", role: RoleType.Operator);

        var operatorConnection = Factory.CreateHubConnection("/hubs/incidents", RoleType.Operator, userId: operatorUser.Id);
        var receivedPayloads = new List<JsonElement>();
        var tcs = new TaskCompletionSource<bool>();

        operatorConnection.On<JsonElement>("ReceiveIncidentStatusChanged", payload =>
        {
            receivedPayloads.Add(payload);
            tcs.TrySetResult(true);
        });

        // Act
        await operatorConnection.StartAsync();
        operatorConnection.State.Should().Be(HubConnectionState.Connected);

        var incidentId = Guid.NewGuid();
        await ExecuteDbContextAsync(async ctx =>
        {
            ctx.Incidents.Add(new Incident
            {
                Id = incidentId,
                ReporterId = reporter.Id,
                Category = "Fire",
                EmergencyCode = "RED_ALERT",
                Status = IncidentStatus.Open,
                Latitude = 38.7915m,
                Longitude = 26.9212m,
                Location = new Point(26.9212, 38.7915) { SRID = 4326 },
                CreatedAt = DateTime.UtcNow
            });
            await ctx.SaveChangesAsync();
        });

        var client = Factory.CreateAuthenticatedClient(operatorUser);
        var patchResponse = await client.PatchAsync(
            $"/api/v1/incidents/{incidentId}/status",
            JsonContent.Create(new { status = "Resolved", completionNotes = "Extinguished by response unit." })
        );
        patchResponse.EnsureSuccessStatusCode();

        var received = await Task.WhenAny(tcs.Task, Task.Delay(5000)) == tcs.Task;

        // Cleanup
        await operatorConnection.StopAsync();
        await operatorConnection.DisposeAsync();

        // Assert
        received.Should().BeTrue("Operator must receive real-time IncidentStatusChanged broadcast.");
        receivedPayloads.Should().NotBeEmpty();
    }
}
