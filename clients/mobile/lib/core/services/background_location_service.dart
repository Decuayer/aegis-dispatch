import 'dart:async';
import 'dart:ui';
import 'package:flutter_background_service/flutter_background_service.dart';
import 'package:geolocator/geolocator.dart';
import 'package:signalr_netcore/signalr_client.dart';
import '../constants/api_endpoints.dart';
import 'local_notification_service.dart';

@pragma('vm:entry-point')
void onStart(ServiceInstance service) async {
  DartPluginRegistrant.ensureInitialized();

  HubConnection? hubConnection;
  StreamSubscription<Position>? positionSubscription;
  String? currentTeamId;
  String? currentToken;
  String baseUrl = ApiEndpoints.baseUrl;

  Future<void> connectSignalR() async {
    if (currentToken == null || currentToken!.isEmpty) return;

    final hubUrl = '$baseUrl/hubs/location';

    try {
      await hubConnection?.stop();
    } catch (_) {}

    hubConnection = HubConnectionBuilder()
        .withUrl(
          hubUrl,
          options: HttpConnectionOptions(
            accessTokenFactory: () async => currentToken!,
          ),
        )
        .withAutomaticReconnect(retryDelays: [0, 2000, 5000, 10000, 30000])
        .build();

    try {
      await hubConnection!.start();
    } catch (_) {
      // Reconnect attempts managed by automatic reconnect policy
    }
  }

  void startGpsTracking() {
    positionSubscription?.cancel();

    const locationSettings = LocationSettings(
      accuracy: LocationAccuracy.high,
      distanceFilter: 10,
    );

    positionSubscription = Geolocator.getPositionStream(
      locationSettings: locationSettings,
    ).listen((Position position) async {
      // 1. Transmit telemetry via SignalR
      final teamId = currentTeamId;
      if (hubConnection?.state == HubConnectionState.Connected && teamId != null) {
        try {
          await hubConnection!.invoke(
            'StreamTeamLocation',
            args: <Object>[teamId, position.latitude, position.longitude],
          );
        } catch (_) {}
      }

      // 2. Update Android notification banner
      service.invoke('updateNotification', {
        'title': 'SOCAR Dispatch Active Tracking',
        'content': 'Lat: ${position.latitude.toStringAsFixed(5)}, Lng: ${position.longitude.toStringAsFixed(5)}',
      });

      // 3. Emit position to main application UI
      service.invoke('onLocationUpdate', {
        'latitude': position.latitude,
        'longitude': position.longitude,
        'heading': position.heading,
        'speed': position.speed,
        'accuracy': position.accuracy,
      });
    });
  }


  service.on('setTeamData').listen((data) async {
    if (data == null) return;
    currentTeamId = data['teamId']?.toString();
    currentToken = data['token']?.toString();
    if (data['baseUrl'] != null) {
      baseUrl = data['baseUrl'].toString();
    }

    await connectSignalR();
    startGpsTracking();
  });

  service.on('stopTracking').listen((_) async {
    await positionSubscription?.cancel();
    positionSubscription = null;
    try {
      await hubConnection?.stop();
    } catch (_) {}
    hubConnection = null;
    currentTeamId = null;
    currentToken = null;
    service.stopSelf();
  });
}

@pragma('vm:entry-point')
Future<bool> onIosBackground(ServiceInstance service) async {
  return true;
}

class BackgroundLocationService {
  static final BackgroundLocationService _instance = BackgroundLocationService._internal();
  factory BackgroundLocationService() => _instance;
  BackgroundLocationService._internal();

  final FlutterBackgroundService _service = FlutterBackgroundService();

  Stream<Map<String, dynamic>?> get onLocationUpdate => _service.on('onLocationUpdate');

  Future<void> initialize() async {
    await _service.configure(
      androidConfiguration: AndroidConfiguration(
        onStart: onStart,
        autoStart: false,
        isForegroundMode: true,
        notificationChannelId: LocalNotificationService.channelId,
        initialNotificationTitle: 'SOCAR Dispatch Standby',
        initialNotificationContent: 'Awaiting emergency dispatch assignment...',
        foregroundServiceNotificationId: 9999,
        foregroundServiceTypes: [AndroidForegroundType.location],
      ),
      iosConfiguration: IosConfiguration(
        autoStart: false,
        onForeground: onStart,
        onBackground: onIosBackground,
      ),
    );
  }

  Future<bool> startTracking({
    required String teamId,
    required String token,
    String? baseUrl,
  }) async {
    final isRunning = await _service.isRunning();
    if (!isRunning) {
      final started = await _service.startService();
      if (!started) return false;
    }

    _service.invoke('setTeamData', {
      'teamId': teamId,
      'token': token,
      'baseUrl': baseUrl ?? ApiEndpoints.baseUrl,
    });
    return true;
  }

  Future<void> stopTracking() async {
    final isRunning = await _service.isRunning();
    if (isRunning) {
      _service.invoke('stopTracking');
    }
  }

  Future<bool> isTracking() async {
    return await _service.isRunning();
  }
}
