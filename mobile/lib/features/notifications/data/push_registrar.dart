import 'dart:async';
import 'dart:math';

import 'package:dio/dio.dart';
import 'package:flutter/foundation.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:shared_preferences/shared_preferences.dart';

import '../../../core/network/api_client.dart';
import '../../../core/network/api_exception.dart';
import 'push_gateway.dart';

class PushDevicesRepository {
  PushDevicesRepository(this._dio);

  final Dio _dio;

  Future<void> register({
    required int platform,
    required String deviceId,
    required String token,
  }) => guardApi(
    () => _dio.post<void>(
      '/api/push-devices',
      data: {'platform': platform, 'deviceId': deviceId, 'token': token},
    ),
  );

  Future<void> unregister(String deviceId) => guardApi(
    () => _dio.delete<void>(
      '/api/push-devices',
      queryParameters: {'deviceId': deviceId},
    ),
  );
}

void _log(String message) {
  if (kDebugMode) debugPrint('[push] $message');
}

/// Keeps this phone's push token registered with the backend for the signed-in
/// user, and removes it on sign-out so the next person on the phone doesn't
/// receive the previous user's notifications.
class PushRegistrar {
  PushRegistrar({
    required this.gateway,
    required this.repository,
    required this.deviceId,
    required this.platform,
  });

  final PushGateway gateway;
  final PushDevicesRepository repository;
  final Future<String> Function() deviceId;

  /// Backend platform id, or null on platforms without push.
  final int? platform;

  StreamSubscription<String>? _refreshSub;
  bool _started = false;

  /// Backend platform ids: 1 = Android, 2 = iOS.
  static int? platformFor(TargetPlatform p) => switch (p) {
    TargetPlatform.android => 1,
    TargetPlatform.iOS => 2,
    _ => null,
  };

  /// Asks for permission and registers the token. Safe to call repeatedly.
  Future<void> start() async {
    if (_started || !gateway.isAvailable || platform == null) {
      _log(
        _started
            ? 'already started'
            : 'skipped (Firebase not configured or unsupported platform)',
      );
      return;
    }
    _started = true;
    try {
      _log('asking for permission');
      if (!await gateway.requestPermission()) {
        _log('permission denied');
        _started = false;
        return;
      }
      final token = await gateway.token();
      _log(token == null ? 'no token received' : 'got token');
      if (token != null) await _send(token);
      _refreshSub = gateway.tokenRefreshes.listen((t) => unawaited(_send(t)));
    } catch (e) {
      // Push is a bonus: any failure here must never reach the user.
      _log('failed: $e');
      _started = false;
    }
  }

  Future<void> _send(String token) async {
    try {
      await repository.register(
        platform: platform!,
        deviceId: await deviceId(),
        token: token,
      );
      _log('registered with the server');
    } catch (e) {
      _log('registering failed: $e');
    }
  }

  void stop() {
    _refreshSub?.cancel();
    _refreshSub = null;
    _started = false;
  }

  /// Call before signing out (the request needs the user's session).
  Future<void> unregister() async {
    stop();
    if (!gateway.isAvailable) return;
    try {
      await repository.unregister(await deviceId());
    } catch (_) {}
  }
}

/// A random id that stays the same for this installation.
Future<String> _loadDeviceId() async {
  const key = 'push_device_id';
  final prefs = await SharedPreferences.getInstance();
  var id = prefs.getString(key);
  if (id == null) {
    final random = Random.secure();
    id = List.generate(
      16,
      (_) => random.nextInt(256).toRadixString(16).padLeft(2, '0'),
    ).join();
    await prefs.setString(key, id);
  }
  return id;
}

final pushRegistrarProvider = Provider<PushRegistrar>((ref) {
  final registrar = PushRegistrar(
    gateway: ref.watch(pushGatewayProvider),
    repository: PushDevicesRepository(ref.watch(dioProvider)),
    deviceId: _loadDeviceId,
    platform: PushRegistrar.platformFor(defaultTargetPlatform),
  );
  ref.onDispose(registrar.stop);
  return registrar;
});
