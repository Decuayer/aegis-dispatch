import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:socar_dispatch_mobile/features/employee_tracking/presentation/widgets/assigned_team_info_card.dart';

void main() {
  group('AssignedTeamInfoCard Widget Tests', () {
    testWidgets('renders team name, leader, member count and call button', (
      tester,
    ) async {
      await tester.pumpWidget(
        const MaterialApp(
          home: Scaffold(
            body: AssignedTeamInfoCard(
              teamName: 'ERT Rapid Squad',
              leaderFullName: 'Captain Rauf',
              leaderPhone: '+905551112233',
              memberCount: 5,
              operationalStatus: 'EnRoute',
            ),
          ),
        ),
      );

      expect(find.text('ERT Rapid Squad'), findsOneWidget);
      expect(find.text('En Route'), findsOneWidget);
      expect(find.text('Team Leader: Captain Rauf'), findsOneWidget);
      expect(find.text('5 Responders'), findsOneWidget);
      expect(find.text('Call'), findsOneWidget);
      expect(find.byIcon(Icons.phone_in_talk_rounded), findsOneWidget);
    });
  });
}
