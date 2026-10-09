import 'dart:async';

import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../auth/presentation/auth_controller.dart';
import '../data/notifications_realtime.dart';
import '../data/notifications_repository.dart';
import '../domain/notification_models.dart';

class NotificationsState {
  const NotificationsState({
    required this.items,
    required this.page,
    required this.hasMore,
    this.loadingMore = false,
  });

  final List<AppNotification> items;
  final int page;
  final bool hasMore;
  final bool loadingMore;

  int get unread => items.where((n) => !n.isRead).length;

  NotificationsState copyWith({
    List<AppNotification>? items,
    int? page,
    bool? hasMore,
    bool? loadingMore,
  }) => NotificationsState(
    items: items ?? this.items,
    page: page ?? this.page,
    hasMore: hasMore ?? this.hasMore,
    loadingMore: loadingMore ?? this.loadingMore,
  );
}

/// The notification list, newest first, kept live by the hub.
class NotificationsController extends AsyncNotifier<NotificationsState> {
  NotificationsRepository get _repo =>
      ref.read(notificationsRepositoryProvider);

  @override
  Future<NotificationsState> build() async {
    if (ref.watch(authControllerProvider).value == null) {
      return const NotificationsState(items: [], page: 1, hasMore: false);
    }
    final sub = ref
        .watch(notificationsRealtimeProvider)
        ?.events
        .listen(_onEvent);
    ref.onDispose(() => sub?.cancel());

    final first = await _repo.list();
    return NotificationsState(
      items: first.items,
      page: first.page,
      hasMore: first.hasMore,
    );
  }

  void _onEvent(NotificationEvent e) {
    final current = state.value;
    if (current == null) return;
    switch (e) {
      case NotificationArrived(:final notification):
        if (current.items.any((n) => n.id == notification.id)) return;
        state = AsyncData(
          current.copyWith(items: [notification, ...current.items]),
        );
      case NotificationsResync():
        unawaited(refresh());
    }
  }

  Future<void> refresh() async {
    try {
      final first = await _repo.list();
      state = AsyncData(
        NotificationsState(
          items: first.items,
          page: first.page,
          hasMore: first.hasMore,
        ),
      );
    } catch (e, st) {
      if (!state.hasValue) state = AsyncError(e, st);
    }
  }

  Future<void> loadMore() async {
    final current = state.value;
    if (current == null || !current.hasMore || current.loadingMore) return;
    state = AsyncData(current.copyWith(loadingMore: true));
    try {
      final next = await _repo.list(page: current.page + 1);
      final known = current.items.map((n) => n.id).toSet();
      state = AsyncData(
        NotificationsState(
          items: [
            ...current.items,
            ...next.items.where((n) => !known.contains(n.id)),
          ],
          page: next.page,
          hasMore: next.hasMore,
        ),
      );
    } catch (_) {
      state = AsyncData(current.copyWith(loadingMore: false));
    }
  }

  /// Marks one notification read right away; the request follows in the
  /// background (everything is re-synced if it fails).
  Future<void> markRead(String id) async {
    final current = state.value;
    if (current == null) return;
    final target = current.items.where((n) => n.id == id).firstOrNull;
    if (target == null || target.isRead) return;
    state = AsyncData(
      current.copyWith(
        items: [for (final n in current.items) n.id == id ? n.asRead() : n],
      ),
    );
    ref.read(unreadNotificationsProvider.notifier).adjust(-1);
    try {
      await _repo.markRead(id);
    } catch (_) {
      unawaited(refresh());
      unawaited(ref.read(unreadNotificationsProvider.notifier).refresh());
    }
  }

  Future<void> markAllRead() async {
    final current = state.value;
    if (current == null) return;
    state = AsyncData(
      current.copyWith(items: [for (final n in current.items) n.asRead()]),
    );
    ref.read(unreadNotificationsProvider.notifier).set(0);
    try {
      await _repo.markAllRead();
    } catch (_) {
      unawaited(refresh());
      unawaited(ref.read(unreadNotificationsProvider.notifier).refresh());
    }
  }
}

final notificationsProvider =
    AsyncNotifierProvider<NotificationsController, NotificationsState>(
      NotificationsController.new,
    );

/// Unread notifications, for the bell dot.
class UnreadNotificationsController extends AsyncNotifier<int> {
  @override
  Future<int> build() async {
    if (ref.watch(authControllerProvider).value == null) return 0;
    final sub = ref.watch(notificationsRealtimeProvider)?.events.listen((e) {
      switch (e) {
        case NotificationArrived(:final notification):
          if (!notification.isRead) adjust(1);
        case NotificationsResync():
          unawaited(refresh());
      }
    });
    ref.onDispose(() => sub?.cancel());
    try {
      return await ref.read(notificationsRepositoryProvider).unreadCount();
    } catch (_) {
      return 0;
    }
  }

  void adjust(int delta) {
    final now = state.value ?? 0;
    state = AsyncData((now + delta).clamp(0, 1 << 30));
  }

  void set(int value) => state = AsyncData(value);

  Future<void> refresh() async {
    try {
      state = AsyncData(
        await ref.read(notificationsRepositoryProvider).unreadCount(),
      );
    } catch (_) {}
  }
}

final unreadNotificationsProvider =
    AsyncNotifierProvider<UnreadNotificationsController, int>(
      UnreadNotificationsController.new,
    );
