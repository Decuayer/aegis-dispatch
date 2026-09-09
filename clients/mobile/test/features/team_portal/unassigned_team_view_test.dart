import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:socar_dispatch_mobile/core/network/api_client.dart';
import 'package:socar_dispatch_mobile/core/storage/secure_storage_service.dart';
import 'package:socar_dispatch_mobile/features/profile/data/models/user_model.dart';
import 'package:socar_dispatch_mobile/features/team_portal/data/models/available_team_model.dart';
import 'package:socar_dispatch_mobile/features/team_portal/data/repositories/team_portal_repository.dart';
import 'package:socar_dispatch_mobile/features/team_portal/presentation/bloc/team_portal_bloc.dart';
import 'package:socar_dispatch_mobile/features/team_portal/presentation/views/unassigned_team_view.dart';

class FakeSecureStorage extends SecureStorageService {}

void main() {
  group('UnassignedTeamView Widget Tests', () {
    const testUser = UserModel(
      id: 'user-1',
      email: 'team@socar.az',
      phone: '+994501234567',
      firstName: 'Demir',
      lastName: 'Cucu',
      roleType: RoleType.team,
      department: 'Fire Safety',
    );

    testWidgets(
      'renders available teams list and create team button correctly',
      (tester) async {
        final repository = TeamPortalRepository(
          apiClient: ApiClient(storageService: FakeSecureStorage()),
        );
        final bloc = TeamPortalBloc(repository: repository);

        final availableTeams = [
          AvailableTeamModel(
            id: 'team-alpha',
            teamName: 'Alpha Fire Unit',
            leaderId: 'leader-john',
            leaderFullName: 'Commander John',
            memberCount: 4,
            createdAt: DateTime.now(),
          ),
        ];

        await tester.pumpWidget(
          MaterialApp(
            home: BlocProvider.value(
              value: bloc,
              child: UnassignedTeamView(
                user: testUser,
                availableTeams: availableTeams,
                isActionInProgress: false,
              ),
            ),
          ),
        );

        expect(find.text('Unassigned Responder'), findsOneWidget);
        expect(find.text('Alpha Fire Unit'), findsOneWidget);
        expect(find.text('Leader: Commander John'), findsOneWidget);
        expect(find.text('4 / 6 Members'), findsOneWidget);
        expect(find.text('Create Team'), findsOneWidget);
        expect(find.text('Join Team'), findsOneWidget);
      },
    );
  });
}
