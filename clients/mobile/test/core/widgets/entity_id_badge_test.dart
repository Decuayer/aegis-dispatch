import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:socar_dispatch_mobile/core/widgets/entity_id_badge.dart';

void main() {
  group('EntityIdBadge Widget Tests', () {
    testWidgets('formats UUID into #INC-XXXX badge for incident type', (
      tester,
    ) async {
      await tester.pumpWidget(
        const MaterialApp(
          home: Scaffold(
            body: EntityIdBadge(
              id: '3fa85f64-5717-4562-b3fc-2c963f66afa6',
              type: EntityBadgeType.incident,
            ),
          ),
        ),
      );

      expect(find.text('#INC-3FA8'), findsOneWidget);
      expect(find.byIcon(Icons.copy_rounded), findsOneWidget);
    });

    testWidgets('formats team ID into #TEAM-XX badge for team type', (
      tester,
    ) async {
      await tester.pumpWidget(
        const MaterialApp(
          home: Scaffold(
            body: EntityIdBadge(id: '04', type: EntityBadgeType.team),
          ),
        ),
      );

      expect(find.text('#TEAM-04'), findsOneWidget);
    });

    testWidgets('preserves already-formatted badge string', (tester) async {
      await tester.pumpWidget(
        const MaterialApp(
          home: Scaffold(
            body: EntityIdBadge(
              id: '#INC-A1B2',
              type: EntityBadgeType.incident,
            ),
          ),
        ),
      );

      expect(find.text('#INC-A1B2'), findsOneWidget);
    });

    testWidgets('hides copy icon when isCompact is true', (tester) async {
      await tester.pumpWidget(
        const MaterialApp(
          home: Scaffold(
            body: EntityIdBadge(
              id: 'test-1234',
              type: EntityBadgeType.incident,
              isCompact: true,
            ),
          ),
        ),
      );

      expect(find.text('#INC-TEST'), findsOneWidget);
      expect(find.byIcon(Icons.copy_rounded), findsNothing);
    });

    testWidgets('tapping disabled when isCopyable is false', (tester) async {
      await tester.pumpWidget(
        const MaterialApp(
          home: Scaffold(
            body: EntityIdBadge(
              id: 'team-01',
              type: EntityBadgeType.team,
              isCopyable: false,
            ),
          ),
        ),
      );

      expect(find.byIcon(Icons.copy_rounded), findsNothing);

      await tester.tap(find.byType(EntityIdBadge));
      await tester.pump();

      expect(find.byType(SnackBar), findsNothing);
    });
  });
}
