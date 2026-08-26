using System.Text.Json.Serialization;
using Microsoft.AspNetCore.SignalR.Client;
using SocarDispatch.Web.Auth;
using SocarDispatch.Web.Events;
using SocarDispatch.Web.Utils; 

namespace SocarDispatch.Web.Services.SignalR;

public class LocationHubClient : ILocationHubClient
{
    private HubConnection? _hubConnection;
    private readonly IAuthService _authService;
    private readonly IConfiguration _config;
    private readonly ILogger<LocationHubClient> _logger;

    public event EventHandler<TeamLocationUpdatedEventArgs>? OnTeamLocationUpdated;
    public event EventHandler<HubConnectionState>? OnConnectionStateChanged;

    public HubConnectionState ConnectionState => _hubConnection?.State ?? HubConnectionState.Disconnected;

    public LocationHubClient(
        IAuthService _authService,
        IConfiguration config,
        ILogger<LocationHubClient> logger)
    {
        this._authService = _authService;
        _config = config;
        _logger = logger;
    }

    public async Task StartAsync()
    {
        if (_hubConnection != null && _hubConnection.State != HubConnectionState.Disconnected)
        {
            return;
        }

        var baseUrl = _config["ApiBaseUrl"]?.TrimEnd('/') ?? "http://localhost:5233";
        var hubUrl = $"{baseUrl}/hubs/location";

        _hubConnection = new HubConnectionBuilder()
            .WithUrl(hubUrl, options =>
            {
                options.AccessTokenProvider = async () =>
                {
                    var token = await _authService.GetTokenAsync();
                    if (string.IsNullOrWhiteSpace(token) || JwtTokenParser.IsTokenExpired(token))
                    {
                        return null; // Return null to prevent unauthorized reconnect loops
                    }
                    return token;
                };
            })
            .WithAutomaticReconnect(new[]
            {
                TimeSpan.Zero,                
                TimeSpan.FromSeconds(2),
                TimeSpan.FromSeconds(5),
                TimeSpan.FromSeconds(10),
                TimeSpan.FromSeconds(30)
            })
            .Build();

        // 1. 'TeamLocationUpdated' event listener from the backend
        _hubConnection.On<TeamLocationPayload>("TeamLocationUpdated", payload =>
        {
            _logger.LogDebug("[LocationHub] Team location received for TeamId: {TeamId}", payload.TeamId);

            OnTeamLocationUpdated?.Invoke(this, new TeamLocationUpdatedEventArgs
            {
                TeamId = payload.TeamId,
                Latitude = payload.Lat,
                Longitude = payload.Lng,
                UpdatedAt = payload.UpdatedAt ?? payload.Timestamp ?? DateTime.UtcNow
            });
        });

        // 2. Connection lifecycle events
        _hubConnection.Reconnecting += ex =>
        {
            _logger.LogWarning("[LocationHub] Connection lost. Reconnecting... Reason: {Message}", ex?.Message);
            OnConnectionStateChanged?.Invoke(this, HubConnectionState.Reconnecting);
            return Task.CompletedTask;
        };

        _hubConnection.Reconnected += connectionId =>
        {
            _logger.LogInformation("[LocationHub] Reconnected successfully. ConnectionId: {ConnectionId}", connectionId);
            OnConnectionStateChanged?.Invoke(this, HubConnectionState.Connected);
            return Task.CompletedTask;
        };

        _hubConnection.Closed += ex =>
        {
            _logger.LogError("[LocationHub] Connection closed permanently. Error: {Message}", ex?.Message);
            OnConnectionStateChanged?.Invoke(this, HubConnectionState.Disconnected);
            return Task.CompletedTask;
        };

        try
        {
            await _hubConnection.StartAsync();
            _logger.LogInformation("[LocationHub] Connected successfully to {HubUrl}", hubUrl);
            OnConnectionStateChanged?.Invoke(this, HubConnectionState.Connected);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[LocationHub] Failed to connect to {HubUrl}", hubUrl);
            OnConnectionStateChanged?.Invoke(this, HubConnectionState.Disconnected);
        }
    }

    public async Task StopAsync()
    {
        if (_hubConnection != null)
        {
            await _hubConnection.StopAsync();
            OnConnectionStateChanged?.Invoke(this, HubConnectionState.Disconnected);
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_hubConnection != null)
        {
            await _hubConnection.DisposeAsync();
            _hubConnection = null;
        }
        GC.SuppressFinalize(this);
    }

    // Internal DTO for backend payload flexibility
    private record TeamLocationPayload(
        [property: JsonPropertyName("teamId")] Guid TeamId,
        [property: JsonPropertyName("lat")] double Lat,
        [property: JsonPropertyName("lng")] double Lng,
        [property: JsonPropertyName("updatedAt")] DateTime? UpdatedAt,
        [property: JsonPropertyName("timestamp")] DateTime? Timestamp
    );
}
