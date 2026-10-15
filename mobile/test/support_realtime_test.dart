import 'dart:async';

import 'package:flutter_test/flutter_test.dart';
import 'package:rentox/core/realtime/signalr_connection.dart';
import 'package:rentox/features/support/data/support_realtime.dart';

class _FakeConnection extends SignalRConnection {
  _FakeConnection()
    : super(
        baseUrl: 'http://localhost',
        hubPath: '/hubs/support',
        accessToken: () async => null,
      );

  final hub = StreamController<HubEvent>.broadcast();
  final hubStates = StreamController<HubState>.broadcast();
  final sent = <String>[];

  @override
  Stream<HubEvent> get events => hub.stream;

  @override
  Stream<HubState> get states => hubStates.stream;

  @override
  bool invoke(String target, [List<Object?> arguments = const []]) {
    sent.add('$target:${arguments.join(',')}');
    return true;
  }
}

void main() {
  late _FakeConnection connection;
  late SupportRealtime realtime;
  late List<SupportEvent> seen;

  setUp(() {
    connection = _FakeConnection();
    realtime = SupportRealtime(connection);
    seen = [];
    realtime.events.listen(seen.add);
  });

  tearDown(() async {
    await connection.hub.close();
    await connection.hubStates.close();
  });

  Future<void> settle() => Future<void>.delayed(Duration.zero);

  test('an answer from the team arrives as a message of that request', () async {
    connection.hub.add(
      const HubEvent('SupportMessageCreated', [
        {
          'ticketId': 't1',
          'message': {
            'id': 'm1',
            'body': 'Salam',
            'isAdmin': true,
            'sentAtUtc': '2026-10-15T10:00:00Z',
          },
        },
      ]),
    );
    await settle();

    final event = seen.single as SupportMessageArrived;
    expect(event.ticketId, 't1');
    expect(event.message.body, 'Salam');
    expect(event.message.isAdmin, isTrue);
  });

  test('typing starts and stops, and says whether it is the team', () async {
    connection.hub
      ..add(
        const HubEvent('TypingStarted', [
          {'ticketId': 't1', 'userId': 'u', 'isStaff': true},
        ]),
      )
      ..add(
        const HubEvent('TypingStopped', [
          {'ticketId': 't1', 'userId': 'u', 'isStaff': true},
        ]),
      );
    await settle();

    final events = seen.cast<SupportTypingChanged>();
    expect(events.map((e) => e.typing), [true, false]);
    expect(events.every((e) => e.isStaff && e.ticketId == 't1'), isTrue);
  });

  test('a changed request and a malformed payload', () async {
    connection.hub
      ..add(
        const HubEvent('SupportTicketChanged', [
          {'ticketId': 't1', 'status': 4, 'priority': 2},
        ]),
      )
      ..add(const HubEvent('SupportMessageCreated', [
        {'ticketId': 't1'},
      ]));
    await settle();

    expect(seen.single, isA<SupportTicketChanged>());
  });

  test('coming back after a drop asks screens to re-fetch', () async {
    connection.hubStates
      ..add(HubState.connected)
      ..add(HubState.disconnected)
      ..add(HubState.connected);
    await settle();

    expect(seen.whereType<SupportResync>(), hasLength(1));
  });

  test('typing is sent to the hub with the request id', () {
    realtime
      ..startTyping('t1')
      ..stopTyping('t1');

    expect(connection.sent, ['StartTyping:t1', 'StopTyping:t1']);
  });
}
