import 'package:flutter_test/flutter_test.dart';
import 'package:socar_dispatch_mobile/core/network/api_client.dart';
import 'package:socar_dispatch_mobile/core/storage/secure_storage_service.dart';
import 'package:socar_dispatch_mobile/features/incident_reporting/data/models/incident_response_model.dart';
import 'package:socar_dispatch_mobile/features/incident_reporting/data/models/rapid_emergency_preset.dart';
import 'package:socar_dispatch_mobile/features/incident_reporting/data/repositories/incident_repository.dart';
import 'package:socar_dispatch_mobile/features/incident_reporting/presentation/bloc/rapid_incident_cubit.dart';
import 'package:socar_dispatch_mobile/features/incident_reporting/presentation/bloc/rapid_incident_state.dart';
import 'package:socar_dispatch_mobile/features/incident_reporting/services/location_service.dart';
import 'package:socar_dispatch_mobile/features/incident_reporting/services/rapid_dispatch_service.dart';

class FakeSecureStorage extends SecureStorageService {
  @override
  Future<String?> getAccessToken() async => 'test-token';
}

class FakeRapidDispatchService extends RapidDispatchService {
  FakeRapidDispatchService()
    : super(
        incidentRepository: IncidentRepository(
          apiClient: ApiClient(storageService: FakeSecureStorage()),
        ),
        locationService: const LocationService(),
      );

  bool shouldFail = false;
  bool wasCancelCalled = false;

  @override
  Future<IncidentResponseModel> dispatchRapidIncident(
    RapidEmergencyPreset preset,
  ) async {
    if (shouldFail) {
      throw Exception('Failed to connect to emergency server.');
    }
    return IncidentResponseModel(
      id: 'rapid-id-777',
      reporterFullName: 'Field Employee',
      category: preset.category,
      emergencyCode: preset.emergencyCode,
      status: 'Open',
      latitude: 38.0,
      longitude: 27.0,
      createdAt: DateTime.now(),
    );
  }

  @override
  Future<bool> cancelDispatchedIncident(String incidentId) async {
    wasCancelCalled = true;
    return true;
  }
}

void main() {
  TestWidgetsFlutterBinding.ensureInitialized();

  group('RapidIncidentCubit Unit Tests', () {
    test('initial state is RapidIncidentInitial', () {
      final dispatchService = FakeRapidDispatchService();
      final cubit = RapidIncidentCubit(dispatchService: dispatchService);

      expect(cubit.state, isA<RapidIncidentInitial>());
      cubit.close();
    });

    test('triggerRapidIncident emits Submitting and then Success', () async {
      final dispatchService = FakeRapidDispatchService();
      final cubit = RapidIncidentCubit(dispatchService: dispatchService);
      final preset = RapidEmergencyPreset.presets.first;

      expectLater(
        cubit.stream,
        emitsInOrder([
          isA<RapidIncidentSubmitting>(),
          isA<RapidIncidentSuccess>()
              .having((s) => s.incident.id, 'id', equals('rapid-id-777'))
              .having((s) => s.isReversible, 'isReversible', isTrue)
              .having((s) => s.remainingSeconds, 'remainingSeconds', equals(5)),
        ]),
      );

      await cubit.triggerRapidIncident(preset);
      cubit.close();
    });

    test(
      'cancelDispatchedIncident emits Canceled and resets to Initial',
      () async {
        final dispatchService = FakeRapidDispatchService();
        final cubit = RapidIncidentCubit(dispatchService: dispatchService);

        expectLater(
          cubit.stream,
          emitsInOrder([
            isA<RapidIncidentCanceled>().having(
              (s) => s.incidentId,
              'id',
              'rapid-id-777',
            ),
            isA<RapidIncidentInitial>(),
          ]),
        );

        await cubit.cancelDispatchedIncident('rapid-id-777');
        expect(dispatchService.wasCancelCalled, isTrue);
        cubit.close();
      },
    );

    test('triggerRapidIncident emits Failure on service error', () async {
      final dispatchService = FakeRapidDispatchService()..shouldFail = true;
      final cubit = RapidIncidentCubit(dispatchService: dispatchService);
      final preset = RapidEmergencyPreset.presets.first;

      expectLater(
        cubit.stream,
        emitsInOrder([
          isA<RapidIncidentSubmitting>(),
          isA<RapidIncidentFailure>().having(
            (s) => s.errorMessage,
            'errorMessage',
            contains('Failed to connect to emergency server.'),
          ),
        ]),
      );

      await cubit.triggerRapidIncident(preset);
      cubit.close();
    });
  });
}
