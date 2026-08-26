import 'package:dio/dio.dart';
import '../../../../core/constants/api_endpoints.dart';
import '../../../../core/network/api_client.dart';
import '../../../../core/storage/secure_storage_service.dart';
import '../models/update_profile_request_model.dart';
import '../models/user_model.dart';

class ProfileRepository {
  final ApiClient _apiClient;
  final SecureStorageService _storageService;

  ProfileRepository({
    required ApiClient apiClient,
    required SecureStorageService storageService,
  })  : _apiClient = apiClient,
        _storageService = storageService;

  Future<UserModel> fetchUserProfile() async {
    try {
      final response = await _apiClient.dio.get(ApiEndpoints.currentUser);
      final responseData = response.data as Map<String, dynamic>;

      if (responseData['success'] == true && responseData['data'] != null) {
        final user = UserModel.fromJson(responseData['data'] as Map<String, dynamic>);
        await _storageService.saveUserData(user);
        return user;
      } else {
        final message = responseData['message'] as String? ?? 'Failed to load profile.';
        throw Exception(message);
      }
    } on DioException catch (e) {
      final errorMsg = _extractErrorMessage(e);
      throw Exception(errorMsg);
    }
  }

  Future<UserModel> updateUserProfile(UpdateProfileRequestModel request) async {
    try {
      final response = await _apiClient.dio.put(
        ApiEndpoints.updateProfile,
        data: request.toJson(),
      );

      final responseData = response.data as Map<String, dynamic>;
      if (responseData['success'] == true && responseData['data'] != null) {
        final updatedUser = UserModel.fromJson(responseData['data'] as Map<String, dynamic>);
        await _storageService.saveUserData(updatedUser);
        return updatedUser;
      } else {
        final message = responseData['message'] as String? ?? 'Failed to update profile.';
        throw Exception(message);
      }
    } on DioException catch (e) {
      final errorMsg = _extractErrorMessage(e);
      throw Exception(errorMsg);
    }
  }

  String _extractErrorMessage(DioException error) {
    if (error.response?.data != null && error.response?.data is Map<String, dynamic>) {
      final data = error.response!.data as Map<String, dynamic>;
      if (data['message'] != null) {
        return data['message'].toString();
      }
      if (data['errors'] != null && (data['errors'] as List).isNotEmpty) {
        return (data['errors'] as List).first.toString();
      }
    }
    return error.message ?? 'Profile operation failed.';
  }
}
