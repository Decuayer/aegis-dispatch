import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import '../../../../core/constants/app_colors.dart';
import '../../../profile/data/models/user_model.dart';
import '../../../team_tasks/presentation/bloc/task_bloc.dart';
import '../../../team_tasks/presentation/bloc/task_event.dart';
import '../bloc/team_portal_bloc.dart';
import '../bloc/team_portal_event.dart';
import '../bloc/team_portal_state.dart';
import 'team_dashboard_view.dart';
import 'unassigned_team_view.dart';

class TeamHomeView extends StatefulWidget {
  final UserModel user;

  const TeamHomeView({super.key, required this.user});

  @override
  State<TeamHomeView> createState() => _TeamHomeViewState();
}

class _TeamHomeViewState extends State<TeamHomeView> {
  @override
  void initState() {
    super.initState();
    context.read<TeamPortalBloc>().add(LoadTeamPortal(widget.user.id));
  }

  @override
  Widget build(BuildContext context) {
    return BlocConsumer<TeamPortalBloc, TeamPortalState>(
      listenWhen: (prev, current) =>
          current is TeamPortalActionFailure ||
          current is TeamPortalActionSuccess ||
          (prev is TeamUnassignedLoaded && current is TeamAssignedLoaded) ||
          (prev is TeamAssignedLoaded && current is TeamUnassignedLoaded),
      listener: (context, state) {
        if (state is TeamPortalActionFailure) {
          ScaffoldMessenger.of(context).showSnackBar(
            SnackBar(
              content: Text(state.message),
              backgroundColor: AppColors.error,
              duration: const Duration(seconds: 4),
            ),
          );
        } else if (state is TeamPortalActionSuccess) {
          ScaffoldMessenger.of(context).showSnackBar(
            SnackBar(
              content: Text(state.message),
              backgroundColor: AppColors.success,
              duration: const Duration(seconds: 3),
            ),
          );
        }

        if (state is TeamAssignedLoaded) {
          context.read<TaskBloc>().add(LoadActiveTask(state.team.id));
        }
      },
      buildWhen: (prev, current) =>
          current is TeamPortalLoading ||
          current is TeamUnassignedLoaded ||
          current is TeamAssignedLoaded,
      builder: (context, state) {
        if (state is TeamUnassignedLoaded) {
          return UnassignedTeamView(
            user: widget.user,
            availableTeams: state.availableTeams,
            isActionInProgress: state.isActionInProgress,
          );
        }

        if (state is TeamAssignedLoaded) {
          return TeamDashboardView(
            user: widget.user,
            team: state.team,
            isActionInProgress: state.isActionInProgress,
          );
        }

        return const Scaffold(
          backgroundColor: AppColors.background,
          body: Center(
            child: CircularProgressIndicator(),
          ),
        );
      },
    );
  }
}
