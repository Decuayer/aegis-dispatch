import 'dart:io';
import '../../data/models/team_model.dart';

abstract class TaskEvent {
  const TaskEvent();
}

class LoadActiveTask extends TaskEvent {
  final String teamId;
  final bool isRefresh;

  const LoadActiveTask(this.teamId, {this.isRefresh = false});
}

class LoadTaskHistory extends TaskEvent {
  final String teamId;

  const LoadTaskHistory(this.teamId);
}

class UpdateOperationalStatus extends TaskEvent {
  final String teamId;
  final String userId;
  final TeamStatus newStatus;

  const UpdateOperationalStatus({
    required this.teamId,
    required this.userId,
    required this.newStatus,
  });
}

class SubmitTaskDebrief extends TaskEvent {
  final String incidentId;
  final String teamId;
  final String notes;
  final File? photo;

  const SubmitTaskDebrief({
    required this.incidentId,
    required this.teamId,
    required this.notes,
    this.photo,
  });
}

class TaskLocationUpdated extends TaskEvent {
  final double latitude;
  final double longitude;

  const TaskLocationUpdated({required this.latitude, required this.longitude});
}

class SignalRTaskReceived extends TaskEvent {
  final Map<String, dynamic> data;

  const SignalRTaskReceived(this.data);
}
