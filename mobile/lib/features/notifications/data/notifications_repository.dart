import 'package:dio/dio.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/network/api_client.dart';
import '../../auth/presentation/auth_controller.dart';

/// Unread notification count for the signed-in user (0 when signed out or on error).
final unreadNotificationsProvider = FutureProvider<int>((ref) async {
  final session = ref.watch(authControllerProvider).value;
  if (session == null) return 0;
  try {
    final res = await ref
        .watch(dioProvider)
        .get<Map<String, dynamic>>('/api/notifications/unread-count');
    return (res.data?['count'] as num?)?.toInt() ?? 0;
  } on DioException {
    return 0;
  }
});
