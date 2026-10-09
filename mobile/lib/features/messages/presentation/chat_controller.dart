import 'dart:async';

import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../auth/presentation/auth_controller.dart';
import '../../catalog/domain/catalog_models.dart';
import '../../listing_create/domain/photo.dart';
import '../data/messaging_repository.dart';
import '../data/realtime_service.dart';
import '../domain/message_models.dart';
import 'inbox_controller.dart';

class ChatState {
  const ChatState({
    required this.messages,
    required this.page,
    required this.hasMore,
    this.loadingMore = false,
    this.block = BlockStatus.open,
    this.online = false,
    this.otherTyping = false,
  });

  /// Newest first (the list is shown reversed, so index 0 sits at the bottom).
  final List<ChatMessage> messages;
  final int page;
  final bool hasMore;
  final bool loadingMore;
  final BlockStatus block;
  final bool online;
  final bool otherTyping;

  ChatState copyWith({
    List<ChatMessage>? messages,
    int? page,
    bool? hasMore,
    bool? loadingMore,
    BlockStatus? block,
    bool? online,
    bool? otherTyping,
  }) => ChatState(
    messages: messages ?? this.messages,
    page: page ?? this.page,
    hasMore: hasMore ?? this.hasMore,
    loadingMore: loadingMore ?? this.loadingMore,
    block: block ?? this.block,
    online: online ?? this.online,
    otherTyping: otherTyping ?? this.otherTyping,
  );
}

/// One open conversation: history, live updates, optimistic sending, typing
/// and presence.
class ChatController extends AsyncNotifier<ChatState> {
  ChatController(this.conversationId);

  final String conversationId;

  static const _pollPresence = Duration(seconds: 30);
  static const _typingTimeout = Duration(seconds: 6);
  static const _typingIdle = Duration(seconds: 3);

  late final String _me;
  int _localCounter = 0;
  final _pendingPhotos = <String, List<PickedPhoto>>{};
  Timer? _otherTypingTimer;
  Timer? _myTypingTimer;
  bool _iAmTyping = false;
  Timer? _readTimer;

  MessagingRepository get _repo => ref.read(messagingRepositoryProvider);

  /// Captured in [build]: providers must not be read while disposing.
  RealtimeService? _realtime;
  bool _disposed = false;

  @override
  Future<ChatState> build() async {
    _me = ref.watch(authControllerProvider).value!.userId;

    _disposed = false;
    _realtime = ref.watch(realtimeServiceProvider);
    final sub = _realtime?.events.listen(_onEvent);
    final presence = Timer.periodic(_pollPresence, (_) => _refreshPresence());
    ref.onDispose(() {
      _disposed = true;
      sub?.cancel();
      presence.cancel();
      _otherTypingTimer?.cancel();
      _readTimer?.cancel();
      _myTypingTimer?.cancel();
      if (_iAmTyping) _realtime?.stopTyping(conversationId);
    });

    final results = await Future.wait([
      _repo.messages(conversationId),
      _repo.blockStatus(conversationId).catchError((_) => BlockStatus.open),
      _repo.isOnline(conversationId).catchError((_) => false),
    ]);
    final page = results[0] as Paged<ChatMessage>;
    final state = ChatState(
      messages: List<ChatMessage>.of(page.items),
      page: page.page,
      hasMore: page.hasMore,
      block: results[1] as BlockStatus,
      online: results[2] as bool,
    );
    _scheduleRead(state.messages);
    return state;
  }

  // ---- realtime ---------------------------------------------------------

  void _onEvent(RealtimeEvent e) {
    final current = state.value;
    if (current == null) return;

    switch (e) {
      case MessageArrived(:final message):
        if (message.conversationId != conversationId) return;
        _insert(message);
        if (message.senderId != _me) {
          _setOtherTyping(false);
          _scheduleRead([message]);
        }
      case MessagesReadEvent(:final conversationId, :final readByUserId):
        if (conversationId != this.conversationId || readByUserId == _me) {
          return;
        }
        final now = DateTime.now().toUtc();
        state = AsyncData(
          current.copyWith(
            messages: [
              for (final m in current.messages)
                m.senderId == _me && !m.isRead && m.sendState == SendState.sent
                    ? m.copyWith(readAt: now)
                    : m,
            ],
          ),
        );
      case TypingChanged(:final conversationId, :final userId, :final typing):
        if (conversationId == this.conversationId && userId != _me) {
          _setOtherTyping(typing);
        }
      case Reconnected():
        unawaited(refresh());
    }
  }

  void _setOtherTyping(bool typing) {
    final current = state.value;
    if (current == null) return;
    _otherTypingTimer?.cancel();
    if (typing) {
      _otherTypingTimer = Timer(_typingTimeout, () => _setOtherTyping(false));
    }
    if (current.otherTyping != typing) {
      state = AsyncData(current.copyWith(otherTyping: typing));
    }
  }

  /// Adds a message unless it is already there (REST reply and SignalR echo
  /// both deliver the same message).
  void _insert(ChatMessage message) {
    final current = state.value;
    if (current == null) return;
    if (current.messages.any((m) => m.id == message.id)) return;
    state = AsyncData(
      current.copyWith(messages: [message, ...current.messages]),
    );
  }

  // ---- read receipts ----------------------------------------------------

  /// Marks the conversation read shortly after incoming messages are seen
  /// (debounced so a burst of messages costs one request).
  void _scheduleRead(List<ChatMessage> seen) {
    final hasIncoming = seen.any((m) => m.senderId != _me && !m.isRead);
    if (!hasIncoming) return;
    _readTimer?.cancel();
    _readTimer = Timer(const Duration(milliseconds: 400), () async {
      try {
        await _repo.markRead(conversationId);
        if (_disposed) return;
        ref.read(inboxProvider.notifier).markRead(conversationId);
        unawaited(ref.read(unreadMessagesProvider.notifier).refresh());
      } catch (_) {
        // Will be retried the next time something arrives.
      }
    });
  }

  // ---- history ----------------------------------------------------------

  /// Re-fetches the newest page and merges it with what is on screen.
  Future<void> refresh() async {
    final current = state.value;
    if (current == null) return;
    try {
      final fresh = await _repo.messages(conversationId);
      final known = {for (final m in fresh.items) m.id};
      final pending = current.messages.where(
        (m) => m.sendState != SendState.sent,
      );
      final older = current.messages.where(
        (m) => m.sendState == SendState.sent && !known.contains(m.id),
      );
      // Everything older than the fresh page is kept; the fresh page wins.
      final oldest = fresh.items.isEmpty ? null : fresh.items.last.sentAt;
      final merged = [
        ...pending,
        ...fresh.items,
        ...older.where((m) => oldest != null && m.sentAt.isBefore(oldest)),
      ];
      state = AsyncData(current.copyWith(messages: merged));
      _scheduleRead(fresh.items);
      unawaited(_refreshPresence());
    } catch (_) {
      // Keep the current view.
    }
  }

  Future<void> loadOlder() async {
    final current = state.value;
    if (current == null || !current.hasMore || current.loadingMore) return;
    state = AsyncData(current.copyWith(loadingMore: true));
    try {
      final next = await _repo.messages(conversationId, page: current.page + 1);
      final known = current.messages.map((m) => m.id).toSet();
      state = AsyncData(
        current.copyWith(
          messages: [
            ...current.messages,
            ...next.items.where((m) => !known.contains(m.id)),
          ],
          page: next.page,
          hasMore: next.hasMore,
          loadingMore: false,
        ),
      );
    } catch (_) {
      state = AsyncData(current.copyWith(loadingMore: false));
    }
  }

  Future<void> _refreshPresence() async {
    try {
      final online = await _repo.isOnline(conversationId);
      final current = state.value;
      if (current != null && current.online != online) {
        state = AsyncData(current.copyWith(online: online));
      }
    } catch (_) {}
  }

  // ---- sending ----------------------------------------------------------

  ChatMessage _pending({
    required String body,
    List<PickedPhoto> photos = const [],
  }) {
    return ChatMessage(
      id: 'local-${_localCounter++}',
      conversationId: conversationId,
      senderId: _me,
      body: body,
      sentAt: DateTime.now().toUtc(),
      sendState: SendState.sending,
      localImages: [for (final p in photos) p.bytes],
    );
  }

  Future<void> sendText(String text) async {
    final body = text.trim();
    if (body.isEmpty || body.length > maxMessageLength) return;
    stopTyping();
    final pending = _pending(body: body);
    _addPending(pending);
    await _deliver(pending);
  }

  Future<void> sendImages(List<PickedPhoto> photos, {String? caption}) async {
    if (photos.isEmpty) return;
    stopTyping();
    final pending = _pending(
      body: (caption ?? '').trim(),
      photos: photos.take(maxChatImages).toList(),
    );
    _pendingPhotos[pending.id] = photos.take(maxChatImages).toList();
    _addPending(pending);
    await _deliver(pending);
  }

  void _addPending(ChatMessage m) {
    final current = state.value;
    if (current == null) return;
    state = AsyncData(current.copyWith(messages: [m, ...current.messages]));
  }

  Future<void> _deliver(ChatMessage pending) async {
    try {
      final photos = _pendingPhotos[pending.id];
      final sent = photos == null
          ? await _repo.send(conversationId, pending.body)
          : await _repo.sendImages(
              conversationId,
              photos,
              caption: pending.body,
            );
      _pendingPhotos.remove(pending.id);
      if (_disposed) return;
      _replace(pending.id, sent);
      unawaited(ref.read(inboxProvider.notifier).refresh());
    } catch (_) {
      _setSendState(pending.id, SendState.failed);
    }
  }

  void _replace(String localId, ChatMessage real) {
    final current = state.value;
    if (current == null) return;
    final withoutLocal = current.messages
        .where((m) => m.id != localId)
        .toList();
    // The echo over SignalR may already have added the real message.
    final exists = withoutLocal.any((m) => m.id == real.id);
    state = AsyncData(
      current.copyWith(
        messages: exists ? withoutLocal : _insertSorted(withoutLocal, real),
      ),
    );
  }

  List<ChatMessage> _insertSorted(List<ChatMessage> list, ChatMessage m) {
    final out = [...list];
    var i = 0;
    while (i < out.length && out[i].sentAt.isAfter(m.sentAt)) {
      i++;
    }
    out.insert(i, m);
    return out;
  }

  void _setSendState(String id, SendState sendState) {
    final current = state.value;
    if (current == null) return;
    state = AsyncData(
      current.copyWith(
        messages: [
          for (final m in current.messages)
            m.id == id ? m.copyWith(sendState: sendState) : m,
        ],
      ),
    );
  }

  Future<void> retry(String localId) async {
    final current = state.value;
    final index = current?.messages.indexWhere((m) => m.id == localId) ?? -1;
    if (current == null || index < 0) return;
    final message = current.messages[index];
    if (message.sendState != SendState.failed) return;
    _setSendState(localId, SendState.sending);
    await _deliver(message);
  }

  void discard(String localId) {
    final current = state.value;
    if (current == null) return;
    _pendingPhotos.remove(localId);
    state = AsyncData(
      current.copyWith(
        messages: current.messages.where((m) => m.id != localId).toList(),
      ),
    );
  }

  // ---- typing -----------------------------------------------------------

  /// Call on every text change; tells the other side "typing…" and stops on
  /// its own after a short pause.
  void typing() {
    final realtime = _realtime;
    if (realtime == null) return;
    if (!_iAmTyping) {
      _iAmTyping = true;
      realtime.startTyping(conversationId);
    }
    _myTypingTimer?.cancel();
    _myTypingTimer = Timer(_typingIdle, stopTyping);
  }

  void stopTyping() {
    _myTypingTimer?.cancel();
    if (_iAmTyping) {
      _iAmTyping = false;
      _realtime?.stopTyping(conversationId);
    }
  }

  // ---- block / report ---------------------------------------------------

  Future<void> setBlocked(bool blocked) async {
    if (blocked) {
      await _repo.block(conversationId);
    } else {
      await _repo.unblock(conversationId);
    }
    final status = await _repo.blockStatus(conversationId);
    final current = state.value;
    if (current != null) state = AsyncData(current.copyWith(block: status));
  }

  Future<void> report(
    ReportReason reason, {
    String? details,
    String? evidenceMessageId,
  }) => _repo.report(
    conversationId,
    reason: reason,
    details: details,
    evidenceMessageId: evidenceMessageId,
  );
}

final chatControllerProvider = AsyncNotifierProvider.autoDispose
    .family<ChatController, ChatState, String>(ChatController.new);
