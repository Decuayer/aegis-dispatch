import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:socar_dispatch_mobile/features/team_portal/presentation/widgets/claim_leadership_banner.dart';

void main() {
  group('ClaimLeadershipBanner Widget Tests', () {
    testWidgets('renders vacant leadership alert and responds to tap', (
      tester,
    ) async {
      bool wasClaimTapped = false;

      await tester.pumpWidget(
        MaterialApp(
          home: Scaffold(
            body: ClaimLeadershipBanner(
              isClaiming: false,
              onClaim: () {
                wasClaimTapped = true;
              },
            ),
          ),
        ),
      );

      expect(find.text('Team Leadership Vacant'), findsOneWidget);
      expect(find.text('Claim Leadership'), findsOneWidget);

      await tester.tap(find.text('Claim Leadership'));
      await tester.pump();

      expect(wasClaimTapped, isTrue);
    });
  });
}
