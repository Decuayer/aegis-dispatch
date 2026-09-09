import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:socar_dispatch_mobile/features/team_tasks/data/models/team_model.dart';
import 'package:socar_dispatch_mobile/features/team_tasks/presentation/widgets/status_action_bar.dart';

void main() {
  group('StatusActionBar Widget Tests', () {
    testWidgets(
      'tapping Depart Now invokes onStatusChangeRequested with EnRoute without dialog',
      (tester) async {
        TeamStatus? requestedStatus;

        await tester.pumpWidget(
          MaterialApp(
            home: Scaffold(
              bottomNavigationBar: StatusActionBar(
                currentStatus: TeamStatus.forwarded,
                isLoading: false,
                onStatusChangeRequested: (status) => requestedStatus = status,
                onResolveRequested: () {},
              ),
            ),
          ),
        );

        expect(find.text('Depart Now (En Route)'), findsOneWidget);

        await tester.tap(find.text('Depart Now (En Route)'));
        await tester.pump();

        expect(requestedStatus, equals(TeamStatus.enRoute));
        expect(find.byType(AlertDialog), findsNothing);
      },
    );

    testWidgets(
      'tapping Confirm On Scene invokes onStatusChangeRequested with OnScene without dialog',
      (tester) async {
        TeamStatus? requestedStatus;

        await tester.pumpWidget(
          MaterialApp(
            home: Scaffold(
              bottomNavigationBar: StatusActionBar(
                currentStatus: TeamStatus.enRoute,
                isLoading: false,
                onStatusChangeRequested: (status) => requestedStatus = status,
                onResolveRequested: () {},
              ),
            ),
          ),
        );

        expect(find.text('Confirm On Scene'), findsOneWidget);

        await tester.tap(find.text('Confirm On Scene'));
        await tester.pump();

        expect(requestedStatus, equals(TeamStatus.onScene));
        expect(find.byType(AlertDialog), findsNothing);
      },
    );

    testWidgets('tapping Complete Task & Debrief invokes onResolveRequested', (
      tester,
    ) async {
      bool resolveCalled = false;

      await tester.pumpWidget(
        MaterialApp(
          home: Scaffold(
            bottomNavigationBar: StatusActionBar(
              currentStatus: TeamStatus.onScene,
              isLoading: false,
              onStatusChangeRequested: (_) {},
              onResolveRequested: () => resolveCalled = true,
            ),
          ),
        ),
      );

      expect(find.text('Complete Task & Debrief'), findsOneWidget);

      await tester.tap(find.text('Complete Task & Debrief'));
      await tester.pump();

      expect(resolveCalled, isTrue);
    });

    testWidgets(
      'shows CircularProgressIndicator and disables tap when isLoading is true',
      (tester) async {
        TeamStatus? requestedStatus;

        await tester.pumpWidget(
          MaterialApp(
            home: Scaffold(
              bottomNavigationBar: StatusActionBar(
                currentStatus: TeamStatus.forwarded,
                isLoading: true,
                onStatusChangeRequested: (status) => requestedStatus = status,
                onResolveRequested: () {},
              ),
            ),
          ),
        );

        expect(find.byType(CircularProgressIndicator), findsOneWidget);
        expect(find.text('Depart Now (En Route)'), findsNothing);

        final button = tester.widget<ElevatedButton>(
          find.byType(ElevatedButton),
        );
        expect(button.onPressed, isNull);
        expect(requestedStatus, isNull);
      },
    );

    testWidgets('renders empty SizedBox when status is idle or resolved', (
      tester,
    ) async {
      await tester.pumpWidget(
        MaterialApp(
          home: Scaffold(
            bottomNavigationBar: StatusActionBar(
              currentStatus: TeamStatus.idle,
              isLoading: false,
              onStatusChangeRequested: (_) {},
              onResolveRequested: () {},
            ),
          ),
        ),
      );

      expect(find.byType(ElevatedButton), findsNothing);

      await tester.pumpWidget(
        MaterialApp(
          home: Scaffold(
            bottomNavigationBar: StatusActionBar(
              currentStatus: TeamStatus.resolved,
              isLoading: false,
              onStatusChangeRequested: (_) {},
              onResolveRequested: () {},
            ),
          ),
        ),
      );

      expect(find.byType(ElevatedButton), findsNothing);
    });
  });
}
