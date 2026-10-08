import 'package:dio/dio.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/network/api_client.dart';
import '../../../core/network/api_exception.dart';

class ConversationsRepository {
  ConversationsRepository(this._dio);

  final Dio _dio;

  /// Starts (or continues) the conversation with a listing's owner by sending
  /// the first message. Returns the conversation id.
  Future<String> startConversation({
    required String listingId,
    required String body,
  }) => guardApi(() async {
    final res = await _dio.post<Map<String, dynamic>>(
      '/api/conversations',
      data: {'listingId': listingId, 'body': body},
    );
    return res.data!['conversationId'] as String;
  });
}

final conversationsRepositoryProvider = Provider<ConversationsRepository>(
  (ref) => ConversationsRepository(ref.watch(dioProvider)),
);
