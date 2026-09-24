import 'package:flutter_test/flutter_test.dart';
import 'package:geolocator/geolocator.dart';
import 'package:socar_dispatch_mobile/core/network/api_client.dart';
import 'package:socar_dispatch_mobile/core/storage/secure_storage_service.dart';
import 'package:socar_dispatch_mobile/features/incident_reporting/data/models/create_incident_request_model.dart';
import 'package:socar_dispatch_mobile/features/incident_reporting/data/models/incident_response_model.dart';
import 'package:socar_dispatch_mobile/features/incident_reporting/data/models/rapid_emergency_preset.dart';
import 'package:socar_dispatch_mobile/features/incident_reporting/data/repositories/incident_repository.dart';
import 'package:socar_dispatch_mobile/features/incident_reporting/services/location_service.dart';
import 'package:socar_dispatch_mobile/features/incident_reporting/services/rapid_dispatch_service.dart';

class MockSecureStorage extends SecureStorageService {
  @override
  Future<String?> getAccessToken() async => 'mock-token';
}

class MockIncidentRepository extends IncidentRepository {
  MockIncidentRepository()
    : super(apiClient: ApiClient(storageService: MockSecureStorage()));

  CreateIncidentRequestModel? lastCreatedRequest;
  bool shouldFailCreate = false;
  bool wasCancelCalled = false;
  String? canceledIncidentId;

  @override
  Future<IncidentResponseModel> createIncident(
    CreateIncidentRequestModel request,
  ) async {
    lastCreatedRequest = request;
    if (shouldFailCreate) {
      throw Exception('Network error during incident creation');
    }
    return IncidentResponseModel(
      id: 'mock-incident-1234',
      reporterFullName: 'John Doe',
      category: request.category,
      emergencyCode: request.emergencyCode,
      description: request.description,
      status: 'Open',
      latitude: request.latitude,
      longitude: request.longitude,
      createdAt: DateTime.now(),
    );
  }

  @override
  Future<bool> cancelIncident(String incidentId) async {
    wasCancelCalled = true;
    canceledIncidentId = incidentId;
    return true;
  }
}

class MockLocationService extends LocationService {
  final Position? mockPosition;
  final bool shouldThrow;

  const MockLocationService({this.mockPosition, this.shouldThrow = false});

  @override
  Future<Position> getCurrentLocation({
    Duration timeout = const Duration(seconds: 8),
  }) async {
    if (shouldThrow) {
      throw const LocationServiceException('GPS fix timed out');
    }
    return mockPosition ??
        Position(
          longitude: 27.123456,
          latitude: 38.654321,
          timestamp: DateTime.now(),
          accuracy: 5.0,
          altitude: 10.0,
          heading: 0.0,
          speed: 0.0,
          speedAccuracy: 0.0,
          altitudeAccuracy: 0.0,
          headingAccuracy: 0.0,
        );
  }
}

void main() {
  TestWidgetsFlutterBinding.ensureInitialized();

  group('RapidDispatchService Unit Tests', () {
    test(
      'dispatchRapidIncident creates incident request with accurate coordinates',
      () async {
        final repository = MockIncidentRepository();
        const locationService = MockLocationService();
        final service = RapidDispatchService(
          incidentRepository: repository,
          locationService: locationService,
        );

        final preset = RapidEmergencyPreset.presets.first; // Fire
        final result = await service.dispatchRapidIncident(preset);

        expect(result.id, equals('mock-incident-1234'));
        expect(repository.lastCreatedRequest, isNotNull);
        expect(repository.lastCreatedRequest!.category, equals('Fire'));
        expect(repository.lastCreatedRequest!.emergencyCode, equals('Red'));
        expect(repository.lastCreatedRequest!.latitude, equals(38.654321));
        expect(repository.lastCreatedRequest!.longitude, equals(27.123456));
      },
    );

    test(
      'cancelDispatchedIncident delegates call to repository cancelIncident',
      () async {
        final repository = MockIncidentRepository();
        const locationService = MockLocationService();
        final service = RapidDispatchService(
          incidentRepository: repository,
          locationService: locationService,
        );

        final isSuccess = await service.cancelDispatchedIncident(
          'mock-incident-1234',
        );

        expect(isSuccess, isTrue);
        expect(repository.wasCancelCalled, isTrue);
        expect(repository.canceledIncidentId, equals('mock-incident-1234'));
      },
    );
  });
}
