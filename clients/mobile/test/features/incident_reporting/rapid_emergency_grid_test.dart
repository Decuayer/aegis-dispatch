import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:socar_dispatch_mobile/core/network/api_client.dart';
import 'package:socar_dispatch_mobile/core/storage/secure_storage_service.dart';
import 'package:socar_dispatch_mobile/features/home/presentation/widgets/rapid_emergency_grid.dart';
import 'package:socar_dispatch_mobile/features/incident_reporting/data/models/incident_response_model.dart';
import 'package:socar_dispatch_mobile/features/incident_reporting/data/models/rapid_emergency_preset.dart';
import 'package:socar_dispatch_mobile/features/incident_reporting/data/repositories/incident_repository.dart';
import 'package:socar_dispatch_mobile/features/incident_reporting/presentation/bloc/rapid_incident_cubit.dart';
import 'package:socar_dispatch_mobile/features/incident_reporting/services/location_service.dart';
import 'package:socar_dispatch_mobile/features/incident_reporting/services/rapid_dispatch_service.dart';

class StubSecureStorage extends SecureStorageService {
  @override
  Future<String?> getAccessToken() async => 'stub';
}

class StubDispatchService extends RapidDispatchService {
  StubDispatchService()
      : super(
          incidentRepository: IncidentRepository(
            apiClient: ApiClient(storageService: StubSecureStorage()),
          ),
          locationService: const LocationService(),
        );

  RapidEmergencyPreset? triggeredPreset;

  @override
  Future<IncidentResponseModel> dispatchRapidIncident(RapidEmergencyPreset preset) async {
    triggeredPreset = preset;
    return IncidentResponseModel(
      id: 'stub-id-1',
      reporterFullName: 'Tester',
      category: preset.category,
      emergencyCode: preset.emergencyCode,
      status: 'Open',
      latitude: 0,
      longitude: 0,
      createdAt: DateTime.now(),
    );
  }
}

void main() {
  testWidgets('RapidEmergencyGrid renders 4 preset buttons and responds to tap', (tester) async {
    final dispatchService = StubDispatchService();
    final cubit = RapidIncidentCubit(dispatchService: dispatchService);

    await tester.pumpWidget(
      MaterialApp(
        home: Scaffold(
          body: BlocProvider<RapidIncidentCubit>.value(
            value: cubit,
            child: const RapidEmergencyGrid(),
          ),
        ),
      ),
    );

    // Verify all 4 preset titles are displayed
    expect(find.text('Fire / Explosion'), findsOneWidget);
    expect(find.text('Gas Leak / Vapor'), findsOneWidget);
    expect(find.text('Medical Emergency'), findsOneWidget);
    expect(find.text('SOS / Panic Alert'), findsOneWidget);

    // Tap Fire button and advance clock to complete haptic & dispatch microtasks
    await tester.tap(find.text('Fire / Explosion'));
    await tester.pump();
    await tester.runAsync(() async {
      await Future<void>.delayed(const Duration(milliseconds: 100));
    });
    await tester.pump();

    expect(dispatchService.triggeredPreset?.id, equals('fire'));

    // Clean up cubit and active timers
    cubit.close();
    await tester.pump();
  });
}
