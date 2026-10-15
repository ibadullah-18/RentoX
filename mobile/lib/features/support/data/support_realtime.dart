import 'dart:async';

import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/config/app_config.dart';
import '../../../core/realtime/hub_access_token.dart';
import '../../../core/realtime/signalr_connection.dart';
import '../../auth/presentation/auth_controller.dart';
import '../domain/support_models.dart';

sealed class SupportEvent {
  const SupportEvent();
}

/// A message was written in one of the user's requests (an answer from the team).
class SupportMessageArrived extends SupportEvent {
  const SupportMessageArrived(this.ticketId, this.message);
  final String ticketId;
  final SupportMessage message;
}

/// The status or the priority of a request changed.
class SupportTicketChanged extends SupportEvent {
  const SupportTicketChanged(this.ticketId);
  final String ticketId;
}

/// The team is writing (or stopped writing) in a request.
class SupportTypingChanged extends SupportEvent {
  const SupportTypingChanged({
    required this.ticketId,
    required this.typing,
    required this.isStaff,
  });
  final String ticketId;
  final bool typing;
  final bool isStaff;
}

/// The connection came back after a drop: anything may have been missed.
class SupportResync extends SupportEvent {
  const SupportResync();
}

/// Typed view of the support hub (`/hubs/support`) for the person who owns requests.
class SupportRealtime {
  SupportRealtime(this._connection) {
    _eventSub = _connection.events.listen(_onEvent);
    _stateSub = _connection.states.listen((s) {
      if (s == HubState.connected) {
        if (_connectedBefore && !_out.isClosed) _out.add(const SupportResync());
        _connectedBefore = true;
      }
    });
  }

  final SignalRConnection _connection;
  final _out = StreamController<SupportEvent>.broadcast();
  late final StreamSubscription<HubEvent> _eventSub;
  late final StreamSubscription<HubState> _stateSub;
  bool _connectedBefore = false;

  Stream<SupportEvent> get events => _out.stream;

  void start() => _connection.start();

  void startTyping(String ticketId) =>
      _connection.invoke('StartTyping', [ticketId]);

  void stopTyping(String ticketId) =>
      _connection.invoke('StopTyping', [ticketId]);

  void _onEvent(HubEvent e) {
    if (_out.isClosed || e.arguments.isEmpty) return;
    final arg = e.arguments.first;
    if (arg is! Map<String, dynamic>) return;
    try {
      switch (e.target) {
        case 'SupportMessageCreated':
          _out.add(
            SupportMessageArrived(
              arg['ticketId'] as String,
              SupportMessage.fromJson(arg['message'] as Map<String, dynamic>),
            ),
          );
        case 'SupportTicketChanged':
          _out.add(SupportTicketChanged(arg['ticketId'] as String));
        case 'TypingStarted' || 'TypingStopped':
          _out.add(
            SupportTypingChanged(
              ticketId: arg['ticketId'] as String,
              typing: e.target == 'TypingStarted',
              isStaff: (arg['isStaff'] as bool?) ?? false,
            ),
          );
      }
    } catch (_) {
      // A malformed payload must never break the connection.
    }
  }

  Future<void> dispose() async {
    await _eventSub.cancel();
    await _stateSub.cancel();
    await _connection.dispose();
    await _out.close();
  }
}

/// A live line while signed in, `null` otherwise.
final supportRealtimeProvider = Provider<SupportRealtime?>((ref) {
  if (ref.watch(authControllerProvider).value == null) return null;
  final realtime = SupportRealtime(
    SignalRConnection(
      baseUrl: AppConfig.apiBaseUrl,
      hubPath: '/hubs/support',
      accessToken: hubAccessToken(ref),
    ),
  )..start();
  ref.onDispose(realtime.dispose);
  return realtime;
});
