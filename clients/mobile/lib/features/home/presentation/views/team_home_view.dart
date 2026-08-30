import 'dart:io';
import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import '../../../../core/constants/app_colors.dart';
import '../../../profile/data/models/user_model.dart';
import '../../../profile/presentation/views/profile_view.dart';
import '../../../team_tasks/data/models/team_model.dart';
import '../../../team_tasks/data/repositories/task_repository.dart';
import '../../../team_tasks/presentation/bloc/task_bloc.dart';
import '../../../team_tasks/presentation/bloc/task_event.dart';
import '../../../team_tasks/presentation/bloc/task_state.dart';
import '../../../team_tasks/presentation/views/active_task_view.dart';
import '../../../team_tasks/presentation/views/task_history_view.dart';
import '../../../tracking/presentation/widgets/tracking_status_chip.dart';


class TeamHomeView extends StatefulWidget {
  final UserModel user;

  const TeamHomeView({super.key, required this.user});

  @override
  State<TeamHomeView> createState() => _TeamHomeViewState();
}

class _TeamHomeViewState extends State<TeamHomeView> with SingleTickerProviderStateMixin {
  late TabController _tabController;
  String? _teamId;

  @override
  void initState() {
    super.initState();
    _tabController = TabController(length: 2, vsync: this);
    _initializeTeam();
  }

  Future<void> _initializeTeam() async {
    final taskRepo = context.read<TaskRepository>();
    final team = await taskRepo.getTeamForUser(widget.user.id);
    if (team != null && mounted) {
      setState(() => _teamId = team.id);
      context.read<TaskBloc>().add(LoadActiveTask(team.id));
    }
  }

  @override
  void dispose() {
    _tabController.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: AppColors.background,
      appBar: AppBar(
        title: const Text('Response Team Portal'),
        bottom: TabBar(
          controller: _tabController,
          labelColor: AppColors.primary,
          indicatorColor: AppColors.primary,
          tabs: const [
            Tab(icon: Icon(Icons.emergency_outlined), text: 'Active Task'),
            Tab(icon: Icon(Icons.history_outlined), text: 'Task History'),
          ],
        ),
        actions: [
          BlocBuilder<TaskBloc, TaskState>(
            builder: (context, state) {
              final isTracking = state is TaskActiveLoaded;
              return Padding(
                padding: const EdgeInsets.symmetric(vertical: 12, horizontal: 4),
                child: TrackingStatusChip(isTracking: isTracking),
              );
            },
          ),
          IconButton(
            icon: const Icon(Icons.account_circle_outlined, size: 28),
            tooltip: 'Profile',
            onPressed: () {
              Navigator.push(
                context,
                MaterialPageRoute(
                  builder: (ctx) => ProfileView(currentUser: widget.user),
                ),
              );
            },
          ),
        ],
      ),
      body: BlocConsumer<TaskBloc, TaskState>(
        listenWhen: (previous, current) {
          // Trigger when status changes between ActiveLoaded states
          if (previous is TaskActiveLoaded && current is TaskActiveLoaded) {
            return previous.task.status != current.task.status;
          }
          return current is TaskFailure || current is TaskDebriefSuccess;
        },
        listener: (context, state) {
          if (state is TaskFailure) {
            ScaffoldMessenger.of(context).showSnackBar(
              SnackBar(content: Text(state.message), backgroundColor: AppColors.error),
            );
          } else if (state is TaskDebriefSuccess) {
            ScaffoldMessenger.of(context).showSnackBar(
              SnackBar(content: Text(state.message), backgroundColor: AppColors.success),
            );
          } else if (state is TaskActiveLoaded) {
            final currentStatus = TeamStatus.fromString(state.task.status);
            ScaffoldMessenger.of(context).showSnackBar(
              SnackBar(
                content: Text('Status updated: ${currentStatus.label}'),
                backgroundColor: AppColors.primary,
                duration: const Duration(seconds: 2),
              ),
            );
          }
        },
        builder: (context, state) {
          return TabBarView(
            controller: _tabController,
            children: [
              _buildActiveTaskTab(context, state),
              _buildHistoryTab(context, state),
            ],
          );
        },
      ),
    );
  }

  Widget _buildActiveTaskTab(BuildContext context, TaskState state) {
    if (state is TaskLoading) {
      return const Center(child: CircularProgressIndicator());
    }

    if (state is TaskActiveLoaded || state is TaskStatusUpdating) {
      final task = state is TaskActiveLoaded
          ? state.task
          : (state as TaskStatusUpdating).currentTask;
      final routePoints = state is TaskActiveLoaded ? state.routePoints : const [];
      final distanceKm = state is TaskActiveLoaded ? state.distanceKm : 0.0;
      final estimatedMinutes = state is TaskActiveLoaded ? state.estimatedMinutes : 0;
      final teamLocation = state is TaskActiveLoaded ? state.teamLocation : null;
      final isRouteFallback = state is TaskActiveLoaded ? state.isRouteFallback : false;

      return ActiveTaskView(
        task: task,
        routePoints: routePoints.cast(),
        distanceKm: distanceKm,
        estimatedMinutes: estimatedMinutes,
        teamLocation: teamLocation,
        isRouteFallback: isRouteFallback,
        isStatusUpdating: state is TaskStatusUpdating,
        onStatusChange: (newStatus) {
          if (_teamId != null) {
            context.read<TaskBloc>().add(
                  UpdateOperationalStatus(
                    teamId: _teamId!,
                    userId: widget.user.id,
                    newStatus: newStatus,
                  ),
                );
          }
        },
        onDebriefSubmit: (notes, photo) async {
          if (_teamId != null) {
            context.read<TaskBloc>().add(
                  SubmitTaskDebrief(
                    incidentId: task.id,
                    teamId: _teamId!,
                    notes: notes,
                    photo: photo is File ? photo : null,
                  ),
                );
          }
        },
      );
    }

    return _buildIdleStandbyView();
  }

  Widget _buildHistoryTab(BuildContext context, TaskState state) {
    List historyList = [];
    if (state is TaskActiveLoaded) {
      historyList = state.history;
    } else if (state is TaskIdle) {
      historyList = state.history;
    }

    return TaskHistoryView(
      history: historyList.cast(),
      onRefresh: () {
        if (_teamId != null) {
          context.read<TaskBloc>().add(LoadTaskHistory(_teamId!));
        }
      },
    );
  }

  Widget _buildIdleStandbyView() {
    return Center(
      child: Padding(
        padding: const EdgeInsets.all(24),
        child: Column(
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            Container(
              padding: const EdgeInsets.all(20),
              decoration: BoxDecoration(
                color: AppColors.secondary.withValues(alpha: 0.1),
                shape: BoxShape.circle,
              ),
              child: const Icon(Icons.verified_outlined, color: AppColors.secondary, size: 64),
            ),
            const SizedBox(height: 20),
            const Text(
              'Standing By & Available',
              style: TextStyle(fontSize: 20, fontWeight: FontWeight.bold),
            ),
            const SizedBox(height: 8),
            const Text(
              'No active emergency dispatches assigned to your unit. Standing by for incoming calls.',
              textAlign: TextAlign.center,
              style: TextStyle(color: AppColors.textSecondary, height: 1.4),
            ),
            const SizedBox(height: 24),
            OutlinedButton.icon(
              onPressed: () {
                if (_teamId != null) {
                  context.read<TaskBloc>().add(LoadActiveTask(_teamId!, isRefresh: true));
                } else {
                  _initializeTeam();
                }
              },
              icon: const Icon(Icons.refresh),
              label: const Text('Check for Assignments'),
            ),
          ],
        ),
      ),
    );
  }
}
