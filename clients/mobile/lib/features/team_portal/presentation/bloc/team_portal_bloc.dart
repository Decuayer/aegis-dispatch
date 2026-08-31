import 'package:flutter_bloc/flutter_bloc.dart';
import '../../../team_tasks/data/models/team_model.dart';
import '../../data/repositories/team_portal_repository.dart';
import 'team_portal_event.dart';
import 'team_portal_state.dart';

class TeamPortalBloc extends Bloc<TeamPortalEvent, TeamPortalState> {
  final TeamPortalRepository _repository;

  TeamPortalBloc({required TeamPortalRepository repository})
      : _repository = repository,
        super(const TeamPortalInitial()) {
    on<LoadTeamPortal>(_onLoadTeamPortal);
    on<RefreshAvailableTeams>(_onRefreshAvailableTeams);
    on<JoinTeamRequested>(_onJoinTeamRequested);
    on<CreateTeamRequested>(_onCreateTeamRequested);
    on<ClaimLeadershipRequested>(_onClaimLeadershipRequested);
    on<RenameTeamRequested>(_onRenameTeamRequested);
    on<LeaveTeamRequested>(_onLeaveTeamRequested);
    on<RemoveMemberRequested>(_onRemoveMemberRequested);
    on<ToggleTeamStatusRequested>(_onToggleTeamStatusRequested);
    on<ToggleMemberStatusRequested>(_onToggleMemberStatusRequested);
  }

  Future<void> _onLoadTeamPortal(
    LoadTeamPortal event,
    Emitter<TeamPortalState> emit,
  ) async {
    if (!event.isRefresh) {
      emit(const TeamPortalLoading());
    }

    try {
      final userTeam = await _repository.getUserTeam(event.userId);
      if (userTeam != null) {
        emit(TeamAssignedLoaded(team: userTeam));
      } else {
        final availableTeams = await _repository.getAvailableTeams();
        emit(TeamUnassignedLoaded(availableTeams: availableTeams));
      }
    } catch (e) {
      emit(TeamPortalActionFailure(_cleanErrorMessage(e)));
      emit(const TeamUnassignedLoaded(availableTeams: []));
    }
  }

  Future<void> _onRefreshAvailableTeams(
    RefreshAvailableTeams event,
    Emitter<TeamPortalState> emit,
  ) async {
    try {
      final availableTeams = await _repository.getAvailableTeams();
      emit(TeamUnassignedLoaded(availableTeams: availableTeams));
    } catch (e) {
      emit(TeamPortalActionFailure(_cleanErrorMessage(e)));
    }
  }

  Future<void> _onJoinTeamRequested(
    JoinTeamRequested event,
    Emitter<TeamPortalState> emit,
  ) async {
    final previousState = state;
    if (state is TeamUnassignedLoaded) {
      emit((state as TeamUnassignedLoaded).copyWith(isActionInProgress: true));
    }

    try {
      final team = await _repository.joinTeam(
        teamId: event.teamId,
        userId: event.userId,
      );
      emit(TeamPortalActionSuccess('Successfully joined ${team.teamName}.'));
      emit(TeamAssignedLoaded(team: team));
    } catch (e) {
      emit(TeamPortalActionFailure(_cleanErrorMessage(e)));
      if (previousState is TeamUnassignedLoaded) {
        emit(previousState.copyWith(isActionInProgress: false));
      }
    }
  }

  Future<void> _onCreateTeamRequested(
    CreateTeamRequested event,
    Emitter<TeamPortalState> emit,
  ) async {
    final previousState = state;
    if (state is TeamUnassignedLoaded) {
      emit((state as TeamUnassignedLoaded).copyWith(isActionInProgress: true));
    }

    try {
      final leaderId = event.designateAsLeader ? event.userId : null;
      final team = await _repository.createTeam(
        teamName: event.teamName,
        leaderId: leaderId,
      );
      emit(TeamPortalActionSuccess('Team "${team.teamName}" created successfully.'));
      emit(TeamAssignedLoaded(team: team));
    } catch (e) {
      emit(TeamPortalActionFailure(_cleanErrorMessage(e)));
      if (previousState is TeamUnassignedLoaded) {
        emit(previousState.copyWith(isActionInProgress: false));
      }
    }
  }

  Future<void> _onClaimLeadershipRequested(
    ClaimLeadershipRequested event,
    Emitter<TeamPortalState> emit,
  ) async {
    final previousState = state;
    if (state is TeamAssignedLoaded) {
      emit((state as TeamAssignedLoaded).copyWith(isActionInProgress: true));
    }

    try {
      final team = await _repository.claimLeadership(
        teamId: event.teamId,
        userId: event.userId,
        teamName: event.teamName,
      );
      emit(TeamPortalActionSuccess('You have claimed leadership of ${team.teamName}.'));
      emit(TeamAssignedLoaded(team: team));
    } catch (e) {
      emit(TeamPortalActionFailure(_cleanErrorMessage(e)));
      if (previousState is TeamAssignedLoaded) {
        emit(previousState.copyWith(isActionInProgress: false));
      }
    }
  }

  Future<void> _onRenameTeamRequested(
    RenameTeamRequested event,
    Emitter<TeamPortalState> emit,
  ) async {
    final previousState = state;
    if (state is TeamAssignedLoaded) {
      emit((state as TeamAssignedLoaded).copyWith(isActionInProgress: true));
    }

    try {
      final team = await _repository.renameTeam(
        teamId: event.teamId,
        newTeamName: event.newTeamName,
        leaderId: event.leaderId,
      );
      emit(TeamPortalActionSuccess('Team renamed to "${team.teamName}".'));
      emit(TeamAssignedLoaded(team: team));
    } catch (e) {
      emit(TeamPortalActionFailure(_cleanErrorMessage(e)));
      if (previousState is TeamAssignedLoaded) {
        emit(previousState.copyWith(isActionInProgress: false));
      }
    }
  }

  Future<void> _onLeaveTeamRequested(
    LeaveTeamRequested event,
    Emitter<TeamPortalState> emit,
  ) async {
    final previousState = state;
    if (state is TeamAssignedLoaded) {
      emit((state as TeamAssignedLoaded).copyWith(isActionInProgress: true));
    }

    try {
      await _repository.leaveTeam(
        teamId: event.teamId,
        userId: event.userId,
      );
      emit(const TeamPortalActionSuccess('You have left the team.'));
      final availableTeams = await _repository.getAvailableTeams();
      emit(TeamUnassignedLoaded(availableTeams: availableTeams));
    } catch (e) {
      emit(TeamPortalActionFailure(_cleanErrorMessage(e)));
      if (previousState is TeamAssignedLoaded) {
        emit(previousState.copyWith(isActionInProgress: false));
      }
    }
  }

  Future<void> _onRemoveMemberRequested(
    RemoveMemberRequested event,
    Emitter<TeamPortalState> emit,
  ) async {
    final previousState = state;
    if (state is TeamAssignedLoaded) {
      emit((state as TeamAssignedLoaded).copyWith(isActionInProgress: true));
    }

    try {
      final updatedTeam = await _repository.removeMember(
        teamId: event.teamId,
        memberId: event.memberId,
      );
      emit(const TeamPortalActionSuccess('Team member removed successfully.'));
      emit(TeamAssignedLoaded(team: updatedTeam));
    } catch (e) {
      emit(TeamPortalActionFailure(_cleanErrorMessage(e)));
      if (previousState is TeamAssignedLoaded) {
        emit(previousState.copyWith(isActionInProgress: false));
      }
    }
  }

  Future<void> _onToggleTeamStatusRequested(
    ToggleTeamStatusRequested event,
    Emitter<TeamPortalState> emit,
  ) async {
    if (state is! TeamAssignedLoaded) return;
    final currentAssigned = state as TeamAssignedLoaded;

    try {
      await _repository.updateTeamStatus(
        teamId: event.teamId,
        status: event.status,
      );
      final updatedTeam = currentAssigned.team.copyWith(status: event.status);
      emit(TeamPortalActionSuccess('Team readiness updated to ${event.status.label}.'));
      emit(TeamAssignedLoaded(team: updatedTeam));
    } catch (e) {
      emit(TeamPortalActionFailure(_cleanErrorMessage(e)));
      emit(currentAssigned);
    }
  }

  Future<void> _onToggleMemberStatusRequested(
    ToggleMemberStatusRequested event,
    Emitter<TeamPortalState> emit,
  ) async {
    if (state is! TeamAssignedLoaded) return;
    final currentAssigned = state as TeamAssignedLoaded;

    try {
      await _repository.updateMemberStatus(
        teamId: event.teamId,
        userId: event.userId,
        status: event.status,
      );

      final updatedMembers = currentAssigned.team.members.map((m) {
        if (m.userId.toLowerCase() == event.userId.toLowerCase()) {
          return TeamMemberModel(
            userId: m.userId,
            fullName: m.fullName,
            email: m.email,
            phone: m.phone,
            department: m.department,
            subRole: m.subRole,
            memberStatus: event.status,
            statusUpdatedAt: DateTime.now(),
            joinedAt: m.joinedAt,
          );
        }
        return m;
      }).toList();

      final updatedTeam = currentAssigned.team.copyWith(members: updatedMembers);
      emit(TeamPortalActionSuccess('Duty status updated to ${event.status.label}.'));
      emit(TeamAssignedLoaded(team: updatedTeam));
    } catch (e) {
      emit(TeamPortalActionFailure(_cleanErrorMessage(e)));
      emit(currentAssigned);
    }
  }

  String _cleanErrorMessage(dynamic error) {
    return error.toString().replaceAll('Exception: ', '').trim();
  }
}
