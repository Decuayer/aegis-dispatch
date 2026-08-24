using Microsoft.AspNetCore.SignalR.Client;
using SocarDispatch.Web.Events;

namespace SocarDispatch.Web.Services.SignalR;

public interface IIncidentHubClient : IAsyncDisposable
{
    HubConnectionState ConnectionState { get; }

    event EventHandler<NewIncidentReceivedEventArgs>? OnNewIncidentReceived;
    event EventHandler<IncidentStatusChangedEventArgs>? OnIncidentStatusChanged;
    event EventHandler<TeamDispatchedEventArgs>? OnTeamDispatched;
    event EventHandler<HubConnectionState>? OnConnectionStateChanged;

    Task StartAsync();
    Task StopAsync();
}
