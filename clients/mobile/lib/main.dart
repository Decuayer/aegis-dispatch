import 'package:firebase_core/firebase_core.dart';
import 'package:firebase_messaging/firebase_messaging.dart';
import 'package:flutter/material.dart';
import 'app.dart';
import 'core/network/api_client.dart';
import 'core/permissions/permission_handler_service.dart';
import 'core/services/background_location_service.dart';
import 'core/services/fcm_notification_service.dart';
import 'core/services/local_notification_service.dart';
import 'core/storage/secure_storage_service.dart';
import 'features/auth/data/repositories/auth_repository.dart';
import 'features/incident_reporting/data/repositories/incident_repository.dart';
import 'features/incident_reporting/services/location_service.dart';
import 'features/incident_reporting/services/media_picker_service.dart';
import 'features/profile/data/repositories/media_repository.dart';
import 'features/profile/data/repositories/profile_repository.dart';
import 'features/team_tasks/data/repositories/task_repository.dart';
import 'features/team_tasks/services/route_service.dart';
import 'features/tracking/data/location_stream_repository.dart';
import 'package:shared_preferences/shared_preferences.dart';
import 'features/onboarding/data/onboarding_repository.dart';
import 'features/onboarding/services/onboarding_permission_service.dart';
import 'features/team_portal/data/repositories/team_portal_repository.dart';



void main() async {
  WidgetsFlutterBinding.ensureInitialized();

  // Initialize Native Platform Services
  try {
    await Firebase.initializeApp();
    FirebaseMessaging.onBackgroundMessage(firebaseMessagingBackgroundHandler);
  } catch (_) {
    // Non-fatal initialization fallback for headless/test environments
  }

  await LocalNotificationService().initialize();
  await BackgroundLocationService().initialize();

  // Storage & Persistent Preferences
  final prefs = await SharedPreferences.getInstance();
  final onboardingRepository = OnboardingRepository(prefs: prefs);
  const onboardingPermissionService = OnboardingPermissionService();

  // Core Services
  final secureStorage = SecureStorageService();
  final apiClient = ApiClient(storageService: secureStorage);
  const locationService = LocationService();
  const permissionService = PermissionHandlerService();
  final mediaPickerService = MediaPickerService();
  final routeService = RouteService();
  final fcmNotificationService = FcmNotificationService(apiClient: apiClient);
  final locationStreamRepository = LocationStreamRepository(storageService: secureStorage);

  // Repositories
  final authRepository = AuthRepository(
    apiClient: apiClient,
    storageService: secureStorage,
  );
  final profileRepository = ProfileRepository(
    apiClient: apiClient,
    storageService: secureStorage,
  );
  final mediaRepository = MediaRepository(
    apiClient: apiClient,
  );
  final incidentRepository = IncidentRepository(
    apiClient: apiClient,
  );
  final taskRepository = TaskRepository(
    apiClient: apiClient,
  );
  final teamPortalRepository = TeamPortalRepository(
    apiClient: apiClient,
  );

  runApp(
    SocarDispatchApp(
      authRepository: authRepository,
      profileRepository: profileRepository,
      mediaRepository: mediaRepository,
      incidentRepository: incidentRepository,
      taskRepository: taskRepository,
      locationService: locationService,
      permissionService: permissionService,
      mediaPickerService: mediaPickerService,
      routeService: routeService,
      fcmNotificationService: fcmNotificationService,
      locationStreamRepository: locationStreamRepository,
      onboardingRepository: onboardingRepository,
      onboardingPermissionService: onboardingPermissionService,
      teamPortalRepository: teamPortalRepository,
    ),
  );

}
