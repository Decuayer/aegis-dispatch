import 'package:flutter_test/flutter_test.dart';
import 'package:shared_preferences/shared_preferences.dart';
import 'package:socar_dispatch_mobile/core/storage/map_settings_repository.dart';
import 'package:socar_dispatch_mobile/features/profile/presentation/cubit/map_settings_cubit.dart';

void main() {
  TestWidgetsFlutterBinding.ensureInitialized();

  group('MapSettingsCubit Tests', () {
    test('initial state reflects repository boundary lock status', () async {
      SharedPreferences.setMockInitialValues({});
      final prefs = await SharedPreferences.getInstance();
      final repository = MapSettingsRepository(prefs: prefs);
      final cubit = MapSettingsCubit(repository: repository);

      expect(cubit.state.isBoundaryLockEnabled, isTrue);
    });

    test('toggleBoundaryLock updates state and persists change', () async {
      SharedPreferences.setMockInitialValues({});
      final prefs = await SharedPreferences.getInstance();
      final repository = MapSettingsRepository(prefs: prefs);
      final cubit = MapSettingsCubit(repository: repository);

      await cubit.toggleBoundaryLock(false);
      expect(cubit.state.isBoundaryLockEnabled, isFalse);
      expect(repository.isBoundaryLockEnabled(), isFalse);

      await cubit.toggleBoundaryLock(true);
      expect(cubit.state.isBoundaryLockEnabled, isTrue);
      expect(repository.isBoundaryLockEnabled(), isTrue);
    });
  });
}
