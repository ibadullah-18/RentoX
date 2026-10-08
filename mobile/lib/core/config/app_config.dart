import 'package:flutter/foundation.dart';

abstract final class AppConfig {
  static const _override = String.fromEnvironment('API_BASE_URL');

  /// Override with `--dart-define=API_BASE_URL=https://api.rentox.az`.
  static String get apiBaseUrl {
    if (_override.isNotEmpty) return _override;
    if (!kIsWeb && defaultTargetPlatform == TargetPlatform.android) {
      return 'http://10.0.2.2:5155'; // Android emulator -> host machine
    }
    return 'http://localhost:5155';
  }

  /// The API returns relative media paths (e.g. `/api/listing-images/{id}`).
  static String? resolveUrl(String? path) {
    if (path == null || path.isEmpty) return null;
    if (path.startsWith('http')) return path;
    return '$apiBaseUrl$path';
  }
}
