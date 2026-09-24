import 'package:permission_handler/permission_handler.dart';

class PermissionHandlerService {
  const PermissionHandlerService();

  Future<bool> requestNotificationPermission() async {
    final status = await Permission.notification.status;
    if (status.isGranted) {
      return true;
    }

    final result = await Permission.notification.request();
    return result.isGranted;
  }

  Future<bool> requestForegroundLocationPermission() async {
    final status = await Permission.locationWhenInUse.status;
    if (status.isGranted) {
      return true;
    }

    final result = await Permission.locationWhenInUse.request();
    return result.isGranted;
  }

  Future<bool> requestBackgroundLocationPermission() async {
    final isForegroundGranted = await requestForegroundLocationPermission();
    if (!isForegroundGranted) {
      return false;
    }

    final status = await Permission.locationAlways.status;
    if (status.isGranted) {
      return true;
    }

    final result = await Permission.locationAlways.request();
    return result.isGranted;
  }

  Future<bool> requestContinuousTrackingPermissions() async {
    await requestNotificationPermission();
    return await requestBackgroundLocationPermission();
  }

  Future<bool> isNotificationGranted() async {
    return await Permission.notification.isGranted;
  }

  Future<bool> isBackgroundLocationGranted() async {
    return await Permission.locationAlways.isGranted;
  }

  Future<bool> isLocationPermanentlyDenied() async {
    final whenInUseDenied =
        await Permission.locationWhenInUse.isPermanentlyDenied;
    final alwaysDenied = await Permission.locationAlways.isPermanentlyDenied;
    return whenInUseDenied || alwaysDenied;
  }

  Future<bool> openSettings() async {
    return await openAppSettings();
  }
}
