import 'dart:io';
import 'package:flutter/foundation.dart';

class ApiEndpoints {
  ApiEndpoints._();

  static String get baseUrl {
    if (kIsWeb) {
      return 'http://localhost:5233';
    }
    if (Platform.isAndroid) {
      return 'http://10.0.2.2:5233';
    }
    return 'http://localhost:5233';
  }

  // Auth endpoints
  static const String login = '/api/v1/auth/login';
  static const String register = '/api/v1/auth/register';
  static const String googleLogin = '/api/v1/auth/google-login';

  // User & Profile endpoints
  static const String currentUser = '/api/v1/users/me';
  static const String updateProfile = '/api/v1/users/me';
  static const String updateDeviceToken = '/api/v1/users/me/device-token';
  static const String usersDirectory = '/api/v1/users';

  // Media endpoints
  static const String uploadMedia = '/api/v1/media/upload';
}
