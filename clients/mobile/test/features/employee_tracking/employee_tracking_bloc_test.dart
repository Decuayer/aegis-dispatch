import 'dart:async';
import 'package:flutter_test/flutter_test.dart';
import 'package:latlong2/latlong.dart';
import 'package:socar_dispatch_mobile/core/network/api_client.dart';
import 'package:socar_dispatch_mobile/core/storage/secure_storage_service.dart';
import 'package:socar_dispatch_mobile/features/employee_tracking/data/models/tracked_incident_model.dart';
import 'package:socar_dispatch_mobile/features/employee_tracking/data/repositories/employee_incident_repository.dart';
import 'package:socar_dispatch_mobile/features/employee_tracking/data/services/employee_tracking_hub_service.dart';
import 'package:socar_dispatch_mobile/features/employee_tracking/presentation/bloc/employee_tracking_bloc.dart';
import 'package:socar_dispatch_mobile/features/employee_tracking/presentation/bloc/employee_tracking_event.dart';
import 'package:socar_dispatch_mobile/features/employee_tracking/presentation/bloc/employee_tracking_state.dart';
import 'package:socar_dispatch_mobile/features/team_tasks/services/route_service.dart';

class FakeSecureStorage extends SecureStorageService {}

class FakeEmployeeIncidentRepository extends EmployeeIncidentRepository {
  FakeEmployeeIncidentRepository()
      : super(apiClient: ApiClient(storageService: FakeSecureStorage()));

  List<TrackedIncidentModel> mockIncidents = [];
  TrackedIncidentModel? mockDetailIncident;

  @override
  Future<List<TrackedIncidentModel>> getMyReportedIncidents({
    required String reporterId,
    String? status,
  }) async =>
      mockIncidents;

  @override
  Future<TrackedIncidentModel> getIncidentDetails(String incidentId) async =>
      mockDetailIncident!;
}

class FakeRouteService extends RouteService {
  @override
  Future<RouteResult> calculateRoute({
    required LatLng origin,
    required LatLng destination,
  }) async {
    return const RouteResult(
      points: [LatLng(38.79, 26.92), LatLng(38.80, 26.93)],
      distanceKm: 1.5,
      durationMinutes: 4,
      isFallback: false,
    );
  }
}

class FakeEmployeeTrackingHubService extends EmployeeTrackingHubService {
  FakeEmployeeTrackingHubService()
      : super(storageService: FakeSecureStorage());

  final _statusController = StreamController<IncidentStatusUpdate>.broadcast();
  final _dispatchController = StreamController<TeamDispatchedUpdate>.broadcast();
  final _locationController = StreamController<TeamLocationUpdate>.broadcast();

  @override
  Stream<IncidentStatusUpdate> get onIncidentStatusChanged => _statusController.stream;
  @override
  Stream<TeamDispatchedUpdate> get onTeamDispatched => _dispatchController.stream;
  @override
  Stream<TeamLocationUpdate> get onTeamLocationUpdated => _locationController.stream;

  void emitStatusChange(IncidentStatusUpdate update) => _statusController.add(update);
  void emitLocationUpdate(TeamLocationUpdate update) => _locationController.add(update);
}

void main() {
  group('EmployeeTrackingBloc Unit Tests', () {
    late FakeEmployeeIncidentRepository repository;
    late FakeRouteService routeService;
    late FakeEmployeeTrackingHubService hubService;
    late EmployeeTrackingBloc bloc;

    setUp(() {
      repository = FakeEmployeeIncidentRepository();
      routeService = FakeRouteService();
      hubService = FakeEmployeeTrackingHubService();
      bloc = EmployeeTrackingBloc(
        repository: repository,
        routeService: routeService,
        hubService: hubService,
      );
    });

    tearDown(() {
      bloc.close();
    });

    test('emits EmployeeTrackingLoaded when LoadMyIncidents succeeds', () async {
      repository.mockIncidents = [
        TrackedIncidentModel(
          id: 'inc-1',
          reporterId: 'user-1',
          reporterFullName: 'Test Employee',
          category: 'Fire',
          emergencyCode: 'Code Red',
          status: 'Open',
          latitude: 38.795,
          longitude: 26.925,
          createdAt: DateTime.now(),
        ),
      ];

      expectLater(
        bloc.stream,
        emitsInOrder([
          isA<EmployeeTrackingLoading>(),
          isA<EmployeeTrackingLoaded>().having(
            (s) => s.incidents.length,
            'incidents length',
            1,
          ),
        ]),
      );

      bloc.add(const LoadMyIncidents('user-1'));
    });

    test('updates team coordinates and calculates route when TeamLocationReceived', () async {
      final incident = TrackedIncidentModel(
        id: 'inc-1',
        reporterId: 'user-1',
        reporterFullName: 'Test Employee',
        category: 'Fire',
        emergencyCode: 'Code Red',
        status: 'Assigned',
        latitude: 38.795,
        longitude: 26.925,
        assignedTeamId: 'team-42',
        assignedTeamName: 'Rescue Unit 1',
        createdAt: DateTime.now(),
      );

      repository.mockDetailIncident = incident;

      bloc.emit(EmployeeTrackingLoaded(
        incidents: [incident],
        selectedIncident: incident,
      ));

      expectLater(
        bloc.stream,
        emits(
          isA<EmployeeTrackingLoaded>()
              .having((s) => s.distanceKm, 'distanceKm', 1.5)
              .having((s) => s.etaMinutes, 'etaMinutes', 4)
              .having((s) => s.activeRoute.length, 'activeRoute', 2),
        ),
      );

      bloc.add(const TeamLocationReceived(
        teamId: 'team-42',
        latitude: 38.79,
        longitude: 26.92,
      ));
    });

    test('disables editing when incident transitions to Resolved', () async {
      final incident = TrackedIncidentModel(
        id: 'inc-1',
        reporterId: 'user-1',
        reporterFullName: 'Test Employee',
        category: 'Fire',
        emergencyCode: 'Code Red',
        status: 'Open',
        latitude: 38.795,
        longitude: 26.925,
        createdAt: DateTime.now(),
      );

      bloc.emit(EmployeeTrackingLoaded(
        incidents: [incident],
        selectedIncident: incident,
      ));

      expect(incident.isEditable, isTrue);

      bloc.add(const IncidentStatusReceived(
        incidentId: 'inc-1',
        status: 'Resolved',
      ));

      await expectLater(
        bloc.stream,
        emits(
          isA<EmployeeTrackingLoaded>().having(
            (s) => s.selectedIncident?.isEditable,
            'isEditable is false',
            isFalse,
          ),
        ),
      );
    });
  });
}
