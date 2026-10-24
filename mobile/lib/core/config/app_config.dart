import 'package:flutter/foundation.dart';

abstract final class AppConfig {
  static const _override = String.fromEnvironment('API_BASE_URL');

  /// The real server. A release build (the app people install) always talks to it, over HTTPS only.
  static const _production = 'https://api.rentox.az';

  /// While developing, `--dart-define=API_BASE_URL=http://192.168.1.5:5155` points the app at another machine.
  /// A release build ignores a plain-http address: the app never sends anything unencrypted.
  static String get apiBaseUrl {
    if (kReleaseMode) {
      return _override.startsWith('https://') ? _override : _production;
    }
    if (_override.isNotEmpty) return _override;
    if (!kIsWeb && defaultTargetPlatform == TargetPlatform.android) {
      return 'http://10.0.2.2:5155'; // Android emulator -> host machine
    }
    return 'http://localhost:5155';
  }

  static const _shareBase = String.fromEnvironment(
    'SHARE_BASE_URL',
    defaultValue: 'https://rentox.az',
  );

  /// The public site used in links people share (no trailing slash).
  static String get shareBaseUrl => _shareBase.endsWith('/')
      ? _shareBase.substring(0, _shareBase.length - 1)
      : _shareBase;

  /// The API returns relative media paths (e.g. `/api/listing-images/{id}`).
  static String? resolveUrl(String? path) {
    if (path == null || path.isEmpty) return null;
    if (path.startsWith('http')) return path;
    return '$apiBaseUrl$path';
  }
}
