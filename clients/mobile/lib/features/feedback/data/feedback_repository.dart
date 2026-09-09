import 'package:dio/dio.dart';
import '../../../../core/constants/api_endpoints.dart';
import '../../../../core/network/api_client.dart';
import 'models/create_feedback_request.dart';
import 'models/feedback_response_model.dart';

class FeedbackRepository {
  final ApiClient _apiClient;

  FeedbackRepository({required ApiClient apiClient}) : _apiClient = apiClient;

  Future<FeedbackResponseModel> submitFeedback(
    CreateFeedbackRequest request,
  ) async {
    try {
      final formData = FormData();
      formData.fields.add(MapEntry('Title', request.formattedTitle));
      formData.fields.add(MapEntry('Description', request.description.trim()));

      if (request.attachments.isNotEmpty) {
        for (final media in request.attachments) {
          final multipartFile = await MultipartFile.fromFile(
            media.file.path,
            filename: media.fileName,
          );
          formData.files.add(MapEntry('Attachments', multipartFile));
        }
      }

      final response = await _apiClient.dio.post(
        ApiEndpoints.feedbacks,
        data: formData,
        options: Options(contentType: 'multipart/form-data'),
      );

      final responseData = response.data as Map<String, dynamic>;
      if (responseData['success'] == true && responseData['data'] != null) {
        return FeedbackResponseModel.fromJson(
          responseData['data'] as Map<String, dynamic>,
        );
      } else {
        final message =
            responseData['message'] as String? ?? 'Feedback submission failed.';
        throw Exception(message);
      }
    } on DioException catch (e) {
      throw Exception(_extractErrorMessage(e));
    }
  }

  String _extractErrorMessage(DioException error) {
    if (error.response?.data != null &&
        error.response?.data is Map<String, dynamic>) {
      final data = error.response!.data as Map<String, dynamic>;
      if (data['errors'] != null && (data['errors'] as List).isNotEmpty) {
        return (data['errors'] as List).first.toString();
      }
      if (data['message'] != null && data['message'].toString().isNotEmpty) {
        return data['message'].toString();
      }
    }
    return error.message ??
        'Failed to submit feedback. Please check your connection.';
  }
}
