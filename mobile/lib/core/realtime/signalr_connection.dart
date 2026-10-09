import 'dart:async';
import 'dart:convert';

import 'package:dio/dio.dart';
import 'package:web_socket_channel/web_socket_channel.dart';

/// Minimal ASP.NET Core SignalR client (JSON protocol over WebSockets).
///
/// Only what the app needs: connect with a bearer token, receive server
/// invocations, call hub methods, keep the connection alive and reconnect
/// with backoff. Written in plain Dart on top of `web_socket_channel`, so it
/// works on Android, iOS and web without a platform plugin.
///
/// Wire format (record separator `0x1E` ends every frame):
/// handshake `{"protocol":"json","version":1}`, invocation `{"type":1,...}`,
/// ping `{"type":6}`, close `{"type":7}`.
const _separator = '\u001e';

class HubEvent {
  const HubEvent(this.target, this.arguments);

  final String target;
  final List<dynamic> arguments;
}

enum HubState { disconnected, connecting, connected }

/// A text socket. An interface so tests can drive the client without a
/// network.
abstract class HubSocket {
  Stream<String> get messages;
  Future<void> get ready;
  void send(String frame);
  Future<void> close();
}

class _WebSocketAdapter implements HubSocket {
  _WebSocketAdapter(this._channel);

  final WebSocketChannel _channel;

  @override
  Stream<String> get messages =>
      _channel.stream.map((e) => e is String ? e : utf8.decode(e as List<int>));

  @override
  Future<void> get ready => _channel.ready;

  @override
  void send(String frame) => _channel.sink.add(frame);

  @override
  Future<void> close() async {
    await _channel.sink.close();
  }
}

typedef HubSocketFactory = HubSocket Function(Uri uri);

/// Returns the connection token for [accessToken] (SignalR "negotiate").
typedef HubNegotiator = Future<String> Function(String accessToken);

class SignalRConnection {
  SignalRConnection({
    required this.baseUrl,
    required this.hubPath,
    required this.accessToken,
    HubSocketFactory? socketFactory,
    HubNegotiator? negotiator,
    Dio? dio,
    this.keepAlive = const Duration(seconds: 15),
    this.serverTimeout = const Duration(seconds: 40),
    this.backoff = const [
      Duration(seconds: 1),
      Duration(seconds: 2),
      Duration(seconds: 4),
      Duration(seconds: 8),
      Duration(seconds: 15),
      Duration(seconds: 30),
    ],
  }) : _dio = dio ?? Dio(),
       _socketFactory =
           socketFactory ??
           ((uri) => _WebSocketAdapter(WebSocketChannel.connect(uri))) {
    _negotiator = negotiator ?? _defaultNegotiate;
  }

  /// `http(s)://host:port` of the API.
  final String baseUrl;

  /// `/hubs/conversations`.
  final String hubPath;

  /// Supplies a currently valid access token (refreshing it if needed).
  final Future<String?> Function() accessToken;

  final Duration keepAlive;
  final Duration serverTimeout;
  final List<Duration> backoff;

  final Dio _dio;
  final HubSocketFactory _socketFactory;
  late final HubNegotiator _negotiator;

  final _events = StreamController<HubEvent>.broadcast();
  final _states = StreamController<HubState>.broadcast();
  HubState _state = HubState.disconnected;

  HubSocket? _socket;
  bool _wanted = false;
  bool _loopRunning = false;
  Completer<void>? _wake;

  Stream<HubEvent> get events => _events.stream;
  Stream<HubState> get states => _states.stream;
  HubState get state => _state;

  void _setState(HubState s) {
    if (_state == s) return;
    _state = s;
    if (!_states.isClosed) _states.add(s);
  }

  /// Starts connecting (and keeps reconnecting until [stop]).
  void start() {
    _wanted = true;
    if (!_loopRunning) unawaited(_run());
  }

  Future<void> stop() async {
    _wanted = false;
    _wake?.complete();
    final socket = _socket;
    _socket = null;
    if (socket != null) {
      try {
        await socket.close();
      } catch (_) {}
    }
    _setState(HubState.disconnected);
  }

  Future<void> dispose() async {
    await stop();
    await _events.close();
    await _states.close();
  }

  /// Calls a hub method without waiting for its result. Returns whether the
  /// frame could be sent (i.e. the connection is up).
  bool invoke(String target, [List<Object?> arguments = const []]) {
    final socket = _socket;
    if (socket == null || _state != HubState.connected) return false;
    socket.send(
      '${jsonEncode({'type': 1, 'target': target, 'arguments': arguments})}$_separator',
    );
    return true;
  }

  Future<void> _run() async {
    _loopRunning = true;
    var attempt = 0;
    try {
      while (_wanted) {
        final connected = await _connectOnce();
        if (!_wanted) break;
        // A connection that worked resets the backoff.
        if (connected) attempt = 0;
        final delay = backoff[attempt.clamp(0, backoff.length - 1)];
        attempt++;
        _wake = Completer<void>();
        await Future.any([Future<void>.delayed(delay), _wake!.future]);
      }
    } finally {
      _loopRunning = false;
      _setState(HubState.disconnected);
    }
  }

  /// One connection attempt. Returns `true` if the handshake succeeded.
  Future<bool> _connectOnce() async {
    _setState(HubState.connecting);
    var handshaken = false;
    Timer? pingTimer;
    Timer? watchdog;
    final closed = Completer<void>();
    StreamSubscription<String>? subscription;

    void finish() {
      if (!closed.isCompleted) closed.complete();
    }

    void resetWatchdog() {
      watchdog?.cancel();
      watchdog = Timer(serverTimeout, finish);
    }

    try {
      final token = await accessToken();
      if (token == null || !_wanted) return false;

      final connectionToken = await _negotiator(token);
      final socket = _socketFactory(_webSocketUri(connectionToken, token));
      _socket = socket;
      await socket.ready;

      socket.send('{"protocol":"json","version":1}$_separator');
      resetWatchdog();

      // Frames end with the record separator; keep any incomplete tail until
      // the rest arrives.
      var buffer = '';
      subscription = socket.messages.listen(
        (chunk) {
          resetWatchdog();
          buffer += chunk;
          final parts = buffer.split(_separator);
          buffer = parts.removeLast();
          for (final part in parts) {
            if (part.isEmpty) continue;
            final Map<String, dynamic> frame;
            try {
              frame = jsonDecode(part) as Map<String, dynamic>;
            } catch (_) {
              continue;
            }

            if (!handshaken) {
              // The handshake reply is `{}`; anything with an error aborts.
              if (frame['error'] != null) {
                finish();
                return;
              }
              handshaken = true;
              _setState(HubState.connected);
              pingTimer = Timer.periodic(keepAlive, (_) {
                if (_state == HubState.connected) {
                  socket.send('{"type":6}$_separator');
                }
              });
              continue;
            }

            switch (frame['type']) {
              case 1: // invocation from the server
                if (!_events.isClosed) {
                  _events.add(
                    HubEvent(
                      frame['target'] as String,
                      (frame['arguments'] as List?) ?? const [],
                    ),
                  );
                }
              case 7: // server asked us to close
                finish();
              default: // pings and completions need no action
                break;
            }
          }
        },
        onError: (_) => finish(),
        onDone: finish,
        cancelOnError: true,
      );

      await closed.future;
    } catch (_) {
      // Network failure, rejected token...: fall through to the backoff.
    } finally {
      pingTimer?.cancel();
      watchdog?.cancel();
      await subscription?.cancel();
      final socket = _socket;
      _socket = null;
      if (socket != null) {
        try {
          await socket.close();
        } catch (_) {}
      }
      _setState(HubState.disconnected);
    }
    return handshaken;
  }

  Future<String> _defaultNegotiate(String token) async {
    final res = await _dio.post<Map<String, dynamic>>(
      '$baseUrl$hubPath/negotiate',
      queryParameters: {'negotiateVersion': 1, 'access_token': token},
    );
    final data = res.data!;
    return (data['connectionToken'] ?? data['connectionId']) as String;
  }

  Uri _webSocketUri(String connectionToken, String token) {
    final base = Uri.parse(baseUrl);
    return Uri(
      scheme: base.scheme == 'https' ? 'wss' : 'ws',
      host: base.host,
      port: base.hasPort ? base.port : null,
      path: hubPath,
      queryParameters: {'id': connectionToken, 'access_token': token},
    );
  }
}
