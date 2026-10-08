import 'package:dio/dio.dart';

/// Normalised error surfaced to the UI. Never expose raw Dio errors to widgets.
class ApiException implements Exception {
  const ApiException(this.message, {this.statusCode, this.isNetwork = false});

  final String message;
  final int? statusCode;
  final bool isNetwork;

  bool get isUnauthorized => statusCode == 401;

  /// HTTP 429, or the backend's business-rule "please wait" (400) for OTP resends.
  bool get isRateLimited =>
      statusCode == 429 ||
      (statusCode == 400 && message.toLowerCase().contains('wait'));

  factory ApiException.fromDio(DioException e) {
    switch (e.type) {
      case DioExceptionType.connectionTimeout:
      case DioExceptionType.sendTimeout:
      case DioExceptionType.receiveTimeout:
      case DioExceptionType.connectionError:
        return const ApiException('network', isNetwork: true);
      default:
        break;
    }

    final status = e.response?.statusCode;
    final data = e.response?.data;
    String? message;
    if (data is Map<String, dynamic>) {
      message = (data['detail'] ?? data['title']) as String?;
      final errors = data['errors'];
      if (errors is Map && errors.isNotEmpty) {
        final first = errors.values.first;
        if (first is List && first.isNotEmpty) message = first.first.toString();
      }
    }
    return ApiException(message ?? 'unknown', statusCode: status);
  }

  @override
  String toString() => 'ApiException($statusCode, $message)';
}

/// Runs [call] and converts any [DioException] into an [ApiException], so
/// repositories never leak transport errors to the UI.
Future<T> guardApi<T>(Future<T> Function() call) async {
  try {
    return await call();
  } on DioException catch (e) {
    throw ApiException.fromDio(e);
  }
}
