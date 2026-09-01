import 'dart:io';
import 'package:dio/dio.dart';
import '../../../../core/constants/api_endpoints.dart';
import '../../../../core/network/api_client.dart';
import '../models/tracked_incident_model.dart';

class EmployeeIncidentRepository {
  final ApiClient _apiClient;

  EmployeeIncidentRepository({required ApiClient apiClient})
      : _apiClient = apiClient;

  // Fetches incidents reported by the employee (default: active Open,Assigned)
  Future<List<TrackedIncidentModel>> getMyReportedIncidents({
    required String reporterId,
    String? status,
  }) async {
    try {
      final response = await _apiClient.dio.get(
        ApiEndpoints.incidents,
        queryParameters: {
          'reporterId': reporterId,
          'status': status ?? 'Open,Assigned',
          'pageSize': 20,
        },
      );

      final responseData = response.data as Map<String, dynamic>;
      if (responseData['success'] == true && responseData['data'] != null) {
        final data = responseData['data'];
        final List<dynamic> items = data is Map<String, dynamic> && data['items'] != null
            ? data['items'] as List<dynamic>
            : (data is List<dynamic> ? data : []);

        return items
            .map((json) => TrackedIncidentModel.fromJson(json as Map<String, dynamic>))
            .toList();
      } else {
        final message = responseData['message'] as String? ?? 'Failed to load reported incidents.';
        throw Exception(message);
      }
    } on DioException catch (e) {
      throw Exception(_extractErrorMessage(e));
    }
  }

  // Fetches full incident details and enriches with team coordinates if assigned
  Future<TrackedIncidentModel> getIncidentDetails(String incidentId) async {
    try {
      final response = await _apiClient.dio.get(
        ApiEndpoints.incidentById(incidentId),
      );

      final responseData = response.data as Map<String, dynamic>;
      if (responseData['success'] == true && responseData['data'] != null) {
        var incident = TrackedIncidentModel.fromJson(
          responseData['data'] as Map<String, dynamic>,
        );

        // If a team is assigned, fetch the team's latest GPS coordinate
        if (incident.assignedTeamId != null && incident.assignedTeamId!.isNotEmpty) {
          try {
            final teamResponse = await _apiClient.dio.get(
              ApiEndpoints.teamById(incident.assignedTeamId!),
            );
            final teamData = teamResponse.data as Map<String, dynamic>;
            if (teamData['success'] == true && teamData['data'] != null) {
              final teamJson = teamData['data'] as Map<String, dynamic>;
              final double? teamLat = (teamJson['currentLatitude'] as num?)?.toDouble();
              final double? teamLng = (teamJson['currentLongitude'] as num?)?.toDouble();
              final String? teamStatus = teamJson['status'] as String?;
              final String? leaderPhone = teamJson['leaderPhone'] as String?;

              incident = incident.copyWith(
                teamLatitude: teamLat,
                teamLongitude: teamLng,
                assignedTeamStatus: teamStatus ?? incident.assignedTeamStatus,
                assignedTeamLeaderPhone: leaderPhone ?? incident.assignedTeamLeaderPhone,
              );
            }
          } catch (_) {
            // Non-blocking fallback if team endpoint fails
          }
        }

        return incident;
      } else {
        final message = responseData['message'] as String? ?? 'Failed to load incident details.';
        throw Exception(message);
      }
    } on DioException catch (e) {
      throw Exception(_extractErrorMessage(e));
    }
  }

  // Updates incident details (description & media attachments)
  Future<TrackedIncidentModel> updateIncident({
    required String incidentId,
    required String category,
    required String emergencyCode,
    String? description,
    required List<Map<String, dynamic>> mediaAttachments,
    required double latitude,
    required double longitude,
  }) async {
    try {
      final response = await _apiClient.dio.put(
        ApiEndpoints.incidentById(incidentId),
        data: {
          'category': category,
          'emergencyCode': emergencyCode,
          'description': description,
          'mediaAttachments': mediaAttachments,
          'latitude': latitude,
          'longitude': longitude,
        },
      );

      final responseData = response.data as Map<String, dynamic>;
      if (responseData['success'] == true && responseData['data'] != null) {
        return TrackedIncidentModel.fromJson(
          responseData['data'] as Map<String, dynamic>,
        );
      } else {
        final message = responseData['message'] as String? ?? 'Failed to update incident details.';
        throw Exception(message);
      }
    } on DioException catch (e) {
      throw Exception(_extractErrorMessage(e));
    }
  }

  // Uploads supplementary media file to MinIO S3 object storage
  Future<String> uploadSupplementaryMedia(File file) async {
    try {
      final fileName = file.path.split(Platform.pathSeparator).last;
      final formData = FormData.fromMap({
        'file': await MultipartFile.fromFile(
          file.path,
          filename: fileName,
        ),
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
        throw Exception('Media upload succeeded but returned an empty URL.');
      } else {
        final message = responseData['message'] as String? ?? 'Media upload failed.';
        throw Exception(message);
      }
    } on DioException catch (e) {
      throw Exception(_extractErrorMessage(e));
    }
  }

  String _extractErrorMessage(DioException error) {
    if (error.response?.data != null && error.response?.data is Map<String, dynamic>) {
      final data = error.response!.data as Map<String, dynamic>;
      if (data.containsKey('message')) {
        return data['message'] as String;
      }
    }
    return error.message ?? 'An unexpected network error occurred.';
  }
}
