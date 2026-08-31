import '../../../team_tasks/data/models/team_model.dart';

abstract class TeamPortalEvent {
  const TeamPortalEvent();
}

/// Initial check or pull-to-refresh to resolve user's team membership state.
class LoadTeamPortal extends TeamPortalEvent {
  final String userId;
  final bool isRefresh;

  const LoadTeamPortal(this.userId, {this.isRefresh = false});
}

/// Pull-to-refresh action on the unassigned teams discovery screen.
class RefreshAvailableTeams extends TeamPortalEvent {
  const RefreshAvailableTeams();
}

/// Dispatched when an unassigned responder taps 'Join Team'.
class JoinTeamRequested extends TeamPortalEvent {
  final String teamId;
  final String userId;

  const JoinTeamRequested({
    required this.teamId,
    required this.userId,
  });
}

/// Dispatched when submitting the 'Create New Team' form.
class CreateTeamRequested extends TeamPortalEvent {
  final String teamName;
  final bool designateAsLeader;
  final String userId;

  const CreateTeamRequested({
    required this.teamName,
    required this.designateAsLeader,
    required this.userId,
  });
}

/// Dispatched when an active member claims a vacant team leadership.
class ClaimLeadershipRequested extends TeamPortalEvent {
  final String teamId;
  final String userId;
  final String teamName;

  const ClaimLeadershipRequested({
    required this.teamId,
    required this.userId,
    required this.teamName,
  });
}

/// Dispatched by the team leader to rename the team.
class RenameTeamRequested extends TeamPortalEvent {
  final String teamId;
  final String newTeamName;
  final String? leaderId;

  const RenameTeamRequested({
    required this.teamId,
    required this.newTeamName,
    this.leaderId,
  });
}

/// Dispatched when a member requests to leave their active team off-call.
class LeaveTeamRequested extends TeamPortalEvent {
  final String teamId;
  final String userId;

  const LeaveTeamRequested({
    required this.teamId,
    required this.userId,
  });
}

/// Dispatched by the team leader to dismiss a member from the roster.
class RemoveMemberRequested extends TeamPortalEvent {
  final String teamId;
  final String memberId;

  const RemoveMemberRequested({
    required this.teamId,
    required this.memberId,
  });
}

/// Dispatched by the leader to toggle readiness between Idle and Busy.
class ToggleTeamStatusRequested extends TeamPortalEvent {
  final String teamId;
  final TeamStatus status;

  const ToggleTeamStatusRequested({
    required this.teamId,
    required this.status,
  });
}

/// Dispatched by an active member to toggle personal duty (Available / OffDuty).
class ToggleMemberStatusRequested extends TeamPortalEvent {
  final String teamId;
  final String userId;
  final MemberStatus status;

  const ToggleMemberStatusRequested({
    required this.teamId,
    required this.userId,
    required this.status,
  });
}
