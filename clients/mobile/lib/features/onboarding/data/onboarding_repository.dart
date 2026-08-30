import 'package:shared_preferences/shared_preferences.dart';

class OnboardingRepository {
  static const String _keyKvkkAccepted = 'has_accepted_kvkk';
  static const String _keyKvkkAcceptedAt = 'kvkk_accepted_at';

  final SharedPreferences _prefs;

  OnboardingRepository({required SharedPreferences prefs}) : _prefs = prefs;

  /// Checks if the user has accepted the KVKK privacy disclosure.
  bool hasAcceptedKvkk() {
    return _prefs.getBool(_keyKvkkAccepted) ?? false;
  }

  /// Persists KVKK consent status and records the timestamp when accepted.
  Future<bool> setKvkkAccepted(bool accepted) async {
    final success = await _prefs.setBool(_keyKvkkAccepted, accepted);
    if (accepted) {
      await _prefs.setString(_keyKvkkAcceptedAt, DateTime.now().toIso8601String());
    } else {
      await _prefs.remove(_keyKvkkAcceptedAt);
    }
    return success;
  }

  /// Returns the timestamp when the consent was granted, if available.
  DateTime? getKvkkAcceptedAt() {
    final dateStr = _prefs.getString(_keyKvkkAcceptedAt);
    if (dateStr == null) return null;
    return DateTime.tryParse(dateStr);
  }

  /// Clears stored onboarding flags (used for logout or testing).
  Future<void> clearOnboarding() async {
    await _prefs.remove(_keyKvkkAccepted);
    await _prefs.remove(_keyKvkkAcceptedAt);
  }
}
