import 'dart:convert';
import 'package:dio/dio.dart';
import '../../../../core/constants/api_endpoints.dart';
import '../../../../core/network/api_client.dart';
import '../../../../core/storage/secure_storage_service.dart';
import '../../../profile/data/models/user_model.dart';
import '../models/auth_response_model.dart';
import '../models/login_request_model.dart';
import '../models/register_request_model.dart';

class AuthRepository {
  final ApiClient _apiClient;
  final SecureStorageService _storageService;

  AuthRepository({
    required ApiClient apiClient,
    required SecureStorageService storageService,
  })  : _apiClient = apiClient,
        _storageService = storageService;

  Future<AuthResponseModel> login(String email, String password) async {
    try {
      final request = LoginRequestModel(email: email, password: password);
      final response = await _apiClient.dio.post(
        ApiEndpoints.login,
        data: request.toJson(),
      );

      final dynamic rawData = response.data;
      Map<String, dynamic> responseData;
      if (rawData is String) {
        responseData = json.decode(rawData) as Map<String, dynamic>;
      } else if (rawData is Map<String, dynamic>) {
        responseData = rawData;
      } else {
        throw Exception('Invalid server response format.');
      }

      final isSuccess = responseData['success'] == true || responseData['Success'] == true;
      final payload = responseData['data'] ?? responseData['Data'];

      if (isSuccess && payload != null) {
        final authResponse = AuthResponseModel.fromJson(
          payload is Map<String, dynamic> ? payload : {},
        );

        await _storageService.saveAccessToken(authResponse.accessToken);
        await _storageService.saveUserData(authResponse.user);

        return authResponse;
      } else {
        final message = responseData['message'] ?? responseData['Message'] ?? 'Authentication failed.';
        throw Exception(message.toString());
      }
    } on DioException catch (e) {
      final errorMsg = _extractErrorMessage(e);
      throw Exception(errorMsg);
    } catch (e) {
      throw Exception(e.toString().replaceAll('Exception: ', ''));
    }
  }

  Future<AuthResponseModel> register(RegisterRequestModel request) async {
    try {
      final response = await _apiClient.dio.post(
        ApiEndpoints.register,
        data: request.toJson(),
      );

      final dynamic rawData = response.data;
      Map<String, dynamic> responseData;
      if (rawData is String) {
        responseData = json.decode(rawData) as Map<String, dynamic>;
      } else if (rawData is Map<String, dynamic>) {
        responseData = rawData;
      } else {
        throw Exception('Invalid server response format.');
      }

      final isSuccess = responseData['success'] == true || responseData['Success'] == true;
      final payload = responseData['data'] ?? responseData['Data'];

      if (isSuccess && payload != null) {
        final authResponse = AuthResponseModel.fromJson(
          payload is Map<String, dynamic> ? payload : {},
        );

        await _storageService.saveAccessToken(authResponse.accessToken);
        await _storageService.saveUserData(authResponse.user);

        return authResponse;
      } else {
        final message = responseData['message'] ?? responseData['Message'] ?? 'Registration failed.';
        throw Exception(message.toString());
      }
    } on DioException catch (e) {
      final errorMsg = _extractErrorMessage(e);
      throw Exception(errorMsg);
    } catch (e) {
      throw Exception(e.toString().replaceAll('Exception: ', ''));
    }
  }

  Future<AuthResponseModel> googleLogin(String idToken) async {
    try {
      final response = await _apiClient.dio.post(
        ApiEndpoints.googleLogin,
        data: {'idToken': idToken},
      );

      final dynamic rawData = response.data;
      Map<String, dynamic> responseData = rawData is Map<String, dynamic> ? rawData : {};
      final isSuccess = responseData['success'] == true || responseData['Success'] == true;
      final payload = responseData['data'] ?? responseData['Data'];

      if (isSuccess && payload != null) {
        final authResponse = AuthResponseModel.fromJson(
          payload is Map<String, dynamic> ? payload : {},
        );

        await _storageService.saveAccessToken(authResponse.accessToken);
        await _storageService.saveUserData(authResponse.user);

        return authResponse;
      } else {
        final message = responseData['message'] ?? responseData['Message'] ?? 'Google login failed.';
        throw Exception(message.toString());
      }
    } on DioException catch (e) {
      final errorMsg = _extractErrorMessage(e);
      throw Exception(errorMsg);
    } catch (e) {
      throw Exception(e.toString().replaceAll('Exception: ', ''));
    }
  }

  Future<AuthResponseModel> googleRegister({
    required String idToken,
    String? phone,
    String? department,
  }) async {
    try {
      final response = await _apiClient.dio.post(
        ApiEndpoints.googleRegister,
        data: {
          'idToken': idToken,
          if (phone != null && phone.isNotEmpty) 'phone': phone,
          if (department != null && department.isNotEmpty) 'department': department,
        },
      );

      final dynamic rawData = response.data;
      Map<String, dynamic> responseData = rawData is Map<String, dynamic> ? rawData : {};
      final isSuccess = responseData['success'] == true || responseData['Success'] == true;
      final payload = responseData['data'] ?? responseData['Data'];

      if (isSuccess && payload != null) {
        final authResponse = AuthResponseModel.fromJson(
          payload is Map<String, dynamic> ? payload : {},
        );

        await _storageService.saveAccessToken(authResponse.accessToken);
        await _storageService.saveUserData(authResponse.user);

        return authResponse;
      } else {
        final message = responseData['message'] ?? responseData['Message'] ?? 'Google registration failed.';
        throw Exception(message.toString());
      }
    } on DioException catch (e) {
      final errorMsg = _extractErrorMessage(e);
      throw Exception(errorMsg);
    } catch (e) {
      throw Exception(e.toString().replaceAll('Exception: ', ''));
    }
  }

  Future<UserModel?> getCachedUser() async {
    final token = await _storageService.getAccessToken();
    if (token == null || token.isEmpty) {
      return null;
    }
    return await _storageService.getUserData();
  }

  Future<void> logout() async {
    await _storageService.clearSession();
  }

  String _extractErrorMessage(DioException error) {
    if (error.response?.data != null) {
      final raw = error.response!.data;
      if (raw is Map<String, dynamic>) {
        if (raw['message'] != null && raw['message'].toString().isNotEmpty) {
          return raw['message'].toString();
        }
        if (raw['Message'] != null && raw['Message'].toString().isNotEmpty) {
          return raw['Message'].toString();
        }
        if (raw['errors'] != null && (raw['errors'] is List) && (raw['errors'] as List).isNotEmpty) {
          return (raw['errors'] as List).first.toString();
        }
      } else if (raw is String && raw.isNotEmpty && !raw.startsWith('<!DOCTYPE')) {
        return raw;
      }
    }

    if (error.response?.statusCode == 400 || 
        error.response?.statusCode == 401 || 
        error.response?.statusCode == 403) {
      return 'Invalid email address or password.';
    }

    if (error.type == DioExceptionType.connectionTimeout || error.type == DioExceptionType.receiveTimeout) {
      return 'Server connection timed out. Please verify API is running.';
    }
    if (error.type == DioExceptionType.connectionError) {
      return 'Could not connect to API server (${error.requestOptions.baseUrl}).';
    }
    return 'Authentication failed. Please verify your credentials.';
  }

}
