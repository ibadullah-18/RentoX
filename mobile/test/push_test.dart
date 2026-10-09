import 'package:flutter/foundation.dart' show TargetPlatform;

import 'dart:async';

import 'package:dio/dio.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:rentox/features/notifications/data/push_gateway.dart';
import 'package:rentox/features/notifications/data/push_registrar.dart';
import 'package:rentox/features/notifications/domain/push_target.dart';

const listingId = '11111111-2222-3333-4444-555555555555';

class FakeGateway extends NoopPushGateway {
  FakeGateway({this.allowed = true, this.tokenValue = 'tok-1'});

  bool allowed;
  String? tokenValue;
  int permissionAsks = 0;
  final refreshes = StreamController<String>.broadcast();

  @override
  bool get isAvailable => true;

  @override
  Future<bool> requestPermission() async {
    permissionAsks++;
    return allowed;
  }

  @override
  Future<String?> token() async => tokenValue;

  @override
  Stream<String> get tokenRefreshes => refreshes.stream;
}

class FakeDevices extends PushDevicesRepository {
  FakeDevices() : super(Dio());

  final registered = <({int platform, String deviceId, String token})>[];
  final removed = <String>[];
  bool failing = false;

  @override
  Future<void> register({
    required int platform,
    required String deviceId,
    required String token,
  }) async {
    if (failing) throw Exception('offline');
    registered.add((platform: platform, deviceId: deviceId, token: token));
  }

  @override
  Future<void> unregister(String deviceId) async {
    if (failing) throw Exception('offline');
    removed.add(deviceId);
  }
}

PushRegistrar make(FakeGateway g, FakeDevices d, {int? platform = 1}) =>
    PushRegistrar(
      gateway: g,
      repository: d,
      deviceId: () async => 'device-1',
      platform: platform,
    );

Future<void> settle() => Future<void>.delayed(const Duration(milliseconds: 10));

void main() {
  group('PushTarget', () {
    test('reads the listing and notification id from the push data', () {
      final t = PushTarget.fromData({
        'notificationId': 'n1',
        'actionUrl': '/listings/$listingId',
        'relatedEntityId': listingId,
      });
      expect(t.notificationId, 'n1');
      expect(t.listingId, listingId);
    });

    test('empty values from the backend mean "no target"', () {
      final t = PushTarget.fromData({
        'notificationId': '',
        'actionUrl': '',
        'relatedEntityId': '',
      });
      expect(t.notificationId, isNull);
      expect(t.listingId, isNull);
    });

    test('an unknown link goes nowhere special', () {
      final t = PushTarget.fromData({'actionUrl': '/support/tickets/abc'});
      expect(t.listingId, isNull);
    });
  });

  group('PushRegistrar', () {
    test('asks permission, then registers the token for this device', () async {
      final g = FakeGateway();
      final d = FakeDevices();
      await make(g, d).start();

      expect(g.permissionAsks, 1);
      expect(d.registered.single.token, 'tok-1');
      expect(d.registered.single.deviceId, 'device-1');
      expect(d.registered.single.platform, 1);
    });

    test('does nothing when the user denies permission', () async {
      final g = FakeGateway(allowed: false);
      final d = FakeDevices();
      await make(g, d).start();
      expect(d.registered, isEmpty);
    });

    test('does nothing when push is unavailable', () async {
      final d = FakeDevices();
      await make(FakeGateway(), d, platform: null).start();
      expect(d.registered, isEmpty);

      await PushRegistrar(
        gateway: const NoopPushGateway(),
        repository: d,
        deviceId: () async => 'x',
        platform: 1,
      ).start();
      expect(d.registered, isEmpty);
    });

    test('registers again when Firebase rotates the token', () async {
      final g = FakeGateway();
      final d = FakeDevices();
      final r = make(g, d);
      await r.start();

      g.refreshes.add('tok-2');
      await settle();
      expect(d.registered.map((e) => e.token), ['tok-1', 'tok-2']);
      r.stop();
    });

    test('starting twice registers once', () async {
      final g = FakeGateway();
      final d = FakeDevices();
      final r = make(g, d);
      await r.start();
      await r.start();
      expect(d.registered, hasLength(1));
      expect(g.permissionAsks, 1);
    });

    test('a server error never throws and can be retried later', () async {
      final g = FakeGateway();
      final d = FakeDevices()..failing = true;
      final r = make(g, d);
      await r.start();
      expect(d.registered, isEmpty);

      d.failing = false;
      g.refreshes.add('tok-2');
      await settle();
      expect(d.registered.single.token, 'tok-2');
    });

    test('sign-out removes this device and stops listening', () async {
      final g = FakeGateway();
      final d = FakeDevices();
      final r = make(g, d);
      await r.start();

      await r.unregister();
      expect(d.removed, ['device-1']);

      g.refreshes.add('tok-3');
      await settle();
      expect(d.registered.map((e) => e.token), ['tok-1']);
    });

    test('sign-out still works when the request fails', () async {
      final d = FakeDevices()..failing = true;
      await make(FakeGateway(), d).unregister();
      expect(d.removed, isEmpty);
    });

    test('platform ids match the backend enum', () {
      expect(PushRegistrar.platformFor(TargetPlatform.android), 1);
      expect(PushRegistrar.platformFor(TargetPlatform.iOS), 2);
      expect(PushRegistrar.platformFor(TargetPlatform.windows), isNull);
    });
  });
}
