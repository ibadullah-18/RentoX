import 'package:dio/dio.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/network/api_client.dart';
import '../../../core/network/api_exception.dart';
import '../domain/report_models.dart';

class ReportRepository {
  ReportRepository(this._dio);

  final Dio _dio;

  /// Sends a report about a listing or a store.
  Future<void> submit({
    required ReportTarget target,
    required String targetId,
    required ReportReason reason,
    String details = '',
  }) => guardApi(
    () => _dio.post<void>(
      '/api/${target.path}/$targetId/reports',
      data: {
        'reason': reason.id,
        'details': details.trim().isEmpty ? null : details.trim(),
      },
    ),
  );
}

final reportRepositoryProvider = Provider<ReportRepository>(
  (ref) => ReportRepository(ref.watch(dioProvider)),
);

/// True when the server refused because this person already has an open
/// report about the same thing.
bool isAlreadyReported(Object error) =>
    error is ApiException &&
    error.statusCode == 400 &&
    error.message.toLowerCase().contains('already reported');
