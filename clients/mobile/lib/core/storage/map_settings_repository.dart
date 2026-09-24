import 'package:shared_preferences/shared_preferences.dart';
import '../constants/facility_geo_constants.dart';

class MapSettingsRepository {
  final SharedPreferences _prefs;

  MapSettingsRepository({required SharedPreferences prefs}) : _prefs = prefs;

  /// Returns whether camera boundary lock is active (default: true)
  bool isBoundaryLockEnabled() {
    return _prefs.getBool(FacilityGeoConstants.prefKeyMapBoundaryLock) ?? true;
  }

  /// Persists the user's boundary lock preference to local storage
  Future<bool> setBoundaryLockEnabled(bool enabled) async {
    return await _prefs.setBool(
      FacilityGeoConstants.prefKeyMapBoundaryLock,
      enabled,
    );
  }
}
