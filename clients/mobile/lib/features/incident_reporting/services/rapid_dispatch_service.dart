import 'dart:async';
import 'package:geolocator/geolocator.dart';
import '../data/models/create_incident_request_model.dart';
import '../data/models/incident_response_model.dart';
import '../data/models/rapid_emergency_preset.dart';
import '../data/repositories/incident_repository.dart';
import 'location_service.dart';

class RapidDispatchService {
  final IncidentRepository _incidentRepository;
  final LocationService _locationService;

  const RapidDispatchService({
    required IncidentRepository incidentRepository,
    required LocationService locationService,
  })  : _incidentRepository = incidentRepository,
        _locationService = locationService;

  /// Acquires location silently within a 2-second threshold and submits a 1-tap incident.
  Future<IncidentResponseModel> dispatchRapidIncident(RapidEmergencyPreset preset) async {
    Position? position;

    try {
      position = await _locationService
          .getCurrentLocation(timeout: const Duration(seconds: 2))
          .timeout(const Duration(seconds: 2));
    } catch (_) {
      // Fallback to last known cached location if immediate GPS fix times out
      position = await Geolocator.getLastKnownPosition();
    }

    if (position == null) {
      throw Exception('Unable to acquire device coordinates for emergency alert.');
    }

    final request = CreateIncidentRequestModel(
      category: preset.category,
      emergencyCode: preset.emergencyCode,
      description: 'Rapid 1-Tap Emergency Alert triggered by user (${preset.title})',
      latitude: position.latitude,
      longitude: position.longitude,
      mediaAttachments: const [],
    );

    return await _incidentRepository.createIncident(request);
  }

  /// Cancels a previously dispatched incident during the reversal window.
  Future<bool> cancelDispatchedIncident(String incidentId) async {
    return await _incidentRepository.cancelIncident(incidentId);
  }
}
