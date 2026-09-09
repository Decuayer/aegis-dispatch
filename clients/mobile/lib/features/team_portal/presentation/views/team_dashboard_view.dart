import 'dart:async';
import 'dart:io';
import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import '../../../../core/constants/app_colors.dart';
import '../../../../core/services/local_notification_service.dart';
import '../../../employee_tracking/data/services/employee_tracking_hub_service.dart';
import '../../../profile/data/models/user_model.dart';
import '../../../profile/presentation/views/profile_view.dart';
import '../../../team_tasks/data/models/team_model.dart';
import '../../../team_tasks/presentation/bloc/task_bloc.dart';
import '../../../team_tasks/presentation/bloc/task_event.dart';
import '../../../team_tasks/presentation/bloc/task_state.dart';
import '../../../team_tasks/presentation/views/active_task_view.dart';
import '../../../team_tasks/presentation/views/task_history_view.dart';
import '../../../tracking/presentation/widgets/tracking_status_chip.dart';
import '../bloc/team_portal_bloc.dart';
import '../bloc/team_portal_event.dart';
import '../widgets/claim_leadership_banner.dart';
import '../widgets/member_status_toggle.dart';
import '../widgets/team_roster_tile.dart';

class TeamDashboardView extends StatefulWidget {
  final UserModel user;
  final TeamModel team;
  final bool isActionInProgress;

  const TeamDashboardView({
    super.key,
    required this.user,
    required this.team,
    required this.isActionInProgress,
  });

  @override
  State<TeamDashboardView> createState() => _TeamDashboardViewState();
}

class _TeamDashboardViewState extends State<TeamDashboardView>
    with SingleTickerProviderStateMixin {
  late TabController _tabController;
  StreamSubscription<TeamDispatchedUpdate>? _dispatchedSub;
  StreamSubscription<IncidentStatusUpdate>? _statusSub;

  @override
  void initState() {
    super.initState();
    _tabController = TabController(length: 3, vsync: this);

    _dispatchedSub = context
        .read<EmployeeTrackingHubService>()
        .onTeamDispatched
        .listen((update) {
          if (!mounted) return;
          if (update.teamId.toLowerCase() == widget.team.id.toLowerCase()) {
            LocalNotificationService().showEmergencyNotification(
              id: update.incidentId.hashCode,
              title: 'Acil Görev Ataması / Emergency Dispatch',
              body: 'Müdahale ekibiniz bir acil durum görevine atandı.',
              payload: {
                'teamId': widget.team.id,
                'incidentId': update.incidentId,
              },
            );
            context.read<TaskBloc>().add(
              LoadActiveTask(widget.team.id, isRefresh: true),
            );
            _tabController.animateTo(0);
            context.read<TeamPortalBloc>().add(LoadTeamPortal(widget.user.id));
          }
        });

    _statusSub = context
        .read<EmployeeTrackingHubService>()
        .onIncidentStatusChanged
        .listen((update) {
          if (!mounted) return;
          context.read<TaskBloc>().add(
            LoadActiveTask(widget.team.id, isRefresh: true),
          );
          context.read<TeamPortalBloc>().add(LoadTeamPortal(widget.user.id));
        });
  }

  @override
  void dispose() {
    _dispatchedSub?.cancel();
    _statusSub?.cancel();
    _tabController.dispose();
    super.dispose();
  }

  bool get _isLeader =>
      widget.team.leaderId != null &&
      widget.team.leaderId!.toLowerCase() == widget.user.id.toLowerCase();

  MemberStatus get _currentUserMemberStatus {
    for (var m in widget.team.members) {
      if (m.userId.toLowerCase() == widget.user.id.toLowerCase()) {
        return m.memberStatus;
      }
    }
    return MemberStatus.available;
  }

  void _showRenameDialog(BuildContext context) {
    final controller = TextEditingController(text: widget.team.teamName);
    showDialog(
      context: context,
      builder:
          (ctx) => AlertDialog(
            title: const Text('Rename Response Team'),
            content: TextField(
              controller: controller,
              autofocus: true,
              decoration: const InputDecoration(
                labelText: 'New Team Name',
                border: OutlineInputBorder(),
              ),
            ),
            actions: [
              TextButton(
                onPressed: () => Navigator.pop(ctx),
                child: const Text('Cancel'),
              ),
              FilledButton(
                onPressed: () {
                  final newName = controller.text.trim();
                  if (newName.isNotEmpty && newName != widget.team.teamName) {
                    Navigator.pop(ctx);
                    context.read<TeamPortalBloc>().add(
                      RenameTeamRequested(
                        teamId: widget.team.id,
                        newTeamName: newName,
                        leaderId: widget.team.leaderId,
                      ),
                    );
                  }
                },
                child: const Text('Save'),
              ),
            ],
          ),
    );
  }

  void _handleLeaveTeam(BuildContext context) {
    final taskState = context.read<TaskBloc>().state;
    final hasActiveDispatch = taskState is TaskActiveLoaded;

    if (hasActiveDispatch || widget.team.status != TeamStatus.idle) {
      showDialog(
        context: context,
        builder:
            (ctx) => AlertDialog(
              icon: const Icon(
                Icons.warning_amber_rounded,
                color: AppColors.error,
                size: 36,
              ),
              title: const Text('Active Emergency Dispatch'),
              content: const Text(
                'Cannot leave unit while an emergency dispatch mission is active. Complete or hand over the mission before departing.',
              ),
              actions: [
                FilledButton(
                  onPressed: () => Navigator.pop(ctx),
                  child: const Text('Understood'),
                ),
              ],
            ),
      );
      return;
    }

    showDialog(
      context: context,
      builder:
          (ctx) => AlertDialog(
            title: const Text('Leave Team'),
            content: Text(
              'Are you sure you want to depart from "${widget.team.teamName}"?',
            ),
            actions: [
              TextButton(
                onPressed: () => Navigator.pop(ctx),
                child: const Text('Cancel'),
              ),
              FilledButton(
                onPressed: () {
                  Navigator.pop(ctx);
                  context.read<TeamPortalBloc>().add(
                    LeaveTeamRequested(
                      teamId: widget.team.id,
                      userId: widget.user.id,
                    ),
                  );
                },
                style: FilledButton.styleFrom(backgroundColor: AppColors.error),
                child: const Text('Leave Unit'),
              ),
            ],
          ),
    );
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: AppColors.background,
      appBar: AppBar(
        title: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(widget.team.teamName, style: const TextStyle(fontSize: 17)),
            Text(
              'Unit Status: ${widget.team.status.label}',
              style: TextStyle(fontSize: 11, color: widget.team.status.color),
            ),
          ],
        ),
        bottom: TabBar(
          controller: _tabController,
          labelColor: AppColors.primary,
          indicatorColor: AppColors.primary,
          tabs: const [
            Tab(icon: Icon(Icons.emergency_outlined), text: 'Active Task'),
            Tab(icon: Icon(Icons.groups_outlined), text: 'Unit Roster'),
            Tab(icon: Icon(Icons.history_outlined), text: 'Task History'),
          ],
        ),
        actions: [
          BlocBuilder<TaskBloc, TaskState>(
            builder: (context, state) {
              final isTracking = state is TaskActiveLoaded;
              return Padding(
                padding: const EdgeInsets.symmetric(
                  vertical: 12,
                  horizontal: 4,
                ),
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
      body: TabBarView(
        controller: _tabController,
        children: [
          _buildActiveTaskTab(context),
          _buildRosterTab(context),
          _buildHistoryTab(context),
        ],
      ),
    );
  }

  Widget _buildActiveTaskTab(BuildContext context) {
    return BlocConsumer<TaskBloc, TaskState>(
      listenWhen:
          (prev, current) =>
              current is TaskFailure || current is TaskDebriefSuccess,
      listener: (context, state) {
        if (state is TaskFailure) {
          ScaffoldMessenger.of(context).hideCurrentSnackBar();
          ScaffoldMessenger.of(context).showSnackBar(
            SnackBar(
              behavior: SnackBarBehavior.floating,
              content: Text(state.message),
              backgroundColor: AppColors.error,
              duration: const Duration(seconds: 4),
              action:
                  state.failedStatus != null
                      ? SnackBarAction(
                        label: 'Retry',
                        textColor: Colors.white,
                        onPressed: () {
                          context.read<TaskBloc>().add(
                            UpdateOperationalStatus(
                              teamId: widget.team.id,
                              userId: widget.user.id,
                              newStatus: state.failedStatus!,
                            ),
                          );
                        },
                      )
                      : null,
            ),
          );
        } else if (state is TaskDebriefSuccess) {
          ScaffoldMessenger.of(context).showSnackBar(
            SnackBar(
              behavior: SnackBarBehavior.floating,
              content: Text(state.message),
              backgroundColor: AppColors.success,
              duration: const Duration(seconds: 3),
            ),
          );
        }
      },
      builder: (context, state) {
        if (state is TaskLoading) {
          return const Center(child: CircularProgressIndicator());
        }

        if (state is TaskActiveLoaded || state is TaskStatusUpdating) {
          final task =
              state is TaskActiveLoaded
                  ? state.task
                  : (state as TaskStatusUpdating).currentTask;
          final routePoints =
              state is TaskActiveLoaded ? state.routePoints : const [];
          final distanceKm = state is TaskActiveLoaded ? state.distanceKm : 0.0;
          final estimatedMinutes =
              state is TaskActiveLoaded ? state.estimatedMinutes : 0;
          final teamLocation =
              state is TaskActiveLoaded ? state.teamLocation : null;
          final isRouteFallback =
              state is TaskActiveLoaded ? state.isRouteFallback : false;

          return ActiveTaskView(
            task: task,
            routePoints: routePoints.cast(),
            distanceKm: distanceKm,
            estimatedMinutes: estimatedMinutes,
            teamLocation: teamLocation,
            isRouteFallback: isRouteFallback,
            isStatusUpdating: state is TaskStatusUpdating,
            onStatusChange: (newStatus) {
              context.read<TaskBloc>().add(
                UpdateOperationalStatus(
                  teamId: widget.team.id,
                  userId: widget.user.id,
                  newStatus: newStatus,
                ),
              );
            },
            onDebriefSubmit: (notes, photo) async {
              context.read<TaskBloc>().add(
                SubmitTaskDebrief(
                  incidentId: task.id,
                  teamId: widget.team.id,
                  notes: notes,
                  photo: photo is File ? photo : null,
                ),
              );
            },
          );
        }

        return _buildIdleStandbyView(context);
      },
    );
  }

  Widget _buildIdleStandbyView(BuildContext context) {
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
              child: const Icon(
                Icons.verified_outlined,
                color: AppColors.secondary,
                size: 64,
              ),
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
                context.read<TaskBloc>().add(
                  LoadActiveTask(widget.team.id, isRefresh: true),
                );
              },
              icon: const Icon(Icons.refresh),
              label: const Text('Check for Assignments'),
            ),
          ],
        ),
      ),
    );
  }

  Widget _buildRosterTab(BuildContext context) {
    return ListView(
      padding: const EdgeInsets.symmetric(vertical: 12),
      children: [
        if (widget.team.leaderId == null)
          ClaimLeadershipBanner(
            isClaiming: widget.isActionInProgress,
            onClaim: () {
              context.read<TeamPortalBloc>().add(
                ClaimLeadershipRequested(
                  teamId: widget.team.id,
                  userId: widget.user.id,
                  teamName: widget.team.teamName,
                ),
              );
            },
          ),
        if (_isLeader) _buildLeaderAdminCard(context),
        MemberStatusToggle(
          currentStatus: _currentUserMemberStatus,
          onStatusChanged: (newStatus) {
            context.read<TeamPortalBloc>().add(
              ToggleMemberStatusRequested(
                teamId: widget.team.id,
                userId: widget.user.id,
                status: newStatus,
              ),
            );
          },
        ),
        Padding(
          padding: const EdgeInsets.fromLTRB(16, 12, 16, 4),
          child: Row(
            mainAxisAlignment: MainAxisAlignment.spaceBetween,
            children: [
              Text(
                'Unit Personnel (${widget.team.members.length} / 6)',
                style: const TextStyle(
                  fontSize: 15,
                  fontWeight: FontWeight.bold,
                  color: AppColors.textPrimary,
                ),
              ),
            ],
          ),
        ),
        ...widget.team.members.map(
          (m) => TeamRosterTile(
            member: m,
            isLeader:
                m.userId.toLowerCase() == widget.team.leaderId?.toLowerCase(),
            isCurrentUser:
                m.userId.toLowerCase() == widget.user.id.toLowerCase(),
            canManage: _isLeader,
            onRemove: () {
              context.read<TeamPortalBloc>().add(
                RemoveMemberRequested(
                  teamId: widget.team.id,
                  memberId: m.userId,
                ),
              );
            },
          ),
        ),
        const SizedBox(height: 24),
        Padding(
          padding: const EdgeInsets.symmetric(horizontal: 16),
          child: OutlinedButton.icon(
            onPressed: () => _handleLeaveTeam(context),
            style: OutlinedButton.styleFrom(
              foregroundColor: AppColors.error,
              side: const BorderSide(color: AppColors.error),
              padding: const EdgeInsets.symmetric(vertical: 12),
              shape: RoundedRectangleBorder(
                borderRadius: BorderRadius.circular(10),
              ),
            ),
            icon: const Icon(Icons.logout_outlined),
            label: const Text(
              'Leave Response Team',
              style: TextStyle(fontWeight: FontWeight.bold),
            ),
          ),
        ),
        const SizedBox(height: 32),
      ],
    );
  }

  Widget _buildLeaderAdminCard(BuildContext context) {
    final isIdle = widget.team.status == TeamStatus.idle;

    return Container(
      margin: const EdgeInsets.symmetric(horizontal: 16, vertical: 8),
      padding: const EdgeInsets.all(16),
      decoration: BoxDecoration(
        color: AppColors.surface,
        borderRadius: BorderRadius.circular(14),
        border: Border.all(color: AppColors.primary.withValues(alpha: 0.2)),
        boxShadow: [
          BoxShadow(
            color: AppColors.primary.withValues(alpha: 0.04),
            blurRadius: 8,
            offset: const Offset(0, 2),
          ),
        ],
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            mainAxisAlignment: MainAxisAlignment.spaceBetween,
            children: [
              const Row(
                children: [
                  Icon(
                    Icons.admin_panel_settings_outlined,
                    color: AppColors.primary,
                    size: 22,
                  ),
                  SizedBox(width: 8),
                  Text(
                    'Leader Administration',
                    style: TextStyle(fontSize: 15, fontWeight: FontWeight.bold),
                  ),
                ],
              ),
              IconButton(
                icon: const Icon(Icons.edit_outlined, size: 20),
                tooltip: 'Rename Team',
                onPressed: () => _showRenameDialog(context),
              ),
            ],
          ),
          const Divider(height: 16),
          Row(
            mainAxisAlignment: MainAxisAlignment.spaceBetween,
            children: [
              Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  const Text(
                    'Unit Operational Readiness',
                    style: TextStyle(fontSize: 13, fontWeight: FontWeight.w600),
                  ),
                  Text(
                    isIdle
                        ? 'Available for new dispatches'
                        : 'Marked as Busy / Inactive',
                    style: const TextStyle(
                      fontSize: 11,
                      color: AppColors.textSecondary,
                    ),
                  ),
                ],
              ),
              Switch.adaptive(
                value: isIdle,
                activeTrackColor: AppColors.secondary,
                onChanged: (val) {
                  context.read<TeamPortalBloc>().add(
                    ToggleTeamStatusRequested(
                      teamId: widget.team.id,
                      status: val ? TeamStatus.idle : TeamStatus.busy,
                    ),
                  );
                },
              ),
            ],
          ),
        ],
      ),
    );
  }

  Widget _buildHistoryTab(BuildContext context) {
    return BlocBuilder<TaskBloc, TaskState>(
      builder: (context, state) {
        List historyList = [];
        if (state is TaskActiveLoaded) {
          historyList = state.history;
        } else if (state is TaskIdle) {
          historyList = state.history;
        }

        return TaskHistoryView(
          history: historyList.cast(),
          onRefresh: () {
            context.read<TaskBloc>().add(LoadTaskHistory(widget.team.id));
          },
        );
      },
    );
  }
}
