import 'dart:async';

import 'package:dio/dio.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:rentox/core/l10n/app_localizations.dart';
import 'package:rentox/core/realtime/signalr_connection.dart';
import 'package:rentox/features/auth/domain/auth_models.dart';
import 'package:rentox/features/auth/presentation/auth_controller.dart';
import 'package:rentox/features/catalog/domain/catalog_models.dart';
import 'package:rentox/features/listing_create/domain/photo.dart';
import 'package:rentox/features/messages/data/messaging_repository.dart';
import 'package:rentox/features/messages/data/realtime_service.dart';
import 'package:rentox/features/messages/domain/message_models.dart';
import 'package:rentox/features/messages/presentation/chat_controller.dart';
import 'package:rentox/features/messages/presentation/inbox_controller.dart';
import 'package:rentox/features/messages/presentation/messages_page.dart';

const me = 'me';
const other = 'other';
const cid = 'c1';

class _SignedIn extends AuthController {
  @override
  Future<AuthSession?> build() async =>
      const AuthSession(userId: me, phoneNumber: '+994501234567');
}

class FakeRealtime extends RealtimeService {
  FakeRealtime()
    : super(
        SignalRConnection(
          baseUrl: 'http://localhost',
          hubPath: '/hubs/conversations',
          accessToken: () async => null,
        ),
      );

  final bus = StreamController<RealtimeEvent>.broadcast();
  final typingStarts = <String>[];

  @override
  Stream<RealtimeEvent> get events => bus.stream;

  @override
  void start() {}

  @override
  void startTyping(String conversationId) => typingStarts.add(conversationId);

  @override
  void stopTyping(String conversationId) {}
}

ChatMessage msg(
  String id,
  String sender,
  String body, {
  int minutesAgo = 0,
  DateTime? readAt,
}) => ChatMessage(
  id: id,
  conversationId: cid,
  senderId: sender,
  body: body,
  sentAt: DateTime.now().toUtc().subtract(Duration(minutes: minutesAgo)),
  readAt: readAt,
);

class FakeMessaging extends MessagingRepository {
  FakeMessaging() : super(Dio());

  List<ChatMessage> newest = [];
  List<ChatMessage> older = [];
  List<ConversationSummary> inbox = [];
  int unread = 0;
  int readCalls = 0;
  bool failSend = false;
  Completer<void>? sendGate;
  final sentBodies = <String>[];
  BlockStatus blockState = BlockStatus.open;
  int _ids = 0;

  @override
  Future<Paged<ChatMessage>> messages(
    String conversationId, {
    int page = 1,
    int pageSize = 30,
  }) async {
    final items = page == 1 ? newest : older;
    return Paged(
      items: items,
      page: page,
      totalPages: older.isEmpty ? 1 : 2,
      totalCount: items.length,
    );
  }

  @override
  Future<Paged<ConversationSummary>> conversations({
    int page = 1,
    int pageSize = 20,
  }) async =>
      Paged(items: inbox, page: 1, totalPages: 1, totalCount: inbox.length);

  @override
  Future<int> unreadCount() async => unread;

  @override
  Future<void> markRead(String conversationId) async => readCalls++;

  @override
  Future<void> block(String conversationId) async {}

  @override
  Future<void> unblock(String conversationId) async {}

  @override
  Future<bool> isOnline(String conversationId) async => true;

  @override
  Future<BlockStatus> blockStatus(String conversationId) async => blockState;

  @override
  Future<ChatMessage> send(String conversationId, String body) async {
    sentBodies.add(body);
    await sendGate?.future;
    if (failSend) throw Exception('boom');
    return msg('srv-${_ids++}', me, body);
  }

  @override
  Future<ChatMessage> sendImages(
    String conversationId,
    List<PickedPhoto> photos, {
    String? caption,
  }) async => msg('srv-img-${_ids++}', me, caption ?? '');
}

class Harness {
  Harness() {
    container = ProviderContainer(
      overrides: [
        authControllerProvider.overrideWith(_SignedIn.new),
        messagingRepositoryProvider.overrideWithValue(repo),
        realtimeServiceProvider.overrideWithValue(realtime),
      ],
    );
  }

  final repo = FakeMessaging();
  final realtime = FakeRealtime();
  late final ProviderContainer container;

  ChatController get chat =>
      container.read(chatControllerProvider(cid).notifier);

  ProviderSubscription<AsyncValue<ChatState>> open() =>
      container.listen(chatControllerProvider(cid), (_, _) {});

  ChatState get state =>
      container.read(chatControllerProvider(cid)).requireValue;

  Future<void> ready() async {
    await container.read(authControllerProvider.future);
    await container.read(chatControllerProvider(cid).future);
  }

  Future<void> pump() => Future<void>.delayed(const Duration(milliseconds: 30));

  void emit(RealtimeEvent e) => realtime.bus.add(e);

  void dispose() => container.dispose();
}

void main() {
  test('loads the newest page and marks incoming messages as read', () async {
    final h = Harness();
    addTearDown(h.dispose);
    h.repo.newest = [
      msg('2', other, 'salam', minutesAgo: 1),
      msg('1', me, 'hi', minutesAgo: 5),
    ];
    final sub = h.open();
    addTearDown(sub.close);
    await h.ready();

    expect(h.state.messages.map((m) => m.id), ['2', '1']);
    expect(h.state.online, isTrue);

    await Future<void>.delayed(const Duration(milliseconds: 500));
    expect(h.repo.readCalls, 1);
  });

  test(
    'a sent message appears at once and is confirmed by the server',
    () async {
      final h = Harness();
      addTearDown(h.dispose);
      final sub = h.open();
      addTearDown(sub.close);
      await h.ready();

      h.repo.sendGate = Completer<void>();
      final sending = h.chat.sendText('  salam  ');
      await h.pump();

      expect(h.state.messages.single.sendState, SendState.sending);
      expect(h.state.messages.single.body, 'salam');

      h.repo.sendGate!.complete();
      await sending;

      final m = h.state.messages.single;
      expect(m.sendState, SendState.sent);
      expect(m.id, startsWith('srv-'));
    },
  );

  test('the realtime echo of my own message is not shown twice', () async {
    final h = Harness();
    addTearDown(h.dispose);
    final sub = h.open();
    addTearDown(sub.close);
    await h.ready();

    h.repo.sendGate = Completer<void>();
    final sending = h.chat.sendText('salam');
    await h.pump();

    // The echo arrives before the HTTP reply.
    h.emit(MessageArrived(msg('srv-0', me, 'salam')));
    await h.pump();
    h.repo.sendGate!.complete();
    await sending;

    expect(h.state.messages, hasLength(1));
    expect(h.state.messages.single.id, 'srv-0');
  });

  test('a failed send can be retried or discarded', () async {
    final h = Harness();
    addTearDown(h.dispose);
    final sub = h.open();
    addTearDown(sub.close);
    await h.ready();

    h.repo.failSend = true;
    await h.chat.sendText('salam');
    final failed = h.state.messages.single;
    expect(failed.sendState, SendState.failed);

    h.repo.failSend = false;
    await h.chat.retry(failed.id);
    expect(h.state.messages.single.sendState, SendState.sent);
    expect(h.repo.sentBodies, ['salam', 'salam']);

    h.repo.failSend = true;
    await h.chat.sendText('again');
    h.chat.discard(h.state.messages.first.id);
    expect(h.state.messages.map((m) => m.body), ['salam']);
  });

  test('empty and over-long messages are not sent', () async {
    final h = Harness();
    addTearDown(h.dispose);
    final sub = h.open();
    addTearDown(sub.close);
    await h.ready();

    await h.chat.sendText('   ');
    await h.chat.sendText('x' * (maxMessageLength + 1));
    expect(h.repo.sentBodies, isEmpty);
    expect(h.state.messages, isEmpty);
  });

  test('incoming messages show up and are marked read while open', () async {
    final h = Harness();
    addTearDown(h.dispose);
    final sub = h.open();
    addTearDown(sub.close);
    await h.ready();

    h.emit(MessageArrived(msg('9', other, 'salam')));
    await Future<void>.delayed(const Duration(milliseconds: 500));

    expect(h.state.messages.single.body, 'salam');
    expect(h.repo.readCalls, 1);
  });

  test('messages for another conversation are ignored', () async {
    final h = Harness();
    addTearDown(h.dispose);
    final sub = h.open();
    addTearDown(sub.close);
    await h.ready();

    h.emit(
      MessageArrived(
        ChatMessage(
          id: 'x',
          conversationId: 'other-conv',
          senderId: other,
          body: 'hey',
          sentAt: DateTime.now().toUtc(),
        ),
      ),
    );
    await h.pump();
    expect(h.state.messages, isEmpty);
  });

  test('a read receipt marks my messages as read', () async {
    final h = Harness();
    addTearDown(h.dispose);
    h.repo.newest = [msg('1', me, 'hi')];
    final sub = h.open();
    addTearDown(sub.close);
    await h.ready();
    expect(h.state.messages.single.isRead, isFalse);

    h.emit(
      MessagesReadEvent(
        conversationId: cid,
        readByUserId: other,
        readAt: DateTime.now().toUtc(),
      ),
    );
    await h.pump();
    expect(h.state.messages.single.isRead, isTrue);
  });

  test('typing indicator turns on and off', () async {
    final h = Harness();
    addTearDown(h.dispose);
    final sub = h.open();
    addTearDown(sub.close);
    await h.ready();

    h.emit(
      const TypingChanged(conversationId: cid, userId: other, typing: true),
    );
    await h.pump();
    expect(h.state.otherTyping, isTrue);

    h.emit(
      const TypingChanged(conversationId: cid, userId: other, typing: false),
    );
    await h.pump();
    expect(h.state.otherTyping, isFalse);
  });

  test('my own typing is announced once per burst', () async {
    final h = Harness();
    addTearDown(h.dispose);
    final sub = h.open();
    addTearDown(sub.close);
    await h.ready();

    h.chat
      ..typing()
      ..typing()
      ..typing();
    expect(h.realtime.typingStarts, [cid]);
  });

  test('older messages are appended without duplicates', () async {
    final h = Harness();
    addTearDown(h.dispose);
    h.repo.newest = [msg('3', me, 'c'), msg('2', me, 'b')];
    h.repo.older = [msg('2', me, 'b'), msg('1', me, 'a')];
    final sub = h.open();
    addTearDown(sub.close);
    await h.ready();
    expect(h.state.hasMore, isTrue);

    await h.chat.loadOlder();
    expect(h.state.messages.map((m) => m.id), ['3', '2', '1']);
  });

  test('blocking updates the block state', () async {
    final h = Harness();
    addTearDown(h.dispose);
    final sub = h.open();
    addTearDown(sub.close);
    await h.ready();
    expect(h.state.block.canSend, isTrue);

    h.repo.blockState = const BlockStatus(isBlockedByMe: true, canSend: false);
    await h.chat.setBlocked(true);
    expect(h.state.block.isBlockedByMe, isTrue);
  });

  group('inbox', () {
    ConversationSummary conv(String id, {int unread = 0, String text = 'x'}) =>
        ConversationSummary(
          id: id,
          listingId: 'l-$id',
          listingTitle: 'Toyota $id',
          otherUserId: other,
          unreadCount: unread,
          lastMessage: text,
          lastMessageAt: DateTime.now().toUtc(),
        );

    test(
      'a new message moves its conversation to the top and counts unread',
      () async {
        final h = Harness();
        addTearDown(h.dispose);
        h.repo.inbox = [conv('a'), conv('b')];
        final sub = h.container.listen(inboxProvider, (_, _) {});
        addTearDown(sub.close);
        await h.container.read(inboxProvider.future);

        h.emit(
          MessageArrived(
            ChatMessage(
              id: 'm',
              conversationId: 'b',
              senderId: other,
              body: 'salam',
              sentAt: DateTime.now().toUtc(),
            ),
          ),
        );
        await h.pump();

        final items = h.container.read(inboxProvider).requireValue.items;
        expect(items.map((c) => c.id), ['b', 'a']);
        expect(items.first.unreadCount, 1);
        expect(items.first.lastMessage, 'salam');
      },
    );

    test('my own messages do not add unread', () async {
      final h = Harness();
      addTearDown(h.dispose);
      h.repo.inbox = [conv('a')];
      final sub = h.container.listen(inboxProvider, (_, _) {});
      addTearDown(sub.close);
      await h.container.read(inboxProvider.future);

      h.emit(
        MessageArrived(
          ChatMessage(
            id: 'm',
            conversationId: 'a',
            senderId: me,
            body: 'hi',
            sentAt: DateTime.now().toUtc(),
          ),
        ),
      );
      await h.pump();
      expect(
        h.container.read(inboxProvider).requireValue.items.single.unreadCount,
        0,
      );
    });

    test('an unknown conversation triggers a reload', () async {
      final h = Harness();
      addTearDown(h.dispose);
      h.repo.inbox = [conv('a')];
      final sub = h.container.listen(inboxProvider, (_, _) {});
      addTearDown(sub.close);
      await h.container.read(inboxProvider.future);

      h.repo.inbox = [conv('new'), conv('a')];
      h.emit(
        MessageArrived(
          ChatMessage(
            id: 'm',
            conversationId: 'new',
            senderId: other,
            body: 'hello',
            sentAt: DateTime.now().toUtc(),
          ),
        ),
      );
      await h.pump();
      expect(
        h.container.read(inboxProvider).requireValue.items.map((c) => c.id),
        ['new', 'a'],
      );
    });

    test('the unread badge follows the server', () async {
      final h = Harness();
      addTearDown(h.dispose);
      h.repo.unread = 2;
      final sub = h.container.listen(unreadMessagesProvider, (_, _) {});
      addTearDown(sub.close);
      expect(await h.container.read(unreadMessagesProvider.future), 2);

      h.repo.unread = 5;
      h.emit(MessageArrived(msg('z', other, 'hey')));
      await h.pump();
      expect(h.container.read(unreadMessagesProvider).value, 5);
    });
  });

  testWidgets('inbox page lists conversations and filters unread', (
    tester,
  ) async {
    final h = Harness();
    addTearDown(h.dispose);
    h.repo.inbox = [
      ConversationSummary(
        id: 'a',
        listingId: 'la',
        listingTitle: 'Toyota Camry',
        otherUserId: other,
        unreadCount: 3,
        lastMessage: 'Salam, aktualdır?',
        lastMessageAt: DateTime.now().toUtc(),
      ),
      ConversationSummary(
        id: 'b',
        listingId: 'lb',
        listingTitle: 'Menzil Nizami',
        otherUserId: other,
        unreadCount: 0,
        lastMessage: 'Sağ olun',
        lastMessageAt: DateTime.now().toUtc().subtract(const Duration(days: 1)),
      ),
    ];

    await tester.pumpWidget(
      UncontrolledProviderScope(
        container: h.container,
        child: const MaterialApp(
          locale: Locale('az'),
          localizationsDelegates: AppL10n.localizationsDelegates,
          supportedLocales: AppL10n.supportedLocales,
          home: MessagesPage(),
        ),
      ),
    );
    await tester.pump(const Duration(milliseconds: 100));
    await tester.pump(const Duration(milliseconds: 100));

    expect(find.text('Toyota Camry'), findsOneWidget);
    expect(find.text('Menzil Nizami'), findsOneWidget);
    expect(find.text('3'), findsOneWidget);

    await tester.tap(find.text('Oxunmamış'));
    await tester.pump();
    expect(find.text('Toyota Camry'), findsOneWidget);
    expect(find.text('Menzil Nizami'), findsNothing);
  });
}
