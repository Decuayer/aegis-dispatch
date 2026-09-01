import 'dart:async';
import 'package:signalr_netcore/signalr_client.dart';
import '../../../../core/constants/api_endpoints.dart';
import '../../../../core/storage/secure_storage_service.dart';

// Event payload models
class IncidentStatusUpdate {
  final String incidentId;
  final String status;
  final String? previousStatus;
  final DateTime changedAt;

  const IncidentStatusUpdate({
    required this.incidentId,
    required this.status,
    this.previousStatus,
    required this.changedAt,
  });
}

class TeamDispatchedUpdate {
  final String incidentId;
  final String teamId;
  final String assignmentId;
  final DateTime assignedAt;

  const TeamDispatchedUpdate({
    required this.incidentId,
    required this.teamId,
    required this.assignmentId,
    required this.assignedAt,
  });
}

class IncidentUpdatedData {
  final String incidentId;
  final String? description;
  final String? emergencyCode;
  final DateTime updatedAt;

  const IncidentUpdatedData({
    required this.incidentId,
    this.description,
    this.emergencyCode,
    required this.updatedAt,
  });
}

class TeamLocationUpdate {
  final String teamId;
  final double latitude;
  final double longitude;
  final DateTime timestamp;

  const TeamLocationUpdate({
    required this.teamId,
    required this.latitude,
    required this.longitude,
    required this.timestamp,
  });
}

class EmployeeTrackingHubService {
  final SecureStorageService _storageService;
  HubConnection? _incidentsHub;
  HubConnection? _locationHub;

  final _incidentStatusController = StreamController<IncidentStatusUpdate>.broadcast();
  final _teamDispatchedController = StreamController<TeamDispatchedUpdate>.broadcast();
  final _incidentUpdatedController = StreamController<IncidentUpdatedData>.broadcast();
  final _teamLocationController = StreamController<TeamLocationUpdate>.broadcast();

  Stream<IncidentStatusUpdate> get onIncidentStatusChanged => _incidentStatusController.stream;
  Stream<TeamDispatchedUpdate> get onTeamDispatched => _teamDispatchedController.stream;
  Stream<IncidentUpdatedData> get onIncidentUpdated => _incidentUpdatedController.stream;
  Stream<TeamLocationUpdate> get onTeamLocationUpdated => _teamLocationController.stream;

  EmployeeTrackingHubService({required SecureStorageService storageService})
      : _storageService = storageService;

  Future<void> initialize() async {
    final token = await _storageService.getAccessToken();
    if (token == null || token.isEmpty) return;

    final baseUrl = ApiEndpoints.baseUrl;

    // 1. Incidents Hub Connection
    _incidentsHub = HubConnectionBuilder()
        .withUrl(
          '$baseUrl/hubs/incidents',
          options: HttpConnectionOptions(
            accessTokenFactory: () async => token,
          ),
        )
        .withAutomaticReconnect(retryDelays: [0, 2000, 5000, 10000, 30000])
        .build();

    _incidentsHub?.on('IncidentStatusChanged', _handleIncidentStatusChanged);
    _incidentsHub?.on('ReceiveIncidentStatusChanged', _handleIncidentStatusChanged);
    _incidentsHub?.on('TeamDispatched', _handleTeamDispatched);
    _incidentsHub?.on('ReceiveAssignmentCreated', _handleTeamDispatched);
    _incidentsHub?.on('IncidentUpdated', _handleIncidentUpdated);
    _incidentsHub?.on('ReceiveIncidentUpdated', _handleIncidentUpdated);

    // 2. Location Hub Connection
    _locationHub = HubConnectionBuilder()
        .withUrl(
          '$baseUrl/hubs/location',
          options: HttpConnectionOptions(
            accessTokenFactory: () async => token,
          ),
        )
        .withAutomaticReconnect(retryDelays: [0, 2000, 5000, 10000, 30000])
        .build();

    _locationHub?.on('TeamLocationUpdated', _handleTeamLocationUpdated);
    _locationHub?.on('ReceiveTeamLocationUpdated', _handleTeamLocationUpdated);

    try {
      await _incidentsHub?.start();
    } catch (_) {}

    try {
      await _locationHub?.start();
    } catch (_) {}
  }

  void _handleIncidentStatusChanged(List<dynamic>? args) {
    if (args == null || args.isEmpty) return;
    final data = args[0] as Map<String, dynamic>;
    final incidentId = (data['incidentId'] ?? data['IncidentId'] ?? '').toString();
    final status = (data['status'] ?? data['Status'] ?? '').toString();
    final prev = (data['previousStatus'] ?? data['PreviousStatus'])?.toString();

    _incidentStatusController.add(
      IncidentStatusUpdate(
        incidentId: incidentId,
        status: status,
        previousStatus: prev,
        changedAt: DateTime.now(),
      ),
    );
  }

  void _handleTeamDispatched(List<dynamic>? args) {
    if (args == null || args.isEmpty) return;
    final data = args[0] as Map<String, dynamic>;
    final incidentId = (data['incidentId'] ?? data['IncidentId'] ?? '').toString();
    final teamId = (data['teamId'] ?? data['TeamId'] ?? '').toString();
    final assignmentId = (data['assignmentId'] ?? data['AssignmentId'] ?? '').toString();

    _teamDispatchedController.add(
      TeamDispatchedUpdate(
        incidentId: incidentId,
        teamId: teamId,
        assignmentId: assignmentId,
        assignedAt: DateTime.now(),
      ),
    );
  }

  void _handleIncidentUpdated(List<dynamic>? args) {
    if (args == null || args.isEmpty) return;
    final data = args[0] as Map<String, dynamic>;
    final incidentId = (data['incidentId'] ?? data['IncidentId'] ?? '').toString();
    final desc = data['description'] as String? ?? data['Description'] as String?;
    final code = data['emergencyCode'] as String? ?? data['EmergencyCode'] as String?;

    _incidentUpdatedController.add(
      IncidentUpdatedData(
        incidentId: incidentId,
        description: desc,
        emergencyCode: code,
        updatedAt: DateTime.now(),
      ),
    );
  }

  void _handleTeamLocationUpdated(List<dynamic>? args) {
    if (args == null || args.isEmpty) return;
    final data = args[0] as Map<String, dynamic>;
    final teamId = (data['teamId'] ?? data['TeamId'] ?? '').toString();
    final lat = (data['lat'] ?? data['latitude'] ?? data['Latitude'] as num?)?.toDouble() ?? 0.0;
    final lng = (data['lng'] ?? data['longitude'] ?? data['Longitude'] as num?)?.toDouble() ?? 0.0;

    _teamLocationController.add(
      TeamLocationUpdate(
        teamId: teamId,
        latitude: lat,
        longitude: lng,
        timestamp: DateTime.now(),
      ),
    );
  }

  Future<void> dispose() async {
    try {
      await _incidentsHub?.stop();
    } catch (_) {}
    try {
      await _locationHub?.stop();
    } catch (_) {}

    await _incidentStatusController.close();
    await _teamDispatchedController.close();
    await _incidentUpdatedController.close();
    await _teamLocationController.close();
  }
}
