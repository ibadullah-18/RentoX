import 'package:dio/dio.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/network/api_client.dart';
import '../../../core/network/api_exception.dart';
import '../../catalog/domain/catalog_models.dart';
import '../domain/notification_models.dart';

class NotificationsRepository {
  NotificationsRepository(this._dio);

  final Dio _dio;

  Future<Paged<AppNotification>> list({int page = 1, int pageSize = 20}) =>
      guardApi(() async {
        final res = await _dio.get<Map<String, dynamic>>(
          '/api/notifications',
          queryParameters: {'page': page, 'pageSize': pageSize},
        );
        return Paged.fromJson(res.data!, AppNotification.fromJson);
      });

  Future<int> unreadCount() => guardApi(() async {
    final res = await _dio.get<Map<String, dynamic>>(
      '/api/notifications/unread-count',
    );
    return (res.data?['count'] as num?)?.toInt() ?? 0;
  });

  Future<void> markRead(String id) =>
      guardApi(() => _dio.patch<void>('/api/notifications/$id/read'));

  Future<void> markAllRead() =>
      guardApi(() => _dio.patch<void>('/api/notifications/read-all'));
}

final notificationsRepositoryProvider = Provider<NotificationsRepository>(
  (ref) => NotificationsRepository(ref.watch(dioProvider)),
);
