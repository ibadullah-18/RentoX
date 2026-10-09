import 'dart:async';

import 'package:firebase_core/firebase_core.dart';
import 'package:firebase_messaging/firebase_messaging.dart';
import 'package:flutter/foundation.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/config/firebase_config.dart';

/// The part of push messaging the app needs, so the rest of the code (and the
/// tests) never touch Firebase directly.
abstract class PushGateway {
  /// False when push isn't configured or supported (web, no Firebase keys).
  bool get isAvailable;

  /// Asks the user for permission. Returns whether notifications are allowed.
  Future<bool> requestPermission();

  Future<String?> token();
  Stream<String> get tokenRefreshes;

  /// Data of a notification the user tapped while the app was in the
  /// background.
  Stream<Map<String, String>> get taps;

  /// Data of the notification that launched the app from a closed state.
  Future<Map<String, String>?> launchTap();

  Future<void> deleteToken();
}

class NoopPushGateway implements PushGateway {
  const NoopPushGateway();

  @override
  bool get isAvailable => false;
  @override
  Future<bool> requestPermission() async => false;
  @override
  Future<String?> token() async => null;
  @override
  Stream<String> get tokenRefreshes => const Stream.empty();
  @override
  Stream<Map<String, String>> get taps => const Stream.empty();
  @override
  Future<Map<String, String>?> launchTap() async => null;
  @override
  Future<void> deleteToken() async {}
}

Map<String, String> _data(RemoteMessage m) => {
  for (final e in m.data.entries) e.key: '${e.value}',
};

class FirebasePushGateway implements PushGateway {
  FirebasePushGateway(this._messaging);

  final FirebaseMessaging _messaging;

  @override
  bool get isAvailable => true;

  @override
  Future<bool> requestPermission() async {
    final settings = await _messaging.requestPermission();
    return settings.authorizationStatus == AuthorizationStatus.authorized ||
        settings.authorizationStatus == AuthorizationStatus.provisional;
  }

  @override
  Future<String?> token() => _messaging.getToken();

  @override
  Stream<String> get tokenRefreshes => _messaging.onTokenRefresh;

  @override
  Stream<Map<String, String>> get taps =>
      FirebaseMessaging.onMessageOpenedApp.map(_data);

  @override
  Future<Map<String, String>?> launchTap() async {
    final m = await _messaging.getInitialMessage();
    return m == null ? null : _data(m);
  }

  @override
  Future<void> deleteToken() => _messaging.deleteToken();
}

/// Starts Firebase once at app launch. Never throws: a broken push setup must
/// not stop the app from opening.
Future<void> initPush() async {
  if (!FirebaseConfig.isConfigured) {
    if (kDebugMode) {
      debugPrint('[push] off: no FIREBASE_* --dart-define values were given');
    }
    return;
  }
  try {
    await Firebase.initializeApp(options: FirebaseConfig.options);
    if (kDebugMode) debugPrint('[push] Firebase started');
  } catch (e) {
    if (kDebugMode) debugPrint('[push] Firebase failed to start: $e');
  }
}

final pushGatewayProvider = Provider<PushGateway>((ref) {
  if (!FirebaseConfig.isConfigured || Firebase.apps.isEmpty) {
    return const NoopPushGateway();
  }
  return FirebasePushGateway(FirebaseMessaging.instance);
});
