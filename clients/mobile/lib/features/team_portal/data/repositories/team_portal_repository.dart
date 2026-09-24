import 'package:dio/dio.dart';
import '../../../../core/constants/api_endpoints.dart';
import '../../../../core/network/api_client.dart';
import '../../../team_tasks/data/models/team_model.dart';
import '../models/available_team_model.dart';

class TeamPortalRepository {
  final ApiClient _apiClient;

  TeamPortalRepository({required ApiClient apiClient}) : _apiClient = apiClient;

  /// Fetches the active team for the given user, or returns null if unassigned.
  Future<TeamModel?> getUserTeam(String userId) async {
    try {
      final response = await _apiClient.dio.get(ApiEndpoints.teams);
      final responseData = response.data as Map<String, dynamic>;

      if (responseData['success'] == true && responseData['data'] != null) {
        final list = _extractListFromData(responseData['data']);
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

  /// Fetches all open idle teams with available capacity for discovery.
  Future<List<AvailableTeamModel>> getAvailableTeams() async {
    try {
      final response = await _apiClient.dio.get(ApiEndpoints.availableTeams);
      final responseData = response.data as Map<String, dynamic>;

      if (responseData['success'] == true && responseData['data'] != null) {
        final list = _extractListFromData(responseData['data']);
        return list
            .map(
              (item) =>
                  AvailableTeamModel.fromJson(item as Map<String, dynamic>),
            )
            .toList();
      }
      return [];
    } on DioException catch (e) {
      throw Exception(_extractErrorMessage(e));
    }
  }

  /// Adds the current user to an open team (self-join).
  Future<TeamModel> joinTeam({
    required String teamId,
    required String userId,
  }) async {
    try {
      final response = await _apiClient.dio.post(
        ApiEndpoints.teamMembers(teamId),
        data: {'userId': userId},
      );
      final responseData = response.data as Map<String, dynamic>;
      if (responseData['success'] == true && responseData['data'] != null) {
        return TeamModel.fromJson(responseData['data'] as Map<String, dynamic>);
      }
      throw Exception(responseData['message'] ?? 'Failed to join team.');
    } on DioException catch (e) {
      throw Exception(_extractErrorMessage(e));
    }
  }

  /// Provisions a new response team and designates the creator as leader if requested.
  Future<TeamModel> createTeam({
    required String teamName,
    String? leaderId,
  }) async {
    try {
      final response = await _apiClient.dio.post(
        ApiEndpoints.teams,
        data: {
          'teamName': teamName.trim(),
          if (leaderId != null) 'leaderId': leaderId,
        },
      );
      final responseData = response.data as Map<String, dynamic>;
      if (responseData['success'] == true && responseData['data'] != null) {
        return TeamModel.fromJson(responseData['data'] as Map<String, dynamic>);
      }
      throw Exception(responseData['message'] ?? 'Failed to create team.');
    } on DioException catch (e) {
      throw Exception(_extractErrorMessage(e));
    }
  }

  /// Claims vacant team leadership for the calling active member.
  Future<TeamModel> claimLeadership({
    required String teamId,
    required String userId,
    required String teamName,
  }) async {
    try {
      final response = await _apiClient.dio.put(
        ApiEndpoints.teamById(teamId),
        data: {'teamName': teamName, 'leaderId': userId},
      );
      final responseData = response.data as Map<String, dynamic>;
      if (responseData['success'] == true && responseData['data'] != null) {
        return TeamModel.fromJson(responseData['data'] as Map<String, dynamic>);
      }
      throw Exception(responseData['message'] ?? 'Failed to claim leadership.');
    } on DioException catch (e) {
      throw Exception(_extractErrorMessage(e));
    }
  }

  /// Updates team operational name (team leader permission required).
  Future<TeamModel> renameTeam({
    required String teamId,
    required String newTeamName,
    String? leaderId,
  }) async {
    try {
      final response = await _apiClient.dio.put(
        ApiEndpoints.teamById(teamId),
        data: {'teamName': newTeamName.trim(), 'leaderId': leaderId},
      );
      final responseData = response.data as Map<String, dynamic>;
      if (responseData['success'] == true && responseData['data'] != null) {
        return TeamModel.fromJson(responseData['data'] as Map<String, dynamic>);
      }
      throw Exception(responseData['message'] ?? 'Failed to rename team.');
    } on DioException catch (e) {
      throw Exception(_extractErrorMessage(e));
    }
  }

  /// Removes current user from team roster (self-leave off-call).
  Future<void> leaveTeam({
    required String teamId,
    required String userId,
  }) async {
    try {
      final response = await _apiClient.dio.delete(
        ApiEndpoints.teamMember(teamId, userId),
      );
      final responseData = response.data as Map<String, dynamic>;
      if (responseData['success'] != true) {
        throw Exception(responseData['message'] ?? 'Failed to leave team.');
      }
    } on DioException catch (e) {
      throw Exception(_extractErrorMessage(e));
    }
  }

  /// Removes a member from team roster (leader permission required).
  Future<TeamModel> removeMember({
    required String teamId,
    required String memberId,
  }) async {
    try {
      final response = await _apiClient.dio.delete(
        ApiEndpoints.teamMember(teamId, memberId),
      );
      final responseData = response.data as Map<String, dynamic>;
      if (responseData['success'] == true && responseData['data'] != null) {
        return TeamModel.fromJson(responseData['data'] as Map<String, dynamic>);
      }
      throw Exception(
        responseData['message'] ?? 'Failed to remove team member.',
      );
    } on DioException catch (e) {
      throw Exception(_extractErrorMessage(e));
    }
  }

  /// Updates team operational status (Idle / Busy).
  Future<void> updateTeamStatus({
    required String teamId,
    required TeamStatus status,
  }) async {
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

  /// Updates personal duty status of an active responder (Available / OffDuty).
  Future<void> updateMemberStatus({
    required String teamId,
    required String userId,
    required MemberStatus status,
  }) async {
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

  List<dynamic> _extractListFromData(dynamic data) {
    if (data is List<dynamic>) {
      return data;
    } else if (data is Map<String, dynamic> && data['items'] is List<dynamic>) {
      return data['items'] as List<dynamic>;
    }
    return const [];
  }

  String _extractErrorMessage(DioException error) {
    if (error.response?.data != null &&
        error.response!.data is Map<String, dynamic>) {
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
