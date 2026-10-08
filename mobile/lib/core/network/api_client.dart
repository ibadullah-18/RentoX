import 'dart:async';

import 'package:dio/dio.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../features/auth/presentation/auth_controller.dart';
import '../config/app_config.dart';
import '../storage/token_storage.dart';

BaseOptions _baseOptions() => BaseOptions(
  baseUrl: AppConfig.apiBaseUrl,
  connectTimeout: const Duration(seconds: 15),
  receiveTimeout: const Duration(seconds: 30),
  headers: {'Accept': 'application/json'},
);

/// Attaches the bearer token and transparently refreshes it once on a 401.
class _AuthInterceptor extends Interceptor {
  _AuthInterceptor({
    required this.dio,
    required this.storage,
    required this.onSessionExpired,
  });

  final Dio dio;
  final TokenStorage storage;
  final void Function() onSessionExpired;
  final Dio _refreshDio = Dio(_baseOptions());

  Future<AuthTokens?>? _refreshing;

  static bool _isAuthEndpoint(String path) => path.startsWith('/api/auth/');

  @override
  Future<void> onRequest(
    RequestOptions options,
    RequestInterceptorHandler handler,
  ) async {
    if (!_isAuthEndpoint(options.path)) {
      final tokens = await storage.readTokens();
      if (tokens != null) {
        options.headers['Authorization'] = 'Bearer ${tokens.accessToken}';
      }
    }
    handler.next(options);
  }

  @override
  Future<void> onError(
    DioException err,
    ErrorInterceptorHandler handler,
  ) async {
    final request = err.requestOptions;
    final alreadyRetried = request.extra['retried'] == true;

    if (err.response?.statusCode != 401 ||
        alreadyRetried ||
        _isAuthEndpoint(request.path)) {
      return handler.next(err);
    }

    final current = await storage.readTokens();
    if (current == null) return handler.next(err);

    // Collapse concurrent 401s into a single refresh call.
    final tokens = await (_refreshing ??= _refresh(current)
        .whenComplete(() => _refreshing = null));

    if (tokens == null) {
      onSessionExpired();
      return handler.next(err);
    }

    try {
      request.extra['retried'] = true;
      request.headers['Authorization'] = 'Bearer ${tokens.accessToken}';
      handler.resolve(await dio.fetch<dynamic>(request));
    } on DioException catch (e) {
      handler.next(e);
    }
  }

  Future<AuthTokens?> _refresh(AuthTokens current) async {
    try {
      final res = await _refreshDio.post<Map<String, dynamic>>(
        '/api/auth/refresh',
        data: {'refreshToken': current.refreshToken},
      );
      final tokens = AuthTokens.fromJson(res.data!);
      await storage.saveTokens(tokens);
      return tokens;
    } on DioException catch (e) {
      // Only a definitive rejection ends the session; network blips keep it.
      final code = e.response?.statusCode;
      if (code == 401 || code == 400) {
        await storage.clear();
        return null;
      }
      return current;
    }
  }
}

final dioProvider = Provider<Dio>((ref) {
  final dio = Dio(_baseOptions());
  dio.interceptors.add(
    _AuthInterceptor(
      dio: dio,
      storage: ref.watch(tokenStorageProvider),
      onSessionExpired: () =>
          ref.read(authControllerProvider.notifier).sessionExpired(),
    ),
  );
  return dio;
});
