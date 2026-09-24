import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:socar_dispatch_mobile/features/tracking/presentation/widgets/tracking_status_chip.dart';

void main() {
  group('TrackingStatusChip Widget Tests', () {
    testWidgets('renders inactive standby status correctly', (tester) async {
      await tester.pumpWidget(
        const MaterialApp(
          home: Scaffold(body: TrackingStatusChip(isTracking: false)),
        ),
      );

      expect(find.text('GPS Standby'), findsOneWidget);
    });

    testWidgets('renders active live tracking status with pulse', (
      tester,
    ) async {
      await tester.pumpWidget(
        const MaterialApp(
          home: Scaffold(body: TrackingStatusChip(isTracking: true)),
        ),
      );

      expect(find.text('Live GPS Active'), findsOneWidget);
      await tester.pump(const Duration(milliseconds: 750));
      expect(find.text('Live GPS Active'), findsOneWidget);
    });
  });
}
