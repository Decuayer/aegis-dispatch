using Microsoft.AspNetCore.SignalR.Client;
using SocarDispatch.Web.Events;

namespace SocarDispatch.Web.Services.SignalR;

public interface ILocationHubClient : IAsyncDisposable
{
    /// <summary>
    /// Hub bağlantısının anlık durumu (Connected, Reconnecting, Disconnected vb.)
    /// </summary>
    HubConnectionState ConnectionState { get; }

    /// <summary>
    /// Saha ekibinden yeni bir GPS konumu geldiğinde tetiklenir.
    /// </summary>
    event EventHandler<TeamLocationUpdatedEventArgs>? OnTeamLocationUpdated;

    /// <summary>
    /// WebSocket bağlantı durumu değiştiğinde (kopma, yeniden bağlanma vb.) tetiklenir.
    /// </summary>
    event EventHandler<HubConnectionState>? OnConnectionStateChanged;

    /// <summary>
    /// Hub bağlantısını başlatır.
    /// </summary>
    Task StartAsync();

    /// <summary>
    /// Hub bağlantısını sonlandırır.
    /// </summary>
    Task StopAsync();
}
