import '../../../../core/services/background_location_service.dart';
import '../../../../core/storage/secure_storage_service.dart';

class LocationStreamRepository {
  final BackgroundLocationService _backgroundService;
  final SecureStorageService _storageService;

  LocationStreamRepository({
    BackgroundLocationService? backgroundService,
    required SecureStorageService storageService,
  }) : _backgroundService = backgroundService ?? BackgroundLocationService(),
       _storageService = storageService;

  Stream<Map<String, dynamic>?> get onLocationUpdate =>
      _backgroundService.onLocationUpdate;

  Future<bool> startTracking(String teamId) async {
    final token = await _storageService.getAccessToken();
    if (token == null || token.isEmpty) {
      return false;
    }

    return await _backgroundService.startTracking(teamId: teamId, token: token);
  }

  Future<void> stopTracking() async {
    await _backgroundService.stopTracking();
  }

  Future<bool> isTracking() async {
    return await _backgroundService.isTracking();
  }
}
