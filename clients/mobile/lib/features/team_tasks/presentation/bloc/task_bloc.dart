import 'dart:async';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:latlong2/latlong.dart';
import '../../../incident_reporting/services/location_service.dart';
import '../../../tracking/data/location_stream_repository.dart';
import '../../data/models/task_detail_model.dart';
import '../../data/models/team_model.dart';
import '../../data/repositories/task_repository.dart';
import '../../services/route_service.dart';
import '../../services/team_location_tracker.dart';
import 'task_event.dart';
import 'task_state.dart';

class TaskBloc extends Bloc<TaskEvent, TaskState> {
  final TaskRepository _taskRepository;
  final RouteService _routeService;
  final LocationService _locationService;
  final TeamLocationTracker _locationTracker;
  final LocationStreamRepository? _locationStreamRepository;
  StreamSubscription? _backgroundLocationSubscription;

  TaskBloc({
    required TaskRepository taskRepository,
    required RouteService routeService,
    required LocationService locationService,
    TeamLocationTracker? locationTracker,
    LocationStreamRepository? locationStreamRepository,
  })  : _taskRepository = taskRepository,
        _routeService = routeService,
        _locationService = locationService,
        _locationTracker = locationTracker ?? TeamLocationTracker(),
        _locationStreamRepository = locationStreamRepository,
        super(const TaskInitial()) {
    on<LoadActiveTask>(_onLoadActiveTask);
    on<LoadTaskHistory>(_onLoadTaskHistory);
    on<UpdateOperationalStatus>(_onUpdateOperationalStatus);
    on<SubmitTaskDebrief>(_onSubmitTaskDebrief);
    on<TaskLocationUpdated>(_onTaskLocationUpdated);
    on<SignalRTaskReceived>(_onSignalRTaskReceived);

    _listenToBackgroundLocationStream();
  }

  void _listenToBackgroundLocationStream() {
    _backgroundLocationSubscription?.cancel();
    _backgroundLocationSubscription = _locationStreamRepository?.onLocationUpdate.listen((data) {
      if (data != null && data['latitude'] != null && data['longitude'] != null) {
        final lat = double.tryParse(data['latitude'].toString());
        final lng = double.tryParse(data['longitude'].toString());
        if (lat != null && lng != null) {
          add(TaskLocationUpdated(latitude: lat, longitude: lng));
        }
      }
    });
  }

  Future<void> _onLoadActiveTask(
    LoadActiveTask event,
    Emitter<TaskState> emit,
  ) async {
    if (!event.isRefresh) {
      emit(const TaskLoading());
    }

    try {
      final activeTask = await _taskRepository.getActiveTaskForTeam(event.teamId);
      final history = await _taskRepository.getTaskHistory(event.teamId);

      if (activeTask == null) {
        _locationTracker.stopTracking();
        await _locationStreamRepository?.stopTracking();
        emit(TaskIdle(history: history));
        return;
      }

      LatLng? currentCoords;
      try {
        final position = await _locationService.getCurrentLocation();
        currentCoords = LatLng(position.latitude, position.longitude);
      } catch (_) {
        currentCoords = LatLng(activeTask.latitude - 0.005, activeTask.longitude - 0.005);
      }

      final destination = LatLng(activeTask.latitude, activeTask.longitude);
      final routeResult = await _routeService.calculateRoute(
        origin: currentCoords,
        destination: destination,
      );

      final currentStatus = TeamStatus.fromString(activeTask.status);
      if (currentStatus == TeamStatus.forwarded ||
          currentStatus == TeamStatus.enRoute ||
          currentStatus == TeamStatus.onScene) {
        _locationTracker.startTracking(
          teamId: event.teamId,
          taskRepository: _taskRepository,
        );
        await _locationStreamRepository?.startTracking(event.teamId);
      }

      emit(
        TaskActiveLoaded(
          task: activeTask,
          routePoints: routeResult.points,
          distanceKm: routeResult.distanceKm,
          estimatedMinutes: routeResult.durationMinutes,
          teamLocation: currentCoords,
          isRouteFallback: routeResult.isFallback,
          history: history,
        ),
      );
    } catch (e) {
      emit(TaskFailure(e.toString()));
    }
  }

  Future<void> _onLoadTaskHistory(
    LoadTaskHistory event,
    Emitter<TaskState> emit,
  ) async {
    try {
      final history = await _taskRepository.getTaskHistory(event.teamId);
      if (state is TaskActiveLoaded) {
        emit((state as TaskActiveLoaded).copyWith(history: history));
      } else if (state is TaskIdle) {
        emit(TaskIdle(history: history));
      }
    } catch (_) {}
  }

  Future<void> _onUpdateOperationalStatus(
    UpdateOperationalStatus event,
    Emitter<TaskState> emit,
  ) async {
    if (state is! TaskActiveLoaded) return;
    final currentState = state as TaskActiveLoaded;
    final currentTask = currentState.task;

    emit(TaskStatusUpdating(
      currentTask: currentTask,
      pendingStatus: event.newStatus,
    ));

    try {
      await _taskRepository.updateTeamStatus(event.teamId, event.newStatus);

      final memberStatus = _mapTeamStatusToMemberStatus(event.newStatus);
      await _taskRepository.updateMemberStatus(
        event.teamId,
        event.userId,
        memberStatus,
      );

      if (event.newStatus == TeamStatus.forwarded ||
          event.newStatus == TeamStatus.enRoute ||
          event.newStatus == TeamStatus.onScene) {
        _locationTracker.startTracking(
          teamId: event.teamId,
          taskRepository: _taskRepository,
        );
        await _locationStreamRepository?.startTracking(event.teamId);
      } else {
        _locationTracker.stopTracking();
        await _locationStreamRepository?.stopTracking();
      }

      final updatedTask = currentTask.copyWith(
        status: event.newStatus == TeamStatus.enRoute ? 'EnRoute' : event.newStatus.apiValue,
      );

      emit(currentState.copyWith(task: updatedTask));
    } catch (e) {
      emit(TaskFailure(
        'Failed to update status: ${e.toString()}',
        cachedTask: currentTask,
        failedStatus: event.newStatus,
      ));
      emit(currentState);
    }
  }

  Future<void> _onSubmitTaskDebrief(
    SubmitTaskDebrief event,
    Emitter<TaskState> emit,
  ) async {
    TeamTaskModel? taskToComplete;
    if (state is TaskActiveLoaded) {
      taskToComplete = (state as TaskActiveLoaded).task;
    }

    if (taskToComplete != null) {
      emit(TaskDebriefSubmitting(currentTask: taskToComplete));
    }

    try {
      await _taskRepository.completeTaskWithReport(
        incidentId: event.incidentId,
        teamId: event.teamId,
        notes: event.notes,
        mediaFile: event.photo,
      );

      _locationTracker.stopTracking();
      await _locationStreamRepository?.stopTracking();

      emit(const TaskDebriefSuccess('Task successfully completed and logged.'));

      final history = await _taskRepository.getTaskHistory(event.teamId);
      emit(TaskIdle(history: history));
    } catch (e) {
      emit(TaskFailure('Failed to submit debrief: ${e.toString()}'));
      if (taskToComplete != null) {
        add(LoadActiveTask(event.teamId, isRefresh: true));
      }
    }
  }

  Future<void> _onTaskLocationUpdated(
    TaskLocationUpdated event,
    Emitter<TaskState> emit,
  ) async {
    if (state is! TaskActiveLoaded) return;
    final currentState = state as TaskActiveLoaded;

    final updatedLocation = LatLng(event.latitude, event.longitude);
    final destination = LatLng(currentState.task.latitude, currentState.task.longitude);

    final routeResult = await _routeService.calculateRoute(
      origin: updatedLocation,
      destination: destination,
    );

    emit(
      currentState.copyWith(
        teamLocation: updatedLocation,
        routePoints: routeResult.points,
        distanceKm: routeResult.distanceKm,
        estimatedMinutes: routeResult.durationMinutes,
        isRouteFallback: routeResult.isFallback,
      ),
    );
  }

  void _onSignalRTaskReceived(
    SignalRTaskReceived event,
    Emitter<TaskState> emit,
  ) {
    final teamId = event.data['teamId']?.toString();
    if (teamId != null && teamId.isNotEmpty) {
      add(LoadActiveTask(teamId, isRefresh: true));
    }
  }

  MemberStatus _mapTeamStatusToMemberStatus(TeamStatus teamStatus) {
    switch (teamStatus) {
      case TeamStatus.enRoute:
        return MemberStatus.enRoute;
      case TeamStatus.onScene:
        return MemberStatus.onScene;
      case TeamStatus.busy:
        return MemberStatus.busy;
      case TeamStatus.idle:
      case TeamStatus.forwarded:
      case TeamStatus.resolved:
        return MemberStatus.available;
    }
  }

  @override
  Future<void> close() async {
    _backgroundLocationSubscription?.cancel();
    _locationTracker.stopTracking();
    await _locationStreamRepository?.stopTracking();
    return super.close();
  }
}
