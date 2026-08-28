import 'dart:async';
import 'package:geolocator/geolocator.dart';
import '../data/repositories/task_repository.dart';

class TeamLocationTracker {
  StreamSubscription<Position>? _positionSubscription;
  DateTime? _lastSyncTime;

  bool get isTracking => _positionSubscription != null;

  void startTracking({
    required String teamId,
    required TaskRepository taskRepository,
  }) {
    stopTracking();

    const locationSettings = LocationSettings(
      accuracy: LocationAccuracy.high,
      distanceFilter: 10, // Sync on 10+ meters displacement
    );

    _positionSubscription = Geolocator.getPositionStream(
      locationSettings: locationSettings,
    ).listen((Position position) {
      final now = DateTime.now();
      // Throttle: Send at most once every 5 seconds to prevent server overload
      if (_lastSyncTime == null ||
          now.difference(_lastSyncTime!).inSeconds >= 5) {
        _lastSyncTime = now;
        taskRepository.updateTeamLocation(
          teamId: teamId,
          latitude: position.latitude,
          longitude: position.longitude,
        );
      }
    });
  }

  void stopTracking() {
    _positionSubscription?.cancel();
    _positionSubscription = null;
    _lastSyncTime = null;
  }
}
