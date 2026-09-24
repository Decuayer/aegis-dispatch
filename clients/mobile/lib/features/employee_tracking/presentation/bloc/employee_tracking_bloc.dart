import 'dart:async';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:latlong2/latlong.dart';
import '../../../team_tasks/services/route_service.dart';
import '../../data/models/tracked_incident_model.dart';
import '../../data/repositories/employee_incident_repository.dart';
import '../../data/services/employee_tracking_hub_service.dart';
import 'employee_tracking_event.dart';
import 'employee_tracking_state.dart';

class EmployeeTrackingBloc
    extends Bloc<EmployeeTrackingEvent, EmployeeTrackingState> {
  final EmployeeIncidentRepository _repository;
  final RouteService _routeService;
  final EmployeeTrackingHubService _hubService;

  StreamSubscription<NewIncidentUpdate>? _newIncidentSub;
  StreamSubscription<IncidentStatusUpdate>? _statusSub;
  StreamSubscription<TeamDispatchedUpdate>? _dispatchSub;
  StreamSubscription<TeamLocationUpdate>? _locationSub;
  StreamSubscription<IncidentUpdatedData>? _incidentUpdatedSub;

  String? _currentUserId;

  EmployeeTrackingBloc({
    required EmployeeIncidentRepository repository,
    required RouteService routeService,
    required EmployeeTrackingHubService hubService,
  }) : _repository = repository,
       _routeService = routeService,
       _hubService = hubService,
       super(const EmployeeTrackingInitial()) {
    on<LoadMyIncidents>(_onLoadMyIncidents);
    on<RefreshMyIncidents>(_onRefreshMyIncidents);
    on<SelectIncidentForTracking>(_onSelectIncidentForTracking);
    on<IncidentStatusReceived>(_onIncidentStatusReceived);
    on<TeamAssignedReceived>(_onTeamAssignedReceived);
    on<TeamLocationReceived>(_onTeamLocationReceived);
    on<UpdateIncidentDetailsRequested>(_onUpdateIncidentDetailsRequested);
    on<ClearTrackingFeedback>(_onClearTrackingFeedback);

    _listenToSignalRStreams();
  }

  void _listenToSignalRStreams() {
    _newIncidentSub = _hubService.onNewIncident.listen((_) {
      add(const RefreshMyIncidents());
    });

    _statusSub = _hubService.onIncidentStatusChanged.listen((update) {
      add(
        IncidentStatusReceived(
          incidentId: update.incidentId,
          status: update.status,
        ),
      );
    });

    _dispatchSub = _hubService.onTeamDispatched.listen((update) {
      add(
        TeamAssignedReceived(
          incidentId: update.incidentId,
          teamId: update.teamId,
        ),
      );
    });

    _locationSub = _hubService.onTeamLocationUpdated.listen((update) {
      add(
        TeamLocationReceived(
          teamId: update.teamId,
          latitude: update.latitude,
          longitude: update.longitude,
        ),
      );
    });

    _incidentUpdatedSub = _hubService.onIncidentUpdated.listen((_) {
      add(const RefreshMyIncidents());
    });
  }

  Future<void> _onLoadMyIncidents(
    LoadMyIncidents event,
    Emitter<EmployeeTrackingState> emit,
  ) async {
    _currentUserId = event.userId;
    emit(const EmployeeTrackingLoading());

    try {
      final incidents = await _repository.getMyReportedIncidents(
        reporterId: event.userId,
      );
      emit(EmployeeTrackingLoaded(incidents: incidents));
    } catch (e) {
      emit(EmployeeTrackingError(_cleanErrorMessage(e)));
    }
  }

  Future<void> _onRefreshMyIncidents(
    RefreshMyIncidents event,
    Emitter<EmployeeTrackingState> emit,
  ) async {
    final userId = _currentUserId;
    if (userId == null) return;

    try {
      final incidents = await _repository.getMyReportedIncidents(
        reporterId: userId,
      );

      if (state is EmployeeTrackingLoaded) {
        final current = state as EmployeeTrackingLoaded;
        TrackedIncidentModel? updatedSelected = current.selectedIncident;

        if (updatedSelected != null) {
          final matched = incidents.where((i) => i.id == updatedSelected!.id);
          if (matched.isNotEmpty) {
            updatedSelected = matched.first;
          }
        }

        emit(
          current.copyWith(
            incidents: incidents,
            selectedIncident: updatedSelected,
          ),
        );
      } else {
        emit(EmployeeTrackingLoaded(incidents: incidents));
      }
    } catch (e) {
      // Non-destructive fallback on silent refresh error
    }
  }

  Future<void> _onSelectIncidentForTracking(
    SelectIncidentForTracking event,
    Emitter<EmployeeTrackingState> emit,
  ) async {
    if (state is! EmployeeTrackingLoaded) return;
    final current = state as EmployeeTrackingLoaded;

    try {
      final incident = await _repository.getIncidentDetails(event.incidentId);

      List<LatLng> routePoints = [];
      double? distance;
      int? eta;
      bool isFallback = false;

      // Calculate route if team coordinates exist
      if (incident.teamLatitude != null && incident.teamLongitude != null) {
        final origin = LatLng(incident.teamLatitude!, incident.teamLongitude!);
        final dest = LatLng(incident.latitude, incident.longitude);

        final routeResult = await _routeService.calculateRoute(
          origin: origin,
          destination: dest,
        );

        routePoints = routeResult.points;
        distance = routeResult.distanceKm;
        eta = routeResult.durationMinutes;
        isFallback = routeResult.isFallback;
      }

      emit(
        current.copyWith(
          selectedIncident: incident,
          activeRoute: routePoints,
          distanceKm: distance,
          etaMinutes: eta,
          isRouteFallback: isFallback,
        ),
      );
    } catch (e) {
      emit(current.copyWith(updateErrorMessage: _cleanErrorMessage(e)));
    }
  }

  Future<void> _onIncidentStatusReceived(
    IncidentStatusReceived event,
    Emitter<EmployeeTrackingState> emit,
  ) async {
    if (state is! EmployeeTrackingLoaded) return;
    final current = state as EmployeeTrackingLoaded;

    final updatedIncidents =
        current.incidents.map((inc) {
          if (inc.id == event.incidentId) {
            return inc.copyWith(status: event.status);
          }
          return inc;
        }).toList();

    TrackedIncidentModel? updatedSelected = current.selectedIncident;
    if (updatedSelected != null && updatedSelected.id == event.incidentId) {
      updatedSelected = updatedSelected.copyWith(status: event.status);
    }

    emit(
      current.copyWith(
        incidents: updatedIncidents,
        selectedIncident: updatedSelected,
      ),
    );
  }

  Future<void> _onTeamAssignedReceived(
    TeamAssignedReceived event,
    Emitter<EmployeeTrackingState> emit,
  ) async {
    if (state is! EmployeeTrackingLoaded) return;
    // Reload details for this incident to fetch assigned team info
    add(SelectIncidentForTracking(event.incidentId));
  }

  Future<void> _onTeamLocationReceived(
    TeamLocationReceived event,
    Emitter<EmployeeTrackingState> emit,
  ) async {
    if (state is! EmployeeTrackingLoaded) return;
    final current = state as EmployeeTrackingLoaded;

    final selected = current.selectedIncident;
    if (selected == null || selected.assignedTeamId != event.teamId) return;

    final updatedIncident = selected.copyWith(
      teamLatitude: event.latitude,
      teamLongitude: event.longitude,
      assignedTeamStatus: 'EnRoute',
    );

    // Re-calculate route, distance and ETA dynamically
    final origin = LatLng(event.latitude, event.longitude);
    final destination = LatLng(selected.latitude, selected.longitude);

    final routeResult = await _routeService.calculateRoute(
      origin: origin,
      destination: destination,
    );

    emit(
      current.copyWith(
        selectedIncident: updatedIncident,
        activeRoute: routeResult.points,
        distanceKm: routeResult.distanceKm,
        etaMinutes: routeResult.durationMinutes,
        isRouteFallback: routeResult.isFallback,
      ),
    );
  }

  Future<void> _onUpdateIncidentDetailsRequested(
    UpdateIncidentDetailsRequested event,
    Emitter<EmployeeTrackingState> emit,
  ) async {
    if (state is! EmployeeTrackingLoaded) return;
    final current = state as EmployeeTrackingLoaded;
    final selected = current.selectedIncident;

    if (selected == null || !selected.isEditable) {
      emit(
        current.copyWith(
          updateErrorMessage:
              'Cannot edit an incident that is resolved or closed.',
        ),
      );
      return;
    }

    emit(current.copyWith(isUpdating: true, clearFeedback: true));

    try {
      // 1. Upload any new supplementary media attachments to MinIO
      final uploadedMedia = <Map<String, dynamic>>[];
      for (final existing in selected.mediaAttachments) {
        uploadedMedia.add(existing.toJson());
      }

      for (final file in event.newFiles) {
        final mediaUrl = await _repository.uploadSupplementaryMedia(file);
        uploadedMedia.add({'mediaUrl': mediaUrl, 'mediaType': 1});
      }

      // 2. Send PUT request to update description and media attachments
      final updated = await _repository.updateIncident(
        incidentId: event.incidentId,
        category: selected.category,
        emergencyCode: selected.emergencyCode,
        description: event.description ?? selected.description,
        mediaAttachments: uploadedMedia,
        latitude: selected.latitude,
        longitude: selected.longitude,
      );

      final updatedList =
          current.incidents.map((i) {
            return i.id == updated.id ? updated : i;
          }).toList();

      emit(
        current.copyWith(
          incidents: updatedList,
          selectedIncident: updated,
          isUpdating: false,
          updateSuccessMessage: 'Incident details updated successfully.',
        ),
      );
    } catch (e) {
      emit(
        current.copyWith(
          isUpdating: false,
          updateErrorMessage: _cleanErrorMessage(e),
        ),
      );
    }
  }

  void _onClearTrackingFeedback(
    ClearTrackingFeedback event,
    Emitter<EmployeeTrackingState> emit,
  ) {
    if (state is EmployeeTrackingLoaded) {
      emit((state as EmployeeTrackingLoaded).copyWith(clearFeedback: true));
    }
  }

  String _cleanErrorMessage(dynamic error) {
    final msg = error.toString();
    return msg.replaceFirst('Exception: ', '');
  }

  @override
  Future<void> close() {
    _newIncidentSub?.cancel();
    _statusSub?.cancel();
    _dispatchSub?.cancel();
    _locationSub?.cancel();
    _incidentUpdatedSub?.cancel();
    return super.close();
  }
}
