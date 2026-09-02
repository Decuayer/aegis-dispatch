import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:shared_preferences/shared_preferences.dart';
import 'package:socar_dispatch_mobile/core/storage/map_settings_repository.dart';
import 'package:socar_dispatch_mobile/features/profile/presentation/cubit/map_settings_cubit.dart';
import 'package:socar_dispatch_mobile/features/profile/presentation/widgets/map_settings_tile.dart';

void main() {
  TestWidgetsFlutterBinding.ensureInitialized();

  group('MapSettingsTile Widget Tests', () {
    testWidgets('renders tile with switch and responds to user toggle', (tester) async {
      SharedPreferences.setMockInitialValues({});
      final prefs = await SharedPreferences.getInstance();
      final repository = MapSettingsRepository(prefs: prefs);
      final cubit = MapSettingsCubit(repository: repository);

      await tester.pumpWidget(
        MaterialApp(
          home: Scaffold(
            body: BlocProvider.value(
              value: cubit,
              child: const MapSettingsTile(),
            ),
          ),
        ),
      );

      expect(find.text('Navigation & Map Settings'), findsOneWidget);
      expect(find.text('Lock Map to Facility Boundary'), findsOneWidget);

      final switchFinder = find.byType(Switch);
      expect(switchFinder, findsOneWidget);

      final initialSwitch = tester.widget<Switch>(switchFinder);
      expect(initialSwitch.value, isTrue);

      // Toggle switch
      await tester.tap(switchFinder);
      await tester.pumpAndSettle();

      expect(cubit.state.isBoundaryLockEnabled, isFalse);
    });
  });
}
