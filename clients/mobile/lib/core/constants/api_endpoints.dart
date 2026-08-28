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

  // Incident & Emergency Endpoints
  static const String emergencyCodes = '/api/v1/emergency-codes';
  static const String incidents = '/api/v1/incidents';
  static String incidentById(String id) => '/api/v1/incidents/$id';
  static String incidentStatus(String id) => '/api/v1/incidents/$id/status';
  static String incidentReports(String incidentId) => '/api/v1/incidents/$incidentId/reports';

  // Response Teams & Tasks Endpoints
  static const String teams = '/api/v1/teams';
  static String teamById(String id) => '/api/v1/teams/$id';
  static String teamStatus(String teamId) => '/api/v1/teams/$teamId/status';
  static String teamMemberStatus(String teamId, String userId) => '/api/v1/teams/$teamId/members/$userId/status';
  static const String teamLocation = '/api/v1/teams/location';
}
