import 'dart:async';
import 'package:geolocator/geolocator.dart';

class LocationServiceException implements Exception {
  final String message;
  final bool isPermissionPermanentlyDenied;

  const LocationServiceException(
    this.message, {
    this.isPermissionPermanentlyDenied = false,
  });

  @override
  String toString() => message;
}

class LocationService {
  const LocationService();

  Future<bool> isLocationServiceEnabled() async {
    return await Geolocator.isLocationServiceEnabled();
  }

  Future<LocationPermission> checkAndRequestPermission() async {
    final serviceEnabled = await isLocationServiceEnabled();
    if (!serviceEnabled) {
      throw const LocationServiceException(
        'Location services are disabled. Please enable GPS on your device.',
      );
    }

    LocationPermission permission = await Geolocator.checkPermission();
    if (permission == LocationPermission.denied) {
      permission = await Geolocator.requestPermission();
      if (permission == LocationPermission.denied) {
        throw const LocationServiceException(
          'Location permission denied. Please grant location access.',
        );
      }
    }

    if (permission == LocationPermission.deniedForever) {
      throw const LocationServiceException(
        'Location permissions are permanently denied. Please enable them in app settings.',
        isPermissionPermanentlyDenied: true,
      );
    }

    return permission;
  }

  Future<Position> getCurrentLocation({
    Duration timeout = const Duration(seconds: 8),
  }) async {
    await checkAndRequestPermission();

    try {
      return await Geolocator.getCurrentPosition(
        locationSettings: const LocationSettings(
          accuracy: LocationAccuracy.high,
          timeLimit: Duration(seconds: 8),
        ),
      );
    } on TimeoutException {
      final lastKnown = await Geolocator.getLastKnownPosition();
      if (lastKnown != null) {
        return lastKnown;
      }
      throw const LocationServiceException(
        'Location request timed out. Unable to obtain accurate GPS fix.',
      );
    } catch (e) {
      if (e is LocationServiceException) rethrow;
      throw LocationServiceException(
        'Failed to acquire device location: ${e.toString()}',
      );
    }
  }

  Future<bool> openAppSettings() async {
    return await Geolocator.openAppSettings();
  }

  Future<bool> openLocationSettings() async {
    return await Geolocator.openLocationSettings();
  }
}
