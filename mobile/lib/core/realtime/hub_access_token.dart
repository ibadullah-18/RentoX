import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../network/api_client.dart';
import '../storage/token_storage.dart';

/// Token supplier for SignalR hubs. A token that is about to expire is
/// refreshed by making any authenticated call (the API client's interceptor
/// does the refresh); the new token is then read back from storage.
Future<String?> Function() hubAccessToken(Ref ref) {
  final storage = ref.watch(tokenStorageProvider);
  final dio = ref.watch(dioProvider);

  return () async {
    var tokens = await storage.readTokens();
    if (tokens == null) return null;
    final soon = DateTime.now().toUtc().add(const Duration(seconds: 30));
    if (tokens.accessExpiresAt.isBefore(soon)) {
      try {
        await dio.get<void>('/api/notifications/unread-count');
      } catch (_) {}
      tokens = await storage.readTokens();
    }
    return tokens?.accessToken;
  };
}
