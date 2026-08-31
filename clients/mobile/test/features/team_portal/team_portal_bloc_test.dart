import 'package:flutter_test/flutter_test.dart';
import 'package:socar_dispatch_mobile/core/network/api_client.dart';
import 'package:socar_dispatch_mobile/core/storage/secure_storage_service.dart';
import 'package:socar_dispatch_mobile/features/team_portal/data/models/available_team_model.dart';
import 'package:socar_dispatch_mobile/features/team_portal/data/repositories/team_portal_repository.dart';
import 'package:socar_dispatch_mobile/features/team_portal/presentation/bloc/team_portal_bloc.dart';
import 'package:socar_dispatch_mobile/features/team_portal/presentation/bloc/team_portal_event.dart';
import 'package:socar_dispatch_mobile/features/team_portal/presentation/bloc/team_portal_state.dart';
import 'package:socar_dispatch_mobile/features/team_tasks/data/models/team_model.dart';

class FakeSecureStorage extends SecureStorageService {}

class MockTeamPortalRepository extends TeamPortalRepository {
  MockTeamPortalRepository() : super(apiClient: ApiClient(storageService: FakeSecureStorage()));

  TeamModel? mockUserTeam;
  List<AvailableTeamModel> mockAvailableTeams = [];

  @override
  Future<TeamModel?> getUserTeam(String userId) async => mockUserTeam;

  @override
  Future<List<AvailableTeamModel>> getAvailableTeams() async => mockAvailableTeams;

  @override
  Future<TeamModel> joinTeam({required String teamId, required String userId}) async {
    return TeamModel(
      id: teamId,
      teamName: 'Joined Team',
      status: TeamStatus.idle,
      leaderId: 'leader-1',
      updatedAt: DateTime.now(),
      members: [],
    );
  }

  @override
  Future<void> leaveTeam({required String teamId, required String userId}) async {
    mockUserTeam = null;
  }

  @override
  Future<TeamModel> claimLeadership({
    required String teamId,
    required String userId,
    required String teamName,
  }) async {
    return TeamModel(
      id: teamId,
      teamName: teamName,
      status: TeamStatus.idle,
      leaderId: userId,
      updatedAt: DateTime.now(),
      members: [],
    );
  }
}

void main() {
  group('TeamPortalBloc Unit Tests', () {
    late MockTeamPortalRepository repository;

    setUp(() {
      repository = MockTeamPortalRepository();
    });

    test('emits TeamUnassignedLoaded when user has no active team', () async {
      repository.mockUserTeam = null;
      repository.mockAvailableTeams = [
        AvailableTeamModel(
          id: 'team-1',
          teamName: 'Rapid Response 1',
          memberCount: 2,
          createdAt: DateTime.now(),
        ),
      ];

      final bloc = TeamPortalBloc(repository: repository);

      expectLater(
        bloc.stream,
        emitsInOrder([
          isA<TeamPortalLoading>(),
          isA<TeamUnassignedLoaded>().having(
            (s) => s.availableTeams.length,
            'availableTeams length',
            1,
          ),
        ]),
      );

      bloc.add(const LoadTeamPortal('user-1'));
    });

    test('emits TeamAssignedLoaded after successfully joining open team', () async {
      final bloc = TeamPortalBloc(repository: repository);

      expectLater(
        bloc.stream,
        emitsInOrder([
          isA<TeamPortalActionSuccess>(),
          isA<TeamAssignedLoaded>().having(
            (s) => s.team.id,
            'team id',
            'team-1',
          ),
        ]),
      );

      bloc.add(const JoinTeamRequested(teamId: 'team-1', userId: 'user-1'));
    });

    test('emits TeamUnassignedLoaded after departing active team', () async {
      final bloc = TeamPortalBloc(repository: repository);

      expectLater(
        bloc.stream,
        emitsInOrder([
          isA<TeamPortalActionSuccess>(),
          isA<TeamUnassignedLoaded>(),
        ]),
      );

      bloc.add(const LeaveTeamRequested(teamId: 'team-1', userId: 'user-1'));
    });

    test('emits TeamAssignedLoaded with new leaderId when claiming vacant leadership', () async {
      final bloc = TeamPortalBloc(repository: repository);

      expectLater(
        bloc.stream,
        emitsInOrder([
          isA<TeamPortalActionSuccess>(),
          isA<TeamAssignedLoaded>().having(
            (s) => s.team.leaderId,
            'leaderId',
            'user-1',
          ),
        ]),
      );

      bloc.add(const ClaimLeadershipRequested(
        teamId: 'team-2',
        userId: 'user-1',
        teamName: 'Bravo Unit',
      ));
    });
  });
}
