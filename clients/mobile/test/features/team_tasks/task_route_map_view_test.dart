import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:latlong2/latlong.dart';
import 'package:socar_dispatch_mobile/features/team_tasks/presentation/views/task_route_map_view.dart';
import 'package:socar_dispatch_mobile/features/team_tasks/presentation/widgets/incident_map_marker.dart';

void main() {
  const testIncidentLoc = LatLng(38.8000, 26.9325);
  const testTeamLoc = LatLng(38.7950, 26.9250);

  group('TaskRouteMapView Widget Tests', () {
    testWidgets('renders Facility Lock badge when boundary lock is enabled', (
      tester,
    ) async {
      await tester.pumpWidget(
        const MaterialApp(
          home: Scaffold(
            body: TaskRouteMapView(
              teamLocation: testTeamLoc,
              incidentLocation: testIncidentLoc,
              routePoints: [testTeamLoc, testIncidentLoc],
              distanceKm: 1.2,
              estimatedMinutes: 4,
              incidentId: '3fa85f64-5717-4562-b3fc-2c963f66afa6',
              teamId: '04',
              isBoundaryLocked: true,
            ),
          ),
        ),
      );

      expect(find.text('Facility Lock'), findsOneWidget);
      expect(find.byIcon(Icons.lock_outline_rounded), findsOneWidget);
      expect(find.text('#INC-3FA8'), findsOneWidget);
      expect(find.text('#TEAM-04'), findsOneWidget);
    });

    testWidgets('renders Unrestricted badge when boundary lock is disabled', (
      tester,
    ) async {
      await tester.pumpWidget(
        const MaterialApp(
          home: Scaffold(
            body: TaskRouteMapView(
              teamLocation: testTeamLoc,
              incidentLocation: testIncidentLoc,
              routePoints: [testTeamLoc, testIncidentLoc],
              distanceKm: 2.5,
              estimatedMinutes: 7,
              isBoundaryLocked: false,
            ),
          ),
        ),
      );

      expect(find.text('Unrestricted'), findsOneWidget);
      expect(find.byIcon(Icons.lock_open_rounded), findsOneWidget);
    });

    testWidgets(
      'IncidentMapMarker and ResponderMapMarker render expected icons',
      (tester) async {
        await tester.pumpWidget(
          const MaterialApp(
            home: Scaffold(
              body: Column(
                children: [
                  IncidentMapMarker(incidentId: 'INC-A1B2'),
                  ResponderMapMarker(teamId: 'TEAM-07'),
                ],
              ),
            ),
          ),
        );

        expect(find.text('#INC-A1B2'), findsOneWidget);
        expect(find.text('#TEAM-07'), findsOneWidget);
        expect(find.byIcon(Icons.local_fire_department), findsOneWidget);
        expect(find.byIcon(Icons.navigation_rounded), findsOneWidget);
      },
    );
  });
}
