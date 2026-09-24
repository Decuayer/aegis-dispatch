import 'dart:io';
import 'package:dio/dio.dart';
import '../../../../core/constants/api_endpoints.dart';
import '../../../../core/network/api_client.dart';
import '../models/task_detail_model.dart';
import '../models/team_model.dart';

class TaskRepository {
  final ApiClient _apiClient;

  TaskRepository({required ApiClient apiClient}) : _apiClient = apiClient;

  Future<TeamModel?> getTeamForUser(String userId) async {
    try {
      final response = await _apiClient.dio.get(ApiEndpoints.teams);
      final responseData = response.data as Map<String, dynamic>;

      if (responseData['success'] == true && responseData['data'] != null) {
        final list = responseData['data'] as List<dynamic>;
        for (var item in list) {
          final team = TeamModel.fromJson(item as Map<String, dynamic>);
          final isMember =
              team.members.any(
                (m) => m.userId.toLowerCase() == userId.toLowerCase(),
              ) ||
              (team.leaderId != null &&
                  team.leaderId!.toLowerCase() == userId.toLowerCase());
          if (isMember) {
            return team;
          }
        }
      }
      return null;
    } on DioException catch (e) {
      throw Exception(_extractErrorMessage(e));
    }
  }

  Future<TeamTaskModel?> getActiveTaskForTeam(String teamId) async {
    try {
      final response = await _apiClient.dio.get(ApiEndpoints.incidents);
      final responseData = response.data as Map<String, dynamic>;

      if (responseData['success'] == true && responseData['data'] != null) {
        final list = _extractListFromData(responseData['data']);
        for (var item in list) {
          final incident = TeamTaskModel.fromJson(item as Map<String, dynamic>);
          final isTeamAssigned =
              incident.assignedTeamId?.toLowerCase() == teamId.toLowerCase();
          final isActive =
              incident.status != 'Resolved' && incident.status != 'Canceled';

          if (isTeamAssigned && isActive) {
            return incident;
          }
        }
      }
      return null;
    } on DioException catch (e) {
      throw Exception(_extractErrorMessage(e));
    }
  }

  Future<List<TeamTaskModel>> getTaskHistory(String teamId) async {
    try {
      final response = await _apiClient.dio.get(
        ApiEndpoints.incidents,
        queryParameters: {'status': 'Resolved'},
      );
      final responseData = response.data as Map<String, dynamic>;
      if (responseData['success'] == true && responseData['data'] != null) {
        final list = _extractListFromData(responseData['data']);
        return list
            .map((item) => TeamTaskModel.fromJson(item as Map<String, dynamic>))
            .where(
              (task) =>
                  task.assignedTeamId?.toLowerCase() == teamId.toLowerCase(),
            )
            .toList();
      }
      return [];
    } on DioException catch (e) {
      throw Exception(_extractErrorMessage(e));
    }
  }

  /// Safely extracts dynamic list whether backend returns raw List or PagedResult { items: [] }
  List<dynamic> _extractListFromData(dynamic data) {
    if (data is List<dynamic>) {
      return data;
    } else if (data is Map<String, dynamic> && data['items'] is List<dynamic>) {
      return data['items'] as List<dynamic>;
    }
    return const [];
  }

  Future<void> updateTeamStatus(String teamId, TeamStatus status) async {
    try {
      final response = await _apiClient.dio.patch(
        ApiEndpoints.teamStatus(teamId),
        data: {'status': status.apiValue},
      );
      final responseData = response.data as Map<String, dynamic>;
      if (responseData['success'] != true) {
        throw Exception(
          responseData['message'] ?? 'Failed to update team status.',
        );
      }
    } on DioException catch (e) {
      throw Exception(_extractErrorMessage(e));
    }
  }

  Future<void> updateMemberStatus(
    String teamId,
    String userId,
    MemberStatus status,
  ) async {
    try {
      final response = await _apiClient.dio.patch(
        ApiEndpoints.teamMemberStatus(teamId, userId),
        data: {'status': status.apiValue},
      );
      final responseData = response.data as Map<String, dynamic>;
      if (responseData['success'] != true) {
        throw Exception(
          responseData['message'] ?? 'Failed to update member status.',
        );
      }
    } on DioException catch (e) {
      throw Exception(_extractErrorMessage(e));
    }
  }

  Future<void> updateTeamLocation({
    required String teamId,
    required double latitude,
    required double longitude,
  }) async {
    try {
      await _apiClient.dio.post(
        ApiEndpoints.teamLocation,
        data: {'teamId': teamId, 'latitude': latitude, 'longitude': longitude},
      );
    } catch (_) {
      // Ignored for background location streaming resilience
    }
  }

  Future<String?> uploadDebriefMedia(File file) async {
    try {
      final fileName = file.path.split(Platform.pathSeparator).last;
      final formData = FormData.fromMap({
        'file': await MultipartFile.fromFile(file.path, filename: fileName),
        'category': 'IncidentReport',
      });

      final response = await _apiClient.dio.post(
        ApiEndpoints.uploadMedia,
        data: formData,
        options: Options(contentType: 'multipart/form-data'),
      );

      final responseData = response.data as Map<String, dynamic>;
      if (responseData['success'] == true && responseData['data'] != null) {
        return responseData['data']['mediaUrl'] as String?;
      }
      return null;
    } on DioException catch (e) {
      throw Exception(_extractErrorMessage(e));
    }
  }

  Future<void> completeTaskWithReport({
    required String incidentId,
    required String teamId,
    required String notes,
    File? mediaFile,
  }) async {
    try {
      String? mediaUrl;
      if (mediaFile != null) {
        mediaUrl = await uploadDebriefMedia(mediaFile);
      }

      // 1. Submit incident report
      final reportResponse = await _apiClient.dio.post(
        ApiEndpoints.incidentReports(incidentId),
        data: {'teamId': teamId, 'content': notes, 'mediaUrl': mediaUrl},
      );
      final reportData = reportResponse.data as Map<String, dynamic>;
      if (reportData['success'] != true) {
        throw Exception(
          reportData['message'] ?? 'Failed to create completion report.',
        );
      }

      // 2. Mark incident as Resolved
      final statusResponse = await _apiClient.dio.patch(
        ApiEndpoints.incidentStatus(incidentId),
        data: {'status': 'Resolved', 'completionNotes': notes},
      );
      final statusData = statusResponse.data as Map<String, dynamic>;
      if (statusData['success'] != true) {
        throw Exception(statusData['message'] ?? 'Failed to resolve incident.');
      }

      // 3. Reset team status to Idle
      await updateTeamStatus(teamId, TeamStatus.idle);
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
