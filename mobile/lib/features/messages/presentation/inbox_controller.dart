import 'dart:async';

import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../auth/presentation/auth_controller.dart';
import '../data/messaging_repository.dart';
import '../data/realtime_service.dart';
import '../domain/message_models.dart';
import 'chat_controller.dart';

class InboxState {
  const InboxState({
    required this.items,
    required this.page,
    required this.hasMore,
    this.loadingMore = false,
  });

  final List<ConversationSummary> items;
  final int page;
  final bool hasMore;
  final bool loadingMore;

  InboxState copyWith({
    List<ConversationSummary>? items,
    int? page,
    bool? hasMore,
    bool? loadingMore,
  }) => InboxState(
    items: items ?? this.items,
    page: page ?? this.page,
    hasMore: hasMore ?? this.hasMore,
    loadingMore: loadingMore ?? this.loadingMore,
  );
}

/// The list of conversations, kept live by the realtime connection.
class InboxController extends AsyncNotifier<InboxState> {
  @override
  Future<InboxState> build() async {
    final session = ref.watch(authControllerProvider).value;
    if (session == null) {
      return const InboxState(items: [], page: 1, hasMore: false);
    }
    final me = session.userId;

    final sub = ref.watch(realtimeServiceProvider)?.events.listen((e) {
      _onEvent(e, me);
    });
    ref.onDispose(() => sub?.cancel());

    final first = await ref.read(messagingRepositoryProvider).conversations();
    return InboxState(
      items: first.items,
      page: first.page,
      hasMore: first.hasMore,
    );
  }

  void _onEvent(RealtimeEvent e, String me) {
    final current = state.value;
    if (current == null) return;

    switch (e) {
      case MessageArrived(:final message):
        final index = current.items.indexWhere(
          (c) => c.id == message.conversationId,
        );
        if (index < 0) {
          unawaited(refresh());
          return;
        }
        final c = current.items[index];
        final mine = message.senderId == me;
        // A chat that is open marks messages read itself.
        final chatOpen = ref.exists(chatControllerProvider(c.id));
        final updated = c.copyWith(
          lastMessage: message.body.isNotEmpty ? message.body : null,
          lastMessageAt: message.sentAt,
          unreadCount: mine || chatOpen ? c.unreadCount : c.unreadCount + 1,
          clearLastMessage: message.body.isEmpty,
        );
        final items = [...current.items]..removeAt(index);
        state = AsyncData(current.copyWith(items: [updated, ...items]));
      case MessagesReadEvent(:final conversationId, :final readByUserId):
        if (readByUserId == me) markRead(conversationId);
      case Reconnected():
        unawaited(refresh());
      case TypingChanged():
        break;
    }
  }

  /// Re-fetches the first page (pull to refresh, reconnect, unknown chat).
  Future<void> refresh() async {
    try {
      final first = await ref.read(messagingRepositoryProvider).conversations();
      state = AsyncData(
        InboxState(
          items: first.items,
          page: first.page,
          hasMore: first.hasMore,
        ),
      );
    } catch (e, st) {
      // Keep showing the old list when a background refresh fails.
      if (!state.hasValue) state = AsyncError(e, st);
    }
  }

  Future<void> loadMore() async {
    final current = state.value;
    if (current == null || !current.hasMore || current.loadingMore) return;
    state = AsyncData(current.copyWith(loadingMore: true));
    try {
      final next = await ref
          .read(messagingRepositoryProvider)
          .conversations(page: current.page + 1);
      final known = current.items.map((c) => c.id).toSet();
      state = AsyncData(
        InboxState(
          items: [
            ...current.items,
            ...next.items.where((c) => !known.contains(c.id)),
          ],
          page: next.page,
          hasMore: next.hasMore,
        ),
      );
    } catch (_) {
      state = AsyncData(current.copyWith(loadingMore: false));
    }
  }

  void markRead(String conversationId) {
    final current = state.value;
    if (current == null) return;
    state = AsyncData(
      current.copyWith(
        items: [
          for (final c in current.items)
            c.id == conversationId ? c.copyWith(unreadCount: 0) : c,
        ],
      ),
    );
  }
}

final inboxProvider = AsyncNotifierProvider<InboxController, InboxState>(
  InboxController.new,
);

/// Total unread messages, for the badge on the "Mesajlar" tab.
class UnreadMessagesController extends AsyncNotifier<int> {
  @override
  Future<int> build() async {
    final session = ref.watch(authControllerProvider).value;
    if (session == null) return 0;

    final sub = ref.watch(realtimeServiceProvider)?.events.listen((e) {
      if (e is MessageArrived || e is MessagesReadEvent || e is Reconnected) {
        unawaited(refresh());
      }
    });
    ref.onDispose(() => sub?.cancel());

    try {
      return await ref.read(messagingRepositoryProvider).unreadCount();
    } catch (_) {
      return 0;
    }
  }

  Future<void> refresh() async {
    try {
      state = AsyncData(
        await ref.read(messagingRepositoryProvider).unreadCount(),
      );
    } catch (_) {
      // Keep the last known number.
    }
  }
}

final unreadMessagesProvider =
    AsyncNotifierProvider<UnreadMessagesController, int>(
      UnreadMessagesController.new,
    );
