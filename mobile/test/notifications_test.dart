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
import 'package:rentox/features/notifications/data/notifications_realtime.dart';
import 'package:rentox/features/notifications/data/notifications_repository.dart';
import 'package:rentox/features/notifications/domain/notification_models.dart';
import 'package:rentox/features/notifications/presentation/notifications_controller.dart';
import 'package:rentox/features/notifications/presentation/notifications_page.dart';

const listingId = '11111111-2222-3333-4444-555555555555';

class _SignedIn extends AuthController {
  @override
  Future<AuthSession?> build() async =>
      const AuthSession(userId: 'u1', phoneNumber: '+994501234567');
}

class FakeRealtime extends NotificationsRealtime {
  FakeRealtime()
    : super(
        SignalRConnection(
          baseUrl: 'http://localhost',
          hubPath: '/hubs/notifications',
          accessToken: () async => null,
        ),
      );

  final bus = StreamController<NotificationEvent>.broadcast();

  @override
  Stream<NotificationEvent> get events => bus.stream;

  @override
  void start() {}
}

AppNotification note(
  String id, {
  NotificationKind kind = NotificationKind.listingApproved,
  bool read = false,
  int minutesAgo = 0,
  String? actionUrl,
}) => AppNotification(
  id: id,
  kind: kind,
  title: 'Başlıq $id',
  body: 'Mətn $id',
  createdAt: DateTime.now().toUtc().subtract(Duration(minutes: minutesAgo)),
  readAt: read ? DateTime.now().toUtc() : null,
  relatedEntityId: listingId,
  actionUrl: actionUrl ?? '/listings/$listingId',
);

class FakeRepo extends NotificationsRepository {
  FakeRepo() : super(Dio());

  List<AppNotification> items = [];
  final markedRead = <String>[];
  int markAllCalls = 0;
  bool failMark = false;

  @override
  Future<Paged<AppNotification>> list({
    int page = 1,
    int pageSize = 20,
  }) async =>
      Paged(items: items, page: 1, totalPages: 1, totalCount: items.length);

  @override
  Future<int> unreadCount() async => items.where((n) => !n.isRead).length;

  @override
  Future<void> markRead(String id) async {
    if (failMark) throw Exception('boom');
    markedRead.add(id);
  }

  @override
  Future<void> markAllRead() async => markAllCalls++;
}

class Harness {
  Harness() {
    container = ProviderContainer(
      overrides: [
        authControllerProvider.overrideWith(_SignedIn.new),
        notificationsRepositoryProvider.overrideWithValue(repo),
        notificationsRealtimeProvider.overrideWithValue(realtime),
      ],
    );
  }

  final repo = FakeRepo();
  final realtime = FakeRealtime();
  late final ProviderContainer container;

  Future<void> open() async {
    container.listen(notificationsProvider, (_, _) {});
    container.listen(unreadNotificationsProvider, (_, _) {});
    await container.read(notificationsProvider.future);
    await container.read(unreadNotificationsProvider.future);
  }

  NotificationsState get list =>
      container.read(notificationsProvider).requireValue;
  int get unread => container.read(unreadNotificationsProvider).requireValue;

  Future<void> pump() => Future<void>.delayed(const Duration(milliseconds: 30));
}

void main() {
  test('listing notifications know their listing, others do not', () {
    expect(note('a').listingId, listingId);
    expect(
      note(
        'b',
        kind: NotificationKind.listingRejected,
        actionUrl: null,
      ).listingId,
      listingId,
    );
    expect(note('c', kind: NotificationKind.supportReply).listingId, isNull);
    expect(NotificationKind.fromId(99), NotificationKind.other);
  });

  test('loads the list and the unread count', () async {
    final h = Harness();
    addTearDown(h.container.dispose);
    h.repo.items = [note('1'), note('2', read: true)];
    await h.open();

    expect(h.list.items.map((n) => n.id), ['1', '2']);
    expect(h.unread, 1);
  });

  test('a live notification goes on top and raises the badge', () async {
    final h = Harness();
    addTearDown(h.container.dispose);
    h.repo.items = [note('1', read: true)];
    await h.open();

    h.realtime.bus.add(NotificationArrived(note('new')));
    await h.pump();

    expect(h.list.items.first.id, 'new');
    expect(h.unread, 1);

    // The same notification delivered twice is only shown once.
    h.realtime.bus.add(NotificationArrived(note('new')));
    await h.pump();
    expect(h.list.items.where((n) => n.id == 'new'), hasLength(1));
  });

  test('marking one read updates the list and the badge at once', () async {
    final h = Harness();
    addTearDown(h.container.dispose);
    h.repo.items = [note('1'), note('2')];
    await h.open();
    expect(h.unread, 2);

    await h.container.read(notificationsProvider.notifier).markRead('1');
    expect(h.list.items.first.isRead, isTrue);
    expect(h.unread, 1);
    expect(h.repo.markedRead, ['1']);

    // Already read: no second request, badge unchanged.
    await h.container.read(notificationsProvider.notifier).markRead('1');
    expect(h.repo.markedRead, ['1']);
    expect(h.unread, 1);
  });

  test('mark all read clears everything', () async {
    final h = Harness();
    addTearDown(h.container.dispose);
    h.repo.items = [note('1'), note('2')];
    await h.open();

    await h.container.read(notificationsProvider.notifier).markAllRead();
    expect(h.list.items.every((n) => n.isRead), isTrue);
    expect(h.unread, 0);
    expect(h.repo.markAllCalls, 1);
  });

  testWidgets('page shows notifications and opens the listing', (tester) async {
    final h = Harness();
    addTearDown(h.container.dispose);
    h.repo.items = [
      note('1'),
      note(
        '2',
        read: true,
        kind: NotificationKind.supportReply,
        minutesAgo: 3000,
      ),
    ];

    await tester.pumpWidget(
      UncontrolledProviderScope(
        container: h.container,
        child: const MaterialApp(
          locale: Locale('az'),
          localizationsDelegates: AppL10n.localizationsDelegates,
          supportedLocales: AppL10n.supportedLocales,
          home: NotificationsPage(),
        ),
      ),
    );
    await tester.pump(const Duration(milliseconds: 100));
    await tester.pump(const Duration(milliseconds: 100));

    expect(find.text('Başlıq 1'), findsOneWidget);
    expect(find.text('Başlıq 2'), findsOneWidget);
    expect(find.text('Bu gün'), findsOneWidget);
    expect(find.text('Hamısını oxu'), findsOneWidget);

    await tester.tap(find.text('Hamısını oxu'));
    await tester.pump();
    expect(find.text('Hamısını oxu'), findsNothing);
  });
}
