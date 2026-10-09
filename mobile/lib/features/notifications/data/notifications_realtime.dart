import 'dart:async';

import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/config/app_config.dart';
import '../../../core/realtime/hub_access_token.dart';
import '../../../core/realtime/signalr_connection.dart';
import '../../auth/presentation/auth_controller.dart';
import '../domain/notification_models.dart';

sealed class NotificationEvent {
  const NotificationEvent();
}

class NotificationArrived extends NotificationEvent {
  const NotificationArrived(this.notification);
  final AppNotification notification;
}

/// The hub reconnected: re-fetch, something may have been missed.
class NotificationsResync extends NotificationEvent {
  const NotificationsResync();
}

/// Live notifications over `/hubs/notifications`.
class NotificationsRealtime {
  NotificationsRealtime(this._connection) {
    _eventSub = _connection.events.listen((e) {
      if (e.target != 'NotificationCreated' || e.arguments.isEmpty) return;
      final arg = e.arguments.first;
      if (arg is! Map<String, dynamic>) return;
      try {
        _out.add(NotificationArrived(AppNotification.fromJson(arg)));
      } catch (_) {
        // A malformed payload must not break the connection.
      }
    });
    _stateSub = _connection.states.listen((s) {
      if (s == HubState.connected) {
        if (_connectedBefore) _out.add(const NotificationsResync());
        _connectedBefore = true;
      }
    });
  }

  final SignalRConnection _connection;
  final _out = StreamController<NotificationEvent>.broadcast();
  late final StreamSubscription<HubEvent> _eventSub;
  late final StreamSubscription<HubState> _stateSub;
  bool _connectedBefore = false;

  Stream<NotificationEvent> get events => _out.stream;

  void start() => _connection.start();

  Future<void> dispose() async {
    await _eventSub.cancel();
    await _stateSub.cancel();
    await _connection.dispose();
    await _out.close();
  }
}

final notificationsRealtimeProvider = Provider<NotificationsRealtime?>((ref) {
  if (ref.watch(authControllerProvider).value == null) return null;
  final realtime = NotificationsRealtime(
    SignalRConnection(
      baseUrl: AppConfig.apiBaseUrl,
      hubPath: '/hubs/notifications',
      accessToken: hubAccessToken(ref),
    ),
  )..start();
  ref.onDispose(realtime.dispose);
  return realtime;
});
