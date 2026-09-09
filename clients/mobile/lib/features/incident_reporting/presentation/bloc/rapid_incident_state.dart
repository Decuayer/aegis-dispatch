import '../../data/models/incident_response_model.dart';
import '../../data/models/rapid_emergency_preset.dart';

abstract class RapidIncidentState {
  const RapidIncidentState();
}

class RapidIncidentInitial extends RapidIncidentState {
  const RapidIncidentInitial();
}

class RapidIncidentSubmitting extends RapidIncidentState {
  final String activePresetId;
  const RapidIncidentSubmitting(this.activePresetId);
}

class RapidIncidentSuccess extends RapidIncidentState {
  final IncidentResponseModel incident;
  final RapidEmergencyPreset preset;
  final bool isReversible;
  final int remainingSeconds;

  const RapidIncidentSuccess({
    required this.incident,
    required this.preset,
    this.isReversible = true,
    this.remainingSeconds = 5,
  });

  RapidIncidentSuccess copyWith({bool? isReversible, int? remainingSeconds}) {
    return RapidIncidentSuccess(
      incident: incident,
      preset: preset,
      isReversible: isReversible ?? this.isReversible,
      remainingSeconds: remainingSeconds ?? this.remainingSeconds,
    );
  }
}

class RapidIncidentFailure extends RapidIncidentState {
  final String errorMessage;
  const RapidIncidentFailure(this.errorMessage);
}

class RapidIncidentCanceled extends RapidIncidentState {
  final String incidentId;
  const RapidIncidentCanceled(this.incidentId);
}
