import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';

class AuthTokens {
  const AuthTokens({
    required this.accessToken,
    required this.refreshToken,
    required this.accessExpiresAt,
    required this.refreshExpiresAt,
  });

  final String accessToken;
  final String refreshToken;
  final DateTime accessExpiresAt;
  final DateTime refreshExpiresAt;

  factory AuthTokens.fromJson(Map<String, dynamic> json) => AuthTokens(
    accessToken: json['accessToken'] as String,
    refreshToken: json['refreshToken'] as String,
    accessExpiresAt: DateTime.parse(json['accessTokenExpiresAtUtc'] as String),
    refreshExpiresAt: DateTime.parse(
      json['refreshTokenExpiresAtUtc'] as String,
    ),
  );
}

/// Persists credentials in the platform keystore (Keychain / EncryptedSharedPreferences).
class TokenStorage {
  TokenStorage([FlutterSecureStorage? storage])
    : _storage = storage ?? const FlutterSecureStorage();

  final FlutterSecureStorage _storage;

  static const _access = 'access_token';
  static const _refresh = 'refresh_token';
  static const _accessExp = 'access_expires_at';
  static const _refreshExp = 'refresh_expires_at';
  static const _userId = 'user_id';
  static const _phone = 'phone_number';

  Future<AuthTokens?> readTokens() async {
    final access = await _storage.read(key: _access);
    final refresh = await _storage.read(key: _refresh);
    final accessExp = await _storage.read(key: _accessExp);
    final refreshExp = await _storage.read(key: _refreshExp);
    if (access == null ||
        refresh == null ||
        accessExp == null ||
        refreshExp == null) {
      return null;
    }
    return AuthTokens(
      accessToken: access,
      refreshToken: refresh,
      accessExpiresAt: DateTime.parse(accessExp),
      refreshExpiresAt: DateTime.parse(refreshExp),
    );
  }

  Future<void> saveTokens(AuthTokens t) async {
    await _storage.write(key: _access, value: t.accessToken);
    await _storage.write(key: _refresh, value: t.refreshToken);
    await _storage.write(
      key: _accessExp,
      value: t.accessExpiresAt.toIso8601String(),
    );
    await _storage.write(
      key: _refreshExp,
      value: t.refreshExpiresAt.toIso8601String(),
    );
  }

  Future<({String userId, String phone})?> readIdentity() async {
    final id = await _storage.read(key: _userId);
    final phone = await _storage.read(key: _phone);
    if (id == null || phone == null) return null;
    return (userId: id, phone: phone);
  }

  Future<void> saveIdentity({
    required String userId,
    required String phone,
  }) async {
    await _storage.write(key: _userId, value: userId);
    await _storage.write(key: _phone, value: phone);
  }

  Future<void> clear() => _storage.deleteAll();
}

final tokenStorageProvider = Provider<TokenStorage>((ref) => TokenStorage());
