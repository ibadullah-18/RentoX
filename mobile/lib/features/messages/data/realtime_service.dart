import 'dart:async';

import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/config/app_config.dart';
import '../../../core/realtime/hub_access_token.dart';
import '../../../core/realtime/signalr_connection.dart';
import '../../auth/presentation/auth_controller.dart';
import '../domain/message_models.dart';

sealed class RealtimeEvent {
  const RealtimeEvent();
}

class MessageArrived extends RealtimeEvent {
  const MessageArrived(this.message);
  final ChatMessage message;
}

class MessagesReadEvent extends RealtimeEvent {
  const MessagesReadEvent({
    required this.conversationId,
    required this.readByUserId,
    required this.readAt,
  });
  final String conversationId;
  final String readByUserId;
  final DateTime readAt;
}

class TypingChanged extends RealtimeEvent {
  const TypingChanged({
    required this.conversationId,
    required this.userId,
    required this.typing,
  });
  final String conversationId;
  final String userId;
  final bool typing;
}

/// The connection came back after a drop: anything may have been missed, so
/// screens should re-fetch.
class Reconnected extends RealtimeEvent {
  const Reconnected();
}

/// Typed view of the conversations hub. Screens never see SignalR frames.
class RealtimeService {
  RealtimeService(
    this._connection, {
    Duration heartbeat = const Duration(seconds: 20),
  }) : _heartbeatEvery = heartbeat {
    _eventSub = _connection.events.listen(_onEvent);
    _stateSub = _connection.states.listen(_onState);
  }

  final SignalRConnection _connection;
  final Duration _heartbeatEvery;
  final _out = StreamController<RealtimeEvent>.broadcast();
  late final StreamSubscription<HubEvent> _eventSub;
  late final StreamSubscription<HubState> _stateSub;
  Timer? _heartbeat;
  bool _connectedBefore = false;

  Stream<RealtimeEvent> get events => _out.stream;
  bool get isConnected => _connection.state == HubState.connected;

  void start() => _connection.start();

  void startTyping(String conversationId) =>
      _connection.invoke('StartTyping', [conversationId]);

  void stopTyping(String conversationId) =>
      _connection.invoke('StopTyping', [conversationId]);

  void _onState(HubState state) {
    _heartbeat?.cancel();
    if (state == HubState.connected) {
      _heartbeat = Timer.periodic(
        _heartbeatEvery,
        (_) => _connection.invoke('Heartbeat'),
      );
      _connection.invoke('Heartbeat');
      if (_connectedBefore && !_out.isClosed) _out.add(const Reconnected());
      _connectedBefore = true;
    }
  }

  void _onEvent(HubEvent e) {
    if (_out.isClosed || e.arguments.isEmpty) return;
    final arg = e.arguments.first;
    if (arg is! Map<String, dynamic>) return;
    try {
      switch (e.target) {
        case 'MessageCreated':
          _out.add(MessageArrived(ChatMessage.fromJson(arg)));
        case 'MessagesRead':
          _out.add(
            MessagesReadEvent(
              conversationId: arg['conversationId'] as String,
              readByUserId: arg['readByUserId'] as String,
              readAt: DateTime.parse(arg['readAtUtc'] as String),
            ),
          );
        case 'TypingStarted' || 'TypingStopped':
          _out.add(
            TypingChanged(
              conversationId: arg['conversationId'] as String,
              userId: arg['userId'] as String,
              typing: e.target == 'TypingStarted',
            ),
          );
      }
    } catch (_) {
      // A malformed payload must never break the connection.
    }
  }

  Future<void> dispose() async {
    _heartbeat?.cancel();
    await _eventSub.cancel();
    await _stateSub.cancel();
    await _connection.dispose();
    await _out.close();
  }
}

/// A live service while signed in, `null` otherwise. It is torn down on sign
/// out and rebuilt on sign in.
final realtimeServiceProvider = Provider<RealtimeService?>((ref) {
  final session = ref.watch(authControllerProvider).value;
  if (session == null) return null;

  final service = RealtimeService(
    SignalRConnection(
      baseUrl: AppConfig.apiBaseUrl,
      hubPath: '/hubs/conversations',
      accessToken: hubAccessToken(ref),
    ),
  )..start();
  ref.onDispose(service.dispose);
  return service;
});
