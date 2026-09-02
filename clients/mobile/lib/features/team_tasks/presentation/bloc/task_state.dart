import 'package:latlong2/latlong.dart';
import '../../data/models/task_detail_model.dart';
import '../../data/models/team_model.dart';

abstract class TaskState {
  const TaskState();
}

class TaskInitial extends TaskState {
  const TaskInitial();
}

class TaskLoading extends TaskState {
  const TaskLoading();
}

class TaskIdle extends TaskState {
  final List<TeamTaskModel> history;

  const TaskIdle({this.history = const []});
}

class TaskActiveLoaded extends TaskState {
  final TeamTaskModel task;
  final List<LatLng> routePoints;
  final double distanceKm;
  final int estimatedMinutes;
  final LatLng? teamLocation;
  final bool isRouteFallback;
  final List<TeamTaskModel> history;

  const TaskActiveLoaded({
    required this.task,
    this.routePoints = const [],
    this.distanceKm = 0.0,
    this.estimatedMinutes = 0,
    this.teamLocation,
    this.isRouteFallback = false,
    this.history = const [],
  });

  TaskActiveLoaded copyWith({
    TeamTaskModel? task,
    List<LatLng>? routePoints,
    double? distanceKm,
    int? estimatedMinutes,
    LatLng? teamLocation,
    bool? isRouteFallback,
    List<TeamTaskModel>? history,
  }) {
    return TaskActiveLoaded(
      task: task ?? this.task,
      routePoints: routePoints ?? this.routePoints,
      distanceKm: distanceKm ?? this.distanceKm,
      estimatedMinutes: estimatedMinutes ?? this.estimatedMinutes,
      teamLocation: teamLocation ?? this.teamLocation,
      isRouteFallback: isRouteFallback ?? this.isRouteFallback,
      history: history ?? this.history,
    );
  }
}

class TaskStatusUpdating extends TaskState {
  final TeamTaskModel currentTask;
  final TeamStatus pendingStatus;

  const TaskStatusUpdating({
    required this.currentTask,
    required this.pendingStatus,
  });
}

class TaskDebriefSubmitting extends TaskState {
  final TeamTaskModel currentTask;

  const TaskDebriefSubmitting({required this.currentTask});
}

class TaskDebriefSuccess extends TaskState {
  final String message;

  const TaskDebriefSuccess(this.message);
}

class TaskFailure extends TaskState {
  final String message;
  final bool isOffline;
  final TeamTaskModel? cachedTask;
  final TeamStatus? failedStatus;

  const TaskFailure(
    this.message, {
    this.isOffline = false,
    this.cachedTask,
    this.failedStatus,
  });
}
