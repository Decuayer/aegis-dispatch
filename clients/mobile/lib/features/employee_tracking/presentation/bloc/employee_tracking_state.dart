import 'package:flutter/foundation.dart';
import 'package:latlong2/latlong.dart';
import '../../data/models/tracked_incident_model.dart';

@immutable
abstract class EmployeeTrackingState {
  const EmployeeTrackingState();
}

class EmployeeTrackingInitial extends EmployeeTrackingState {
  const EmployeeTrackingInitial();
}

class EmployeeTrackingLoading extends EmployeeTrackingState {
  const EmployeeTrackingLoading();
}

class EmployeeTrackingLoaded extends EmployeeTrackingState {
  final List<TrackedIncidentModel> incidents;
  final TrackedIncidentModel? selectedIncident;
  final List<LatLng> activeRoute;
  final double? distanceKm;
  final int? etaMinutes;
  final bool isRouteFallback;
  final bool isUpdating;
  final String? updateSuccessMessage;
  final String? updateErrorMessage;

  const EmployeeTrackingLoaded({
    required this.incidents,
    this.selectedIncident,
    this.activeRoute = const [],
    this.distanceKm,
    this.etaMinutes,
    this.isRouteFallback = false,
    this.isUpdating = false,
    this.updateSuccessMessage,
    this.updateErrorMessage,
  });

  EmployeeTrackingLoaded copyWith({
    List<TrackedIncidentModel>? incidents,
    TrackedIncidentModel? selectedIncident,
    List<LatLng>? activeRoute,
    double? distanceKm,
    int? etaMinutes,
    bool? isRouteFallback,
    bool? isUpdating,
    String? updateSuccessMessage,
    String? updateErrorMessage,
    bool clearSelectedIncident = false,
    bool clearRoute = false,
    bool clearFeedback = false,
  }) {
    return EmployeeTrackingLoaded(
      incidents: incidents ?? this.incidents,
      selectedIncident: clearSelectedIncident
          ? null
          : (selectedIncident ?? this.selectedIncident),
      activeRoute: clearRoute ? const [] : (activeRoute ?? this.activeRoute),
      distanceKm: clearRoute ? null : (distanceKm ?? this.distanceKm),
      etaMinutes: clearRoute ? null : (etaMinutes ?? this.etaMinutes),
      isRouteFallback: isRouteFallback ?? this.isRouteFallback,
      isUpdating: isUpdating ?? this.isUpdating,
      updateSuccessMessage: clearFeedback
          ? null
          : (updateSuccessMessage ?? this.updateSuccessMessage),
      updateErrorMessage: clearFeedback
          ? null
          : (updateErrorMessage ?? this.updateErrorMessage),
    );
  }
}

class EmployeeTrackingError extends EmployeeTrackingState {
  final String message;

  const EmployeeTrackingError(this.message);
}
