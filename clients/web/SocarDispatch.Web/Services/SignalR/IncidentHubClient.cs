using System.Text.Json.Serialization;
using Microsoft.AspNetCore.SignalR.Client;
using SocarDispatch.Web.Auth;
using SocarDispatch.Web.Events;
using SocarDispatch.Web.Utils;

namespace SocarDispatch.Web.Services.SignalR;

public class IncidentHubClient : IIncidentHubClient
{
    private HubConnection? _hubConnection;
    private readonly IAuthService _authService;
    private readonly IConfiguration _config;
    private readonly ILogger<IncidentHubClient> _logger;

    public event EventHandler<NewIncidentReceivedEventArgs>? OnNewIncidentReceived;
    public event EventHandler<IncidentStatusChangedEventArgs>? OnIncidentStatusChanged;
    public event EventHandler<IncidentUpdatedEventArgs>? OnIncidentUpdated;
    public event EventHandler<TeamDispatchedEventArgs>? OnTeamDispatched;
    public event EventHandler<HubConnectionState>? OnConnectionStateChanged;
    public event EventHandler<MemberStatusChangedEventArgs>? OnMemberStatusChanged;


    public HubConnectionState ConnectionState => _hubConnection?.State ?? HubConnectionState.Disconnected;

    public IncidentHubClient(
        IAuthService authService,
        IConfiguration config,
        ILogger<IncidentHubClient> logger)
    {
        _authService = authService;
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
        var hubUrl = $"{baseUrl}/hubs/incidents";

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

        // 1. New Incident Event
        _hubConnection.On<NewIncidentPayload>("NewIncident", payload =>
        {
            _logger.LogInformation("[IncidentHub] New incident received: {IncidentId}", payload.Id);
            OnNewIncidentReceived?.Invoke(this, new NewIncidentReceivedEventArgs
            {
                Id = payload.Id,
                Category = payload.Category,
                EmergencyCode = payload.EmergencyCode,
                ReporterFullName = payload.ReporterFullName,
                Description = payload.Description,
                Latitude = payload.Latitude,
                Longitude = payload.Longitude,
                CreatedAt = payload.CreatedAt
            });
        });

        // 2. Incident Status Change Event
        _hubConnection.On<IncidentStatusChangedPayload>("IncidentStatusChanged", payload =>
        {
            _logger.LogInformation("[IncidentHub] Status changed for {IncidentId}: {Status}", payload.IncidentId, payload.Status);
            OnIncidentStatusChanged?.Invoke(this, new IncidentStatusChangedEventArgs
            {
                IncidentId = payload.IncidentId,
                PreviousStatus = payload.PreviousStatus,
                Status = payload.Status,
                ChangedById = payload.ChangedById,
                ChangedAt = payload.ChangedAt
            });
        });

        // 3. Incident Core Update Event
        _hubConnection.On<IncidentUpdatedPayload>("IncidentUpdated", payload =>
        {
            _logger.LogInformation("[IncidentHub] Incident updated: {IncidentId}", payload.IncidentId);
            OnIncidentUpdated?.Invoke(this, new IncidentUpdatedEventArgs
            {
                IncidentId = payload.IncidentId,
                Category = payload.Category,
                EmergencyCode = payload.EmergencyCode,
                Description = payload.Description,
                Latitude = payload.Latitude,
                Longitude = payload.Longitude,
                UpdatedById = payload.UpdatedById,
                UpdatedAt = payload.UpdatedAt
            });
        });

        // 4. Team Dispatched Event
        _hubConnection.On<TeamDispatchedPayload>("TeamDispatched", payload =>
        {
            _logger.LogInformation("[IncidentHub] Team dispatched: {TeamId} to {IncidentId}", payload.TeamId, payload.IncidentId);
            OnTeamDispatched?.Invoke(this, new TeamDispatchedEventArgs
            {
                AssignmentId = payload.AssignmentId,
                IncidentId = payload.IncidentId,
                TeamId = payload.TeamId,
                OperatorId = payload.OperatorId,
                AssignedAt = payload.AssignedAt
            });
        });

        _hubConnection.On<MemberStatusChangedPayload>("MemberStatusChanged", payload =>
        {
            _logger.LogInformation("[IncidentHub] Member status changed: Team {TeamId}, User {UserId} -> {NewStatus}", 
                payload.TeamId, payload.UserId, payload.NewStatus);
            OnMemberStatusChanged?.Invoke(this, new MemberStatusChangedEventArgs
            {
                TeamId = payload.TeamId,
                UserId = payload.UserId,
                PreviousStatus = payload.PreviousStatus,
                NewStatus = payload.NewStatus,
                ChangedById = payload.ChangedById,
                ChangedAt = payload.ChangedAt
            });
        });

        // Reconnect & Lifecycle
        _hubConnection.Reconnecting += ex =>
        {
            _logger.LogWarning("[IncidentHub] Connection lost. Reconnecting... Reason: {Message}", ex?.Message);
            OnConnectionStateChanged?.Invoke(this, HubConnectionState.Reconnecting);
            return Task.CompletedTask;
        };

        _hubConnection.Reconnected += connectionId =>
        {
            _logger.LogInformation("[IncidentHub] Reconnected successfully. ConnectionId: {ConnectionId}", connectionId);
            OnConnectionStateChanged?.Invoke(this, HubConnectionState.Connected);
            return Task.CompletedTask;
        };

        _hubConnection.Closed += ex =>
        {
            _logger.LogError("[IncidentHub] Connection closed. Error: {Message}", ex?.Message);
            OnConnectionStateChanged?.Invoke(this, HubConnectionState.Disconnected);
            return Task.CompletedTask;
        };

        try
        {
            await _hubConnection.StartAsync();
            _logger.LogInformation("[IncidentHub] Connected to {HubUrl}", hubUrl);
            OnConnectionStateChanged?.Invoke(this, HubConnectionState.Connected);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[IncidentHub] Failed to connect to {HubUrl}", hubUrl);
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

    // JSON Deserialization DTOs
    private record NewIncidentPayload(
        [property: JsonPropertyName("id")] Guid Id,
        [property: JsonPropertyName("category")] string Category,
        [property: JsonPropertyName("emergencyCode")] string EmergencyCode,
        [property: JsonPropertyName("reporterFullName")] string? ReporterFullName,
        [property: JsonPropertyName("description")] string? Description,
        [property: JsonPropertyName("latitude")] double Latitude,
        [property: JsonPropertyName("longitude")] double Longitude,
        [property: JsonPropertyName("createdAt")] DateTime CreatedAt
    );

    private record IncidentStatusChangedPayload(
        [property: JsonPropertyName("incidentId")] Guid IncidentId,
        [property: JsonPropertyName("previousStatus")] string PreviousStatus,
        [property: JsonPropertyName("status")] string Status,
        [property: JsonPropertyName("changedById")] Guid ChangedById,
        [property: JsonPropertyName("changedAt")] DateTime ChangedAt
    );

    private record IncidentUpdatedPayload(
        [property: JsonPropertyName("incidentId")] Guid IncidentId,
        [property: JsonPropertyName("category")] string Category,
        [property: JsonPropertyName("emergencyCode")] string EmergencyCode,
        [property: JsonPropertyName("description")] string? Description,
        [property: JsonPropertyName("latitude")] double Latitude,
        [property: JsonPropertyName("longitude")] double Longitude,
        [property: JsonPropertyName("updatedById")] Guid UpdatedById,
        [property: JsonPropertyName("updatedAt")] DateTime UpdatedAt
    );

    private record TeamDispatchedPayload(
        [property: JsonPropertyName("assignmentId")] Guid AssignmentId,
        [property: JsonPropertyName("incidentId")] Guid IncidentId,
        [property: JsonPropertyName("teamId")] Guid TeamId,
        [property: JsonPropertyName("operatorId")] Guid OperatorId,
        [property: JsonPropertyName("assignedAt")] DateTime AssignedAt
    );

    private record MemberStatusChangedPayload(
        [property: JsonPropertyName("teamId")] Guid TeamId,
        [property: JsonPropertyName("userId")] Guid UserId,
        [property: JsonPropertyName("previousStatus")] string PreviousStatus,
        [property: JsonPropertyName("newStatus")] string NewStatus,
        [property: JsonPropertyName("changedById")] Guid ChangedById,
        [property: JsonPropertyName("changedAt")] DateTime ChangedAt
    );

}
