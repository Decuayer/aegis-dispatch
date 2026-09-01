import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:socar_dispatch_mobile/features/employee_tracking/data/models/tracked_incident_model.dart';
import 'package:socar_dispatch_mobile/features/employee_tracking/presentation/widgets/active_incident_card.dart';

void main() {
  group('ActiveIncidentCard Widget Tests', () {
    testWidgets('renders incident badges and info accurately', (tester) async {
      bool tapped = false;

      final incident = TrackedIncidentModel(
        id: 'inc-test-12345678',
        reporterId: 'user-1',
        reporterFullName: 'Ali Veli',
        category: 'Fire',
        emergencyCode: 'Code Red',
        description: 'Smoke detected in tank area',
        status: 'Assigned',
        assignedTeamName: 'Fire Crew Alpha',
        latitude: 38.79,
        longitude: 26.92,
        createdAt: DateTime.now().subtract(const Duration(minutes: 15)),
      );

      await tester.pumpWidget(
        MaterialApp(
          home: Scaffold(
            body: ActiveIncidentCard(
              incident: incident,
              onTap: () => tapped = true,
            ),
          ),
        ),
      );

      expect(find.text('Code Red'), findsOneWidget);
      expect(find.text('Assigned'), findsOneWidget);
      expect(find.text('Fire Incident'), findsOneWidget);
      expect(find.text('Smoke detected in tank area'), findsOneWidget);
      expect(find.text('Fire Crew Alpha'), findsOneWidget);
      expect(find.text('15m ago'), findsOneWidget);

      await tester.tap(find.byType(ActiveIncidentCard));
      expect(tapped, isTrue);
    });
  });
}
