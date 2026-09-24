import '../../../core/permissions/permission_handler_service.dart';

enum OnboardingPermissionResult { granted, denied, permanentlyDenied }

class OnboardingPermissionService {
  final PermissionHandlerService _permissionHandlerService;

  const OnboardingPermissionService({
    PermissionHandlerService permissionHandlerService =
        const PermissionHandlerService(),
  }) : _permissionHandlerService = permissionHandlerService;

  /// Requests foreground location, background location, and push notification permissions in sequence.
  Future<OnboardingPermissionResult> requestOnboardingPermissions() async {
    // 1. Mandatory foreground location permission
    final foregroundGranted =
        await _permissionHandlerService.requestForegroundLocationPermission();
    if (!foregroundGranted) {
      final isPermanentlyDenied =
          await _permissionHandlerService.isLocationPermanentlyDenied();
      return isPermanentlyDenied
          ? OnboardingPermissionResult.permanentlyDenied
          : OnboardingPermissionResult.denied;
    }

    // 2. Background location permission for response tracking
    await _permissionHandlerService.requestBackgroundLocationPermission();

    // 3. Push notification permission for dispatch alerts
    await _permissionHandlerService.requestNotificationPermission();

    return OnboardingPermissionResult.granted;
  }

  /// Opens the native device application settings.
  Future<bool> openSettings() async {
    return await _permissionHandlerService.openSettings();
  }
}
