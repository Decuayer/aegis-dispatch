import 'package:flutter_test/flutter_test.dart';
import 'package:shared_preferences/shared_preferences.dart';
import 'package:socar_dispatch_mobile/core/constants/facility_geo_constants.dart';
import 'package:socar_dispatch_mobile/core/storage/map_settings_repository.dart';

void main() {
  TestWidgetsFlutterBinding.ensureInitialized();

  group('MapSettingsRepository Unit Tests', () {
    test('isBoundaryLockEnabled returns true by default when storage is empty', () async {
      SharedPreferences.setMockInitialValues({});
      final prefs = await SharedPreferences.getInstance();
      final repository = MapSettingsRepository(prefs: prefs);

      expect(repository.isBoundaryLockEnabled(), isTrue);
    });

    test('isBoundaryLockEnabled returns stored boolean value when set to false', () async {
      SharedPreferences.setMockInitialValues({
        FacilityGeoConstants.prefKeyMapBoundaryLock: false,
      });
      final prefs = await SharedPreferences.getInstance();
      final repository = MapSettingsRepository(prefs: prefs);

      expect(repository.isBoundaryLockEnabled(), isFalse);
    });

    test('setBoundaryLockEnabled updates preference in local storage', () async {
      SharedPreferences.setMockInitialValues({});
      final prefs = await SharedPreferences.getInstance();
      final repository = MapSettingsRepository(prefs: prefs);

      final success = await repository.setBoundaryLockEnabled(false);
      expect(success, isTrue);
      expect(repository.isBoundaryLockEnabled(), isFalse);

      await repository.setBoundaryLockEnabled(true);
      expect(repository.isBoundaryLockEnabled(), isTrue);
    });
  });
}
