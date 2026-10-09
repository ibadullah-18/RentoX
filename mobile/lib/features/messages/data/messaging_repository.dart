import 'package:dio/dio.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/network/api_client.dart';
import '../../../core/network/api_exception.dart';
import '../../catalog/domain/catalog_models.dart';
import '../../listing_create/domain/photo.dart';
import '../domain/message_models.dart';

class MessagingRepository {
  MessagingRepository(this._dio);

  final Dio _dio;

  Future<Paged<ConversationSummary>> conversations({
    int page = 1,
    int pageSize = 20,
  }) => guardApi(() async {
    final res = await _dio.get<Map<String, dynamic>>(
      '/api/conversations',
      queryParameters: {'page': page, 'pageSize': pageSize},
    );
    return Paged.fromJson(res.data!, ConversationSummary.fromJson);
  });

  Future<int> unreadCount() => guardApi(() async {
    final res = await _dio.get<Map<String, dynamic>>(
      '/api/conversations/unread-count',
    );
    return (res.data?['count'] as num?)?.toInt() ?? 0;
  });

  /// Messages come newest first; page 2 holds the older ones.
  Future<Paged<ChatMessage>> messages(
    String conversationId, {
    int page = 1,
    int pageSize = 30,
  }) => guardApi(() async {
    final res = await _dio.get<Map<String, dynamic>>(
      '/api/conversations/$conversationId/messages',
      queryParameters: {'page': page, 'pageSize': pageSize},
    );
    return Paged.fromJson(res.data!, ChatMessage.fromJson);
  });

  Future<ChatMessage> send(String conversationId, String body) =>
      guardApi(() async {
        final res = await _dio.post<Map<String, dynamic>>(
          '/api/conversations/$conversationId/messages',
          data: {'body': body},
        );
        return ChatMessage.fromJson(res.data!);
      });

  Future<ChatMessage> sendImages(
    String conversationId,
    List<PickedPhoto> photos, {
    String? caption,
  }) => guardApi(() async {
    final form = FormData.fromMap({
      if (caption != null && caption.isNotEmpty) 'body': caption,
      'files': [
        for (final p in photos)
          MultipartFile.fromBytes(
            p.bytes,
            filename: p.name,
            contentType: DioMediaType.parse(p.mimeType),
          ),
      ],
    });
    final res = await _dio.post<Map<String, dynamic>>(
      '/api/conversations/$conversationId/messages/images',
      data: form,
      options: Options(sendTimeout: const Duration(minutes: 2)),
    );
    return ChatMessage.fromJson(res.data!);
  });

  Future<void> markRead(String conversationId) => guardApi(
    () => _dio.patch<void>('/api/conversations/$conversationId/read'),
  );

  Future<bool> isOnline(String conversationId) => guardApi(() async {
    final res = await _dio.get<Map<String, dynamic>>(
      '/api/conversations/$conversationId/presence',
    );
    return (res.data?['isOnline'] as bool?) ?? false;
  });

  Future<BlockStatus> blockStatus(String conversationId) => guardApi(() async {
    final res = await _dio.get<Map<String, dynamic>>(
      '/api/conversations/$conversationId/block',
    );
    return BlockStatus.fromJson(res.data!);
  });

  Future<void> block(String conversationId) => guardApi(
    () => _dio.put<void>('/api/conversations/$conversationId/block'),
  );

  Future<void> unblock(String conversationId) => guardApi(
    () => _dio.delete<void>('/api/conversations/$conversationId/block'),
  );

  Future<void> report(
    String conversationId, {
    required ReportReason reason,
    String? details,
    String? evidenceMessageId,
  }) => guardApi(
    () => _dio.post<void>(
      '/api/conversations/$conversationId/reports',
      data: {
        'reason': reason.id,
        if (details != null && details.trim().isNotEmpty)
          'details': details.trim(),
        'evidenceMessageId': ?evidenceMessageId,
      },
    ),
  );
}

final messagingRepositoryProvider = Provider<MessagingRepository>(
  (ref) => MessagingRepository(ref.watch(dioProvider)),
);
