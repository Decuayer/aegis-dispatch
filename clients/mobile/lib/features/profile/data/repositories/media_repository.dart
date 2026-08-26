import 'dart:io';
import 'package:dio/dio.dart';
import '../../../../core/constants/api_endpoints.dart';
import '../../../../core/network/api_client.dart';
import '../models/media_upload_response_model.dart';

class MediaRepository {
  final ApiClient _apiClient;

  MediaRepository({required ApiClient apiClient}) : _apiClient = apiClient;

  Future<String> uploadAvatar(File imageFile) async {
    try {
      final fileName = imageFile.path.split('/').last;

      final formData = FormData.fromMap({
        'file': await MultipartFile.fromFile(
          imageFile.path,
          filename: fileName,
        ),
        'category': 'Avatar',
      });

      final response = await _apiClient.dio.post(
        ApiEndpoints.uploadMedia,
        data: formData,
        options: Options(
          contentType: 'multipart/form-data',
        ),
      );

      final responseData = response.data as Map<String, dynamic>;
      if (responseData['success'] == true && responseData['data'] != null) {
        final mediaModel = MediaUploadResponseModel.fromJson(
          responseData['data'] as Map<String, dynamic>,
        );
        return mediaModel.mediaUrl;
      } else {
        final message = responseData['message'] as String? ?? 'Media upload failed.';
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
    return error.message ?? 'Failed to upload media file. Please try again.';
  }
}
