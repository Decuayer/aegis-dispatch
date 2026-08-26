import 'package:flutter/material.dart';
import 'app.dart';
import 'core/network/api_client.dart';
import 'core/storage/secure_storage_service.dart';
import 'features/auth/data/repositories/auth_repository.dart';
import 'features/profile/data/repositories/media_repository.dart';
import 'features/profile/data/repositories/profile_repository.dart';

void main() async {
  WidgetsFlutterBinding.ensureInitialized();

  // Core Services
  final secureStorage = SecureStorageService();
  final apiClient = ApiClient(storageService: secureStorage);

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

  runApp(
    SocarDispatchApp(
      authRepository: authRepository,
      profileRepository: profileRepository,
      mediaRepository: mediaRepository,
    ),
  );
}
