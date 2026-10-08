import 'package:dio/dio.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/network/api_client.dart';
import '../../../core/network/api_exception.dart';
import '../../../core/storage/token_storage.dart';
import '../domain/auth_models.dart';

class AccountNotFoundException implements Exception {
  const AccountNotFoundException();
}

class AuthRepository {
  AuthRepository(this._dio, this._storage);

  final Dio _dio;
  final TokenStorage _storage;

  /// `+994` + 9 national digits, matching the backend's normalisation.
  static String normalizePhone(String input) {
    var digits = input.replaceAll(RegExp(r'\D'), '');
    if (digits.startsWith('994')) {
      digits = digits.substring(3);
    } else if (digits.startsWith('0')) {
      digits = digits.substring(1);
    }
    return '+994$digits';
  }

  static bool isValidPhone(String input) =>
      normalizePhone(input).length == '+994'.length + 9;

  /// Requests a login code. Throws [AccountNotFoundException] when the phone
  /// has no account so the caller can fall back to registration.
  Future<OtpChallenge> requestLoginOtp(String phone) => guardApi(() async {
    try {
      final res = await _dio.post<Map<String, dynamic>>(
        '/api/auth/login/otp',
        data: {'phoneNumber': phone},
      );
      return OtpChallenge.fromJson(res.data!);
    } on DioException catch (e) {
      final detail =
          (e.response?.data is Map ? (e.response!.data as Map)['detail'] : null)
              ?.toString()
              .toLowerCase();
      if (e.response?.statusCode == 400 &&
          detail != null &&
          detail.contains('not found')) {
        throw const AccountNotFoundException();
      }
      rethrow;
    }
  });

  Future<OtpChallenge> requestRegistrationOtp(String phone) =>
      guardApi(() async {
        final res = await _dio.post<Map<String, dynamic>>(
          '/api/auth/registration/otp',
          data: {'phoneNumber': phone},
        );
        return OtpChallenge.fromJson(res.data!);
      });

  Future<AuthSession> completeLogin({
    required String challengeId,
    required String code,
  }) => guardApi(() async {
    final res = await _dio.post<Map<String, dynamic>>(
      '/api/auth/login/complete',
      data: {'challengeId': challengeId, 'code': code},
    );
    return _persist(res.data!);
  });

  Future<AuthSession> completeRegistration({
    required String challengeId,
    required String code,
    required String fullName,
    required PreferredLanguage language,
  }) => guardApi(() async {
    final res = await _dio.post<Map<String, dynamic>>(
      '/api/auth/registration/complete',
      data: {
        'challengeId': challengeId,
        'code': code,
        'fullName': fullName,
        'preferredLanguage': language.id,
      },
    );
    return _persist(res.data!);
  });

  Future<AuthSession> _persist(Map<String, dynamic> json) async {
    final tokens = AuthTokens.fromJson(json);
    final session = AuthSession(
      userId: json['userId'] as String,
      phoneNumber: json['phoneNumber'] as String,
    );
    await _storage.saveTokens(tokens);
    await _storage.saveIdentity(
      userId: session.userId,
      phone: session.phoneNumber,
    );
    return session;
  }

  Future<AuthSession?> restore() async {
    final tokens = await _storage.readTokens();
    final identity = await _storage.readIdentity();
    if (tokens == null || identity == null) return null;
    if (tokens.refreshExpiresAt.isBefore(DateTime.now().toUtc())) {
      await _storage.clear();
      return null;
    }
    return AuthSession(userId: identity.userId, phoneNumber: identity.phone);
  }

  /// Best effort: the local session is cleared even if the call fails.
  Future<void> logout() async {
    final tokens = await _storage.readTokens();
    try {
      if (tokens != null) {
        await _dio.post<void>(
          '/api/auth/logout',
          data: {'refreshToken': tokens.refreshToken},
          options: Options(
            headers: {'Authorization': 'Bearer ${tokens.accessToken}'},
          ),
        );
      }
    } on DioException {
      // ignore
    } finally {
      await _storage.clear();
    }
  }
}

final authRepositoryProvider = Provider<AuthRepository>(
  (ref) =>
      AuthRepository(ref.watch(dioProvider), ref.watch(tokenStorageProvider)),
);
