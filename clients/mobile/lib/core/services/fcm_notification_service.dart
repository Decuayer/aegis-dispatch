import 'dart:convert';
import 'package:firebase_core/firebase_core.dart';
import 'package:firebase_messaging/firebase_messaging.dart';
import '../constants/api_endpoints.dart';
import '../network/api_client.dart';
import 'local_notification_service.dart';

@pragma('vm:entry-point')
Future<void> firebaseMessagingBackgroundHandler(RemoteMessage message) async {
  await Firebase.initializeApp();
}

class FcmNotificationService {
  final FirebaseMessaging _messaging;
  final ApiClient _apiClient;
  final LocalNotificationService _localNotifications;

  void Function(String incidentId, Map<String, dynamic> data)? onDispatchAlertTapped;

  FcmNotificationService({
    FirebaseMessaging? messaging,
    required ApiClient apiClient,
    LocalNotificationService? localNotifications,
  })  : _messaging = messaging ?? FirebaseMessaging.instance,
        _apiClient = apiClient,
        _localNotifications = localNotifications ?? LocalNotificationService();

  Future<void> initialize({
    void Function(String incidentId, Map<String, dynamic> data)? onNotificationAction,
  }) async {
    onDispatchAlertTapped = onNotificationAction;

    await _messaging.requestPermission(
      alert: true,
      badge: true,
      sound: true,
      provisional: false,
    );

    await _messaging.setForegroundNotificationPresentationOptions(
      alert: true,
      badge: true,
      sound: true,
    );

    // 1. Synchronize FCM token with backend
    await syncDeviceToken();
    _messaging.onTokenRefresh.listen(_sendTokenToBackend);

    // 2. Foreground notification listener
    FirebaseMessaging.onMessage.listen(_handleForegroundMessage);

    // 3. Background tap listener (app was running in background)
    FirebaseMessaging.onMessageOpenedApp.listen(_handleMessageOpenedApp);

    // 4. Terminated state tap listener (app was closed)
    final initialMessage = await _messaging.getInitialMessage();
    if (initialMessage != null) {
      _handleMessageOpenedApp(initialMessage);
    }

    // 5. Connect local notification tap to deep link callback
    _localNotifications.onNotificationTapped = _handleLocalNotificationTap;
  }

  Future<String?> syncDeviceToken() async {
    try {
      final token = await _messaging.getToken();
      if (token != null && token.isNotEmpty) {
        await _sendTokenToBackend(token);
      }
      return token;
    } catch (_) {
      return null;
    }
  }

  Future<void> _sendTokenToBackend(String token) async {
    try {
      await _apiClient.dio.post(
        ApiEndpoints.updateDeviceToken,
        data: {'token': token},
      );
    } catch (_) {
      // Failed token synchronization will retry on next application startup
    }
  }

  void _handleForegroundMessage(RemoteMessage message) {
    final title = message.notification?.title ?? 'Emergency Dispatch Alert';
    final body = message.notification?.body ?? 'New dispatch mission assigned to your unit.';

    _localNotifications.showEmergencyNotification(
      id: message.hashCode,
      title: title,
      body: body,
      payload: message.data,
    );
  }

  void _handleMessageOpenedApp(RemoteMessage message) {
    _processDispatchAlertPayload(message.data);
  }

  void _handleLocalNotificationTap(String? payloadString) {
    if (payloadString == null || payloadString.isEmpty) return;
    try {
      final data = jsonDecode(payloadString) as Map<String, dynamic>;
      _processDispatchAlertPayload(data);
    } catch (_) {
      // Ignored malformed payload
    }
  }

  void _processDispatchAlertPayload(Map<String, dynamic> data) {
    final incidentId = data['incidentId']?.toString();
    if (incidentId != null && incidentId.isNotEmpty) {
      onDispatchAlertTapped?.call(incidentId, data);
    }
  }
}
