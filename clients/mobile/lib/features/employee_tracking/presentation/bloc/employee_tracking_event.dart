import 'dart:io';
import 'package:flutter/foundation.dart';

@immutable
abstract class EmployeeTrackingEvent {
  const EmployeeTrackingEvent();
}

class LoadMyIncidents extends EmployeeTrackingEvent {
  final String userId;

  const LoadMyIncidents(this.userId);
}

class RefreshMyIncidents extends EmployeeTrackingEvent {
  const RefreshMyIncidents();
}

class SelectIncidentForTracking extends EmployeeTrackingEvent {
  final String incidentId;

  const SelectIncidentForTracking(this.incidentId);
}

class IncidentStatusReceived extends EmployeeTrackingEvent {
  final String incidentId;
  final String status;

  const IncidentStatusReceived({
    required this.incidentId,
    required this.status,
  });
}

class TeamAssignedReceived extends EmployeeTrackingEvent {
  final String incidentId;
  final String teamId;

  const TeamAssignedReceived({
    required this.incidentId,
    required this.teamId,
  });
}

class TeamLocationReceived extends EmployeeTrackingEvent {
  final String teamId;
  final double latitude;
  final double longitude;

  const TeamLocationReceived({
    required this.teamId,
    required this.latitude,
    required this.longitude,
  });
}

class UpdateIncidentDetailsRequested extends EmployeeTrackingEvent {
  final String incidentId;
  final String? description;
  final List<File> newFiles;

  const UpdateIncidentDetailsRequested({
    required this.incidentId,
    this.description,
    this.newFiles = const [],
  });
}

class ClearTrackingFeedback extends EmployeeTrackingEvent {
  const ClearTrackingFeedback();
}
