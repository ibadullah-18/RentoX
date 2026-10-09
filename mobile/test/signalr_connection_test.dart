import 'dart:async';

import 'package:flutter_test/flutter_test.dart';
import 'package:rentox/core/realtime/signalr_connection.dart';

const rs = '\u001e';

class FakeSocket implements HubSocket {
  final _in = StreamController<String>();
  final sent = <String>[];
  bool closed = false;

  @override
  Stream<String> get messages => _in.stream;

  @override
  Future<void> get ready => Future.value();

  @override
  void send(String frame) => sent.add(frame);

  @override
  Future<void> close() async {
    closed = true;
    if (!_in.isClosed) await _in.close();
  }

  void receive(String chunk) => _in.add(chunk);

  /// Simulates the server dropping the connection.
  Future<void> drop() => _in.close();
}

class Harness {
  Harness({String? token = 'jwt'}) {
    connection = SignalRConnection(
      baseUrl: 'http://localhost:5155',
      hubPath: '/hubs/conversations',
      accessToken: () async => token,
      negotiator: (t) async {
        negotiated.add(t);
        return 'conn-${negotiated.length}';
      },
      socketFactory: (uri) {
        uris.add(uri);
        final s = FakeSocket();
        sockets.add(s);
        return s;
      },
      keepAlive: const Duration(milliseconds: 40),
      serverTimeout: const Duration(seconds: 5),
      backoff: const [Duration(milliseconds: 20)],
    );
  }

  late final SignalRConnection connection;
  final sockets = <FakeSocket>[];
  final uris = <Uri>[];
  final negotiated = <String>[];

  FakeSocket get socket => sockets.last;

  Future<void> until(bool Function() test, {int ms = 2000}) async {
    final end = DateTime.now().add(Duration(milliseconds: ms));
    while (!test()) {
      if (DateTime.now().isAfter(end)) fail('timed out waiting for condition');
      await Future<void>.delayed(const Duration(milliseconds: 5));
    }
  }

  Future<void> connect() async {
    connection.start();
    await until(() => sockets.isNotEmpty);
    socket.receive('{}$rs'); // server handshake reply
    await until(() => connection.state == HubState.connected);
  }
}

void main() {
  test('negotiates, handshakes and reports connected', () async {
    final h = Harness();
    addTearDown(h.connection.dispose);

    await h.connect();

    expect(h.negotiated, ['jwt']);
    expect(h.uris.single.scheme, 'ws');
    expect(h.uris.single.path, '/hubs/conversations');
    expect(h.uris.single.queryParameters, {
      'id': 'conn-1',
      'access_token': 'jwt',
    });
    expect(h.socket.sent.first, '{"protocol":"json","version":1}$rs');
  });

  test(
    'delivers server invocations, even several frames in one chunk',
    () async {
      final h = Harness();
      addTearDown(h.connection.dispose);
      final events = <HubEvent>[];
      h.connection.events.listen(events.add);
      await h.connect();

      h.socket.receive(
        '{"type":1,"target":"MessageCreated","arguments":[{"id":"m1"}]}$rs'
        '{"type":6}$rs'
        '{"type":1,"target":"TypingStarted","arguments":[{"userId":"u"}]}$rs',
      );
      await h.until(() => events.length == 2);

      expect(events[0].target, 'MessageCreated');
      expect((events[0].arguments.single as Map)['id'], 'm1');
      expect(events[1].target, 'TypingStarted'); // ping frame ignored
    },
  );

  test('a frame split across chunks is not lost or duplicated', () async {
    final h = Harness();
    addTearDown(h.connection.dispose);
    final events = <HubEvent>[];
    h.connection.events.listen(events.add);
    await h.connect();

    h.socket.receive('{"type":1,"target":"A","arguments":[]}$rs{"type":1,');
    await h.until(() => events.length == 1);
    h.socket.receive('"target":"B","arguments":[]}$rs');
    await h.until(() => events.length == 2);
    expect(events.map((e) => e.target), ['A', 'B']);
  });

  test('invoke sends a frame only while connected', () async {
    final h = Harness();
    addTearDown(h.connection.dispose);

    expect(h.connection.invoke('Heartbeat'), isFalse);
    await h.connect();

    expect(h.connection.invoke('StartTyping', ['c1']), isTrue);
    expect(
      h.socket.sent.last,
      '{"type":1,"target":"StartTyping","arguments":["c1"]}$rs',
    );
  });

  test('sends keep-alive pings', () async {
    final h = Harness();
    addTearDown(h.connection.dispose);
    await h.connect();

    await h.until(() => h.socket.sent.contains('{"type":6}$rs'));
  });

  test('reconnects after the server drops the connection', () async {
    final h = Harness();
    addTearDown(h.connection.dispose);
    final states = <HubState>[];
    h.connection.states.listen(states.add);
    await h.connect();

    await h.socket.drop();
    await h.until(() => h.sockets.length == 2);
    h.socket.receive('{}$rs');
    await h.until(() => h.connection.state == HubState.connected);

    expect(h.negotiated, hasLength(2)); // fresh negotiation each time
    expect(
      states,
      containsAllInOrder([
        HubState.connecting,
        HubState.connected,
        HubState.disconnected,
        HubState.connected,
      ]),
    );
  });

  test('a handshake error is retried', () async {
    final h = Harness();
    addTearDown(h.connection.dispose);
    h.connection.start();
    await h.until(() => h.sockets.isNotEmpty);

    h.socket.receive('{"error":"Requested protocol is not available."}$rs');
    await h.until(() => h.sockets.length == 2);
    expect(h.connection.state, isNot(HubState.connected));
  });

  test('does not connect without an access token', () async {
    final h = Harness(token: null);
    addTearDown(h.connection.dispose);
    h.connection.start();
    await Future<void>.delayed(const Duration(milliseconds: 120));

    expect(h.sockets, isEmpty);
    expect(h.negotiated, isEmpty);
  });

  test('stop closes the socket and stays stopped', () async {
    final h = Harness();
    addTearDown(h.connection.dispose);
    await h.connect();

    await h.connection.stop();
    expect(h.socket.closed, isTrue);
    expect(h.connection.state, HubState.disconnected);

    await Future<void>.delayed(const Duration(milliseconds: 120));
    expect(h.sockets, hasLength(1)); // no reconnect after stop
  });

  test('a server close frame triggers a reconnect', () async {
    final h = Harness();
    addTearDown(h.connection.dispose);
    await h.connect();

    h.socket.receive('{"type":7}$rs');
    await h.until(() => h.sockets.length == 2);
  });
}
