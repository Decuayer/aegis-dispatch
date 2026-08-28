import 'package:flutter/material.dart';
import 'app.dart';
import 'core/network/api_client.dart';
import 'core/storage/secure_storage_service.dart';
import 'features/auth/data/repositories/auth_repository.dart';
import 'features/incident_reporting/data/repositories/incident_repository.dart';
import 'features/incident_reporting/services/location_service.dart';
import 'features/incident_reporting/services/media_picker_service.dart';
import 'features/profile/data/repositories/media_repository.dart';
import 'features/profile/data/repositories/profile_repository.dart';
import 'features/team_tasks/data/repositories/task_repository.dart';
import 'features/team_tasks/services/route_service.dart';

void main() async {
  WidgetsFlutterBinding.ensureInitialized();

  // Core Services
  final secureStorage = SecureStorageService();
  final apiClient = ApiClient(storageService: secureStorage);
  const locationService = LocationService();
  final mediaPickerService = MediaPickerService();
  final routeService = RouteService();

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

  runApp(
    SocarDispatchApp(
      authRepository: authRepository,
      profileRepository: profileRepository,
      mediaRepository: mediaRepository,
      incidentRepository: incidentRepository,
      taskRepository: taskRepository,
      locationService: locationService,
      mediaPickerService: mediaPickerService,
      routeService: routeService,
    ),
  );
}
