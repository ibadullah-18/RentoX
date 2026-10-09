import 'dart:async';

import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../auth/presentation/auth_controller.dart';
import '../../notifications/data/notifications_realtime.dart';
import '../../notifications/domain/notification_models.dart';
import '../data/support_repository.dart';
import '../domain/support_models.dart';

class SupportListState {
  const SupportListState({
    required this.items,
    required this.page,
    required this.hasMore,
    this.loadingMore = false,
  });

  final List<SupportTicket> items;
  final int page;
  final bool hasMore;
  final bool loadingMore;

  SupportListState copyWith({
    List<SupportTicket>? items,
    int? page,
    bool? hasMore,
    bool? loadingMore,
  }) => SupportListState(
    items: items ?? this.items,
    page: page ?? this.page,
    hasMore: hasMore ?? this.hasMore,
    loadingMore: loadingMore ?? this.loadingMore,
  );
}

bool _isSupport(NotificationEvent e) =>
    e is NotificationArrived &&
    (e.notification.kind == NotificationKind.supportReply ||
        e.notification.kind == NotificationKind.supportStatusChanged);

/// The user's tickets, newest activity first. Refreshes itself when the team
/// replies (a notification arrives).
class SupportListController extends AsyncNotifier<SupportListState> {
  SupportRepository get _repo => ref.read(supportRepositoryProvider);

  @override
  Future<SupportListState> build() async {
    if (ref.watch(authControllerProvider).value == null) {
      return const SupportListState(items: [], page: 1, hasMore: false);
    }
    final sub = ref.watch(notificationsRealtimeProvider)?.events.listen((e) {
      if (_isSupport(e) || e is NotificationsResync) unawaited(refresh());
    });
    ref.onDispose(() => sub?.cancel());

    final first = await _repo.list();
    return SupportListState(
      items: _sorted(first.items),
      page: first.page,
      hasMore: first.hasMore,
    );
  }

  static List<SupportTicket> _sorted(List<SupportTicket> items) =>
      [...items]..sort((a, b) => b.updatedAt.compareTo(a.updatedAt));

  Future<void> refresh() async {
    try {
      final first = await _repo.list();
      state = AsyncData(
        SupportListState(
          items: _sorted(first.items),
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
      final known = current.items.map((t) => t.id).toSet();
      state = AsyncData(
        SupportListState(
          items: [
            ...current.items,
            ...next.items.where((t) => !known.contains(t.id)),
          ],
          page: next.page,
          hasMore: next.hasMore,
        ),
      );
    } catch (_) {
      state = AsyncData(current.copyWith(loadingMore: false));
    }
  }
}

final supportListProvider =
    AsyncNotifierProvider<SupportListController, SupportListState>(
      SupportListController.new,
    );

/// One ticket with its conversation.
class TicketController extends AsyncNotifier<SupportTicket> {
  TicketController(this.id);

  final String id;

  SupportRepository get _repo => ref.read(supportRepositoryProvider);

  @override
  Future<SupportTicket> build() async {
    final sub = ref.watch(notificationsRealtimeProvider)?.events.listen((e) {
      if (e is NotificationArrived &&
          _isSupport(e) &&
          e.notification.relatedEntityId == id) {
        unawaited(refresh());
      } else if (e is NotificationsResync) {
        unawaited(refresh());
      }
    });
    ref.onDispose(() => sub?.cancel());
    return _repo.details(id);
  }

  Future<void> refresh() async {
    try {
      state = AsyncData(await _repo.details(id));
    } catch (_) {
      // Keep what is on screen.
    }
  }

  /// Sends a reply. Throws on failure so the composer can keep the text.
  Future<void> reply(String body) async {
    await _repo.reply(id, body);
    state = AsyncData(await _repo.details(id));
    ref.invalidate(supportListProvider);
  }
}

final ticketProvider = AsyncNotifierProvider.autoDispose
    .family<TicketController, SupportTicket, String>(TicketController.new);
