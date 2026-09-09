import '../../../team_tasks/data/models/team_model.dart';
import '../../data/models/available_team_model.dart';

abstract class TeamPortalState {
  const TeamPortalState();
}

class TeamPortalInitial extends TeamPortalState {
  const TeamPortalInitial();
}

class TeamPortalLoading extends TeamPortalState {
  const TeamPortalLoading();
}

/// User has no active team. Shows discovery list and team creation entry.
class TeamUnassignedLoaded extends TeamPortalState {
  final List<AvailableTeamModel> availableTeams;
  final bool isActionInProgress;

  const TeamUnassignedLoaded({
    required this.availableTeams,
    this.isActionInProgress = false,
  });

  TeamUnassignedLoaded copyWith({
    List<AvailableTeamModel>? availableTeams,
    bool? isActionInProgress,
  }) {
    return TeamUnassignedLoaded(
      availableTeams: availableTeams ?? this.availableTeams,
      isActionInProgress: isActionInProgress ?? this.isActionInProgress,
    );
  }
}

/// User belongs to an active team. Shows dashboard, roster, and operational tools.
class TeamAssignedLoaded extends TeamPortalState {
  final TeamModel team;
  final bool isActionInProgress;

  const TeamAssignedLoaded({
    required this.team,
    this.isActionInProgress = false,
  });

  TeamAssignedLoaded copyWith({TeamModel? team, bool? isActionInProgress}) {
    return TeamAssignedLoaded(
      team: team ?? this.team,
      isActionInProgress: isActionInProgress ?? this.isActionInProgress,
    );
  }
}

/// Temporary feedback state emitted on operational errors for SnackBar alerts.
class TeamPortalActionFailure extends TeamPortalState {
  final String message;

  const TeamPortalActionFailure(this.message);
}

/// Temporary feedback state emitted on successful actions for SnackBar alerts.
class TeamPortalActionSuccess extends TeamPortalState {
  final String message;

  const TeamPortalActionSuccess(this.message);
}
