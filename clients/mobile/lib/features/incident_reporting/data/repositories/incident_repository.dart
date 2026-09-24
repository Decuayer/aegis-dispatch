import 'dart:io';
import 'package:dio/dio.dart';
import '../../../../core/constants/api_endpoints.dart';
import '../../../../core/network/api_client.dart';
import '../models/create_incident_request_model.dart';
import '../models/emergency_code_model.dart';
import '../models/incident_response_model.dart';

class IncidentRepository {
  final ApiClient _apiClient;

  IncidentRepository({required ApiClient apiClient}) : _apiClient = apiClient;

  Future<List<EmergencyCodeModel>> getEmergencyCodes() async {
    try {
      final response = await _apiClient.dio.get(ApiEndpoints.emergencyCodes);
      final responseData = response.data as Map<String, dynamic>;

      if (responseData['success'] == true && responseData['data'] != null) {
        final List<dynamic> list = responseData['data'] as List<dynamic>;
        return list
            .map(
              (item) =>
                  EmergencyCodeModel.fromJson(item as Map<String, dynamic>),
            )
            .toList();
      } else {
        final message =
            responseData['message'] as String? ??
            'Failed to fetch emergency codes.';
        throw Exception(message);
      }
    } on DioException catch (e) {
      throw Exception(_extractErrorMessage(e));
    }
  }

  Future<String> uploadIncidentMedia(File file) async {
    try {
      final fileName = file.path.split(Platform.pathSeparator).last;
      final formData = FormData.fromMap({
        'file': await MultipartFile.fromFile(file.path, filename: fileName),
        'category': 'Incident',
      });

      final response = await _apiClient.dio.post(
        ApiEndpoints.uploadMedia,
        data: formData,
        options: Options(contentType: 'multipart/form-data'),
      );

      final responseData = response.data as Map<String, dynamic>;
      if (responseData['success'] == true && responseData['data'] != null) {
        final data = responseData['data'] as Map<String, dynamic>;
        final mediaUrl = data['mediaUrl'] as String?;
        if (mediaUrl != null && mediaUrl.isNotEmpty) {
          return mediaUrl;
        }
        throw Exception(
          'Media upload succeeded but returned an invalid media URL.',
        );
      } else {
        final message =
            responseData['message'] as String? ?? 'Media upload failed.';
        throw Exception(message);
      }
    } on DioException catch (e) {
      throw Exception(_extractErrorMessage(e));
    }
  }

  Future<IncidentResponseModel> createIncident(
    CreateIncidentRequestModel request,
  ) async {
    try {
      final response = await _apiClient.dio.post(
        ApiEndpoints.incidents,
        data: request.toJson(),
      );

      final responseData = response.data as Map<String, dynamic>;
      if (responseData['success'] == true && responseData['data'] != null) {
        return IncidentResponseModel.fromJson(
          responseData['data'] as Map<String, dynamic>,
        );
      } else {
        final message =
            responseData['message'] as String? ??
            'Failed to submit incident report.';
        throw Exception(message);
      }
    } on DioException catch (e) {
      throw Exception(_extractErrorMessage(e));
    }
  }

  /// Cancels an active incident report (within accidental trigger reversal window).
  Future<bool> cancelIncident(String incidentId) async {
    try {
      final response = await _apiClient.dio.patch(
        ApiEndpoints.incidentStatus(incidentId),
        data: {'status': 'Canceled'},
      );

      final responseData = response.data as Map<String, dynamic>;
      if (responseData['success'] == true) {
        return true;
      } else {
        final message =
            responseData['message'] as String? ?? 'Failed to cancel incident.';
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
      if (data['message'] != null && data['message'].toString().isNotEmpty) {
        return data['message'].toString();
      }
      if (data['errors'] != null && (data['errors'] as List).isNotEmpty) {
        return (data['errors'] as List).first.toString();
      }
    }
    return error.message ??
        'An unexpected network error occurred. Please try again.';
  }
}
