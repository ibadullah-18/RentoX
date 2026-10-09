import 'dart:async';

import 'package:dio/dio.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:go_router/go_router.dart';
import 'package:rentox/core/l10n/app_localizations.dart';
import 'package:rentox/core/realtime/signalr_connection.dart';
import 'package:rentox/features/auth/domain/auth_models.dart';
import 'package:rentox/features/auth/presentation/auth_controller.dart';
import 'package:rentox/features/catalog/domain/catalog_models.dart';
import 'package:rentox/features/notifications/data/notifications_realtime.dart';
import 'package:rentox/features/notifications/domain/notification_models.dart';
import 'package:rentox/features/notifications/domain/push_target.dart';
import 'package:rentox/features/support/data/support_repository.dart';
import 'package:rentox/features/support/domain/support_models.dart';
import 'package:rentox/features/support/presentation/new_ticket_page.dart';
import 'package:rentox/features/support/presentation/support_controller.dart';
import 'package:rentox/features/support/presentation/support_page.dart';
import 'package:rentox/features/support/presentation/ticket_page.dart';

const tid = '11111111-2222-3333-4444-555555555555';

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

SupportTicket ticket({
  String id = tid,
  SupportStatus status = SupportStatus.open,
  List<SupportMessage> messages = const [],
  String subject = 'Ödəniş keçmədi',
}) => SupportTicket(
  id: id,
  category: SupportCategory.payment,
  subject: subject,
  status: status,
  createdAt: DateTime.now().toUtc(),
  updatedAt: DateTime.now().toUtc(),
  messages: messages,
);

SupportMessage msg(
  String id,
  String body, {
  bool admin = false,
  int minAgo = 0,
}) => SupportMessage(
  id: id,
  body: body,
  isAdmin: admin,
  sentAt: DateTime.now().toUtc().subtract(Duration(minutes: minAgo)),
);

class FakeSupport extends SupportRepository {
  FakeSupport() : super(Dio());

  List<SupportTicket> tickets = [];
  SupportTicket? current;
  final created =
      <({SupportCategory category, String subject, String message})>[];
  final replies = <String>[];
  bool failCreate = false;
  bool failReply = false;

  @override
  Future<Paged<SupportTicket>> list({int page = 1, int pageSize = 20}) async =>
      Paged(items: tickets, page: 1, totalPages: 1, totalCount: tickets.length);

  @override
  Future<SupportTicket> details(String id) async => current ?? ticket(id: id);

  @override
  Future<String> create({
    required SupportCategory category,
    required String subject,
    required String message,
  }) async {
    if (failCreate) throw Exception('boom');
    created.add((category: category, subject: subject, message: message));
    return tid;
  }

  @override
  Future<void> reply(String id, String body) async {
    if (failReply) throw Exception('boom');
    replies.add(body);
    final c = current ?? ticket(id: id);
    current = SupportTicket(
      id: c.id,
      category: c.category,
      subject: c.subject,
      status: SupportStatus.open,
      createdAt: c.createdAt,
      updatedAt: DateTime.now().toUtc(),
      messages: [...c.messages, msg('new-${replies.length}', body.trim())],
    );
  }
}

ProviderContainer make(FakeSupport repo, FakeRealtime rt) => ProviderContainer(
  overrides: [
    authControllerProvider.overrideWith(_SignedIn.new),
    supportRepositoryProvider.overrideWithValue(repo),
    notificationsRealtimeProvider.overrideWithValue(rt),
  ],
);

Widget app(ProviderContainer c, Widget home) => UncontrolledProviderScope(
  container: c,
  child: MaterialApp.router(
    locale: const Locale('az'),
    localizationsDelegates: AppL10n.localizationsDelegates,
    supportedLocales: AppL10n.supportedLocales,
    routerConfig: GoRouter(
      routes: [
        GoRoute(path: '/', builder: (_, _) => home),
        GoRoute(
          path: '/support/new',
          builder: (_, _) => const Scaffold(body: Text('NEW PAGE')),
        ),
        GoRoute(
          path: '/support/:id',
          builder: (_, s) =>
              Scaffold(body: Text('TICKET ${s.pathParameters['id']}')),
        ),
      ],
    ),
  ),
);

void tall(WidgetTester t) {
  t.view.physicalSize = const Size(1080, 3000);
  t.view.devicePixelRatio = 3;
  addTearDown(t.view.reset);
}

void main() {
  final az = lookupAppL10n(const Locale('az'));

  group('validation and body', () {
    test('subject and message are required, with the backend limits', () {
      expect(validateTicket(subject: '', body: ''), {
        SupportIssue.subjectRequired,
        SupportIssue.bodyRequired,
      });
      expect(validateTicket(subject: 'x' * 161, body: 'ok'), {
        SupportIssue.subjectTooLong,
      });
      expect(validateTicket(subject: 'ok', body: 'x' * 4001), {
        SupportIssue.bodyTooLong,
      });
      expect(validateTicket(subject: 'ok', body: 'x' * 4000), isEmpty);
    });

    test('the reference counts towards the length and is appended', () {
      expect(composeBody('  Salam ', ''), 'Salam');
      expect(
        composeBody('Salam', 'Elan: BMW (ID: 1)'),
        'Salam\n\n— Elan: BMW (ID: 1)',
      );
      expect(
        validateTicket(subject: 's', body: 'x' * 3990, reference: 'r' * 30),
        {SupportIssue.bodyTooLong},
      );
    });

    test('closed requests accept no replies', () {
      expect(SupportStatus.closed.canReply, isFalse);
      expect(SupportStatus.resolved.canReply, isTrue);
    });

    test('parses the API, oldest message first', () {
      final t = SupportTicket.fromJson({
        'id': tid,
        'category': 4,
        'subject': 'S',
        'status': 2,
        'createdAtUtc': '2026-10-01T10:00:00Z',
        'updatedAtUtc': null,
        'messages': [
          {
            'id': 'b',
            'body': 'second',
            'isAdmin': true,
            'sentAtUtc': '2026-10-01T11:00:00Z',
          },
          {
            'id': 'a',
            'body': 'first',
            'isAdmin': false,
            'sentAtUtc': '2026-10-01T10:00:00Z',
          },
        ],
      });
      expect(t.category, SupportCategory.payment);
      expect(t.status, SupportStatus.inProgress);
      expect(t.messages.map((m) => m.body), ['first', 'second']);
    });
  });

  group('links from notifications and push', () {
    test('a support notification knows its ticket', () {
      final n = AppNotification(
        id: 'n',
        kind: NotificationKind.supportReply,
        title: 't',
        body: 'b',
        createdAt: DateTime.now(),
        relatedEntityId: tid,
        actionUrl: '/support/tickets/$tid',
      );
      expect(n.ticketId, tid);
      expect(n.listingId, isNull);
      expect(
        AppNotification(
          id: 'n',
          kind: NotificationKind.listingApproved,
          title: 't',
          body: 'b',
          createdAt: DateTime.now(),
          relatedEntityId: tid,
        ).ticketId,
        isNull,
      );
    });

    test('a tapped push opens the ticket', () {
      final t = PushTarget.fromData({
        'notificationId': 'n1',
        'actionUrl': '/support/tickets/$tid',
      });
      expect(t.ticketId, tid);
      expect(t.listingId, isNull);
    });
  });

  group('SupportListController', () {
    test('refreshes by itself when the team replies', () async {
      final repo = FakeSupport()..tickets = [ticket()];
      final rt = FakeRealtime();
      final c = make(repo, rt);
      addTearDown(c.dispose);
      c.listen(supportListProvider, (_, _) {});
      await c.read(supportListProvider.future);
      expect(c.read(supportListProvider).requireValue.items, hasLength(1));

      repo.tickets = [ticket(), ticket(id: 'other', subject: 'Yeni')];
      rt.bus.add(
        NotificationArrived(
          AppNotification(
            id: 'n',
            kind: NotificationKind.supportReply,
            title: 't',
            body: 'b',
            createdAt: DateTime.now(),
            relatedEntityId: tid,
          ),
        ),
      );
      await Future<void>.delayed(const Duration(milliseconds: 50));
      expect(c.read(supportListProvider).requireValue.items, hasLength(2));
    });

    test('unrelated notifications do not reload it', () async {
      final repo = FakeSupport()..tickets = [ticket()];
      final rt = FakeRealtime();
      final c = make(repo, rt);
      addTearDown(c.dispose);
      c.listen(supportListProvider, (_, _) {});
      await c.read(supportListProvider.future);

      repo.tickets = [];
      rt.bus.add(
        NotificationArrived(
          AppNotification(
            id: 'n',
            kind: NotificationKind.listingApproved,
            title: 't',
            body: 'b',
            createdAt: DateTime.now(),
          ),
        ),
      );
      await Future<void>.delayed(const Duration(milliseconds: 50));
      expect(c.read(supportListProvider).requireValue.items, hasLength(1));
    });
  });

  testWidgets('list shows requests and opens one', (t) async {
    tall(t);
    final repo = FakeSupport()..tickets = [ticket()];
    final c = make(repo, FakeRealtime());
    addTearDown(c.dispose);
    await t.pumpWidget(app(c, const SupportPage()));
    await t.pumpAndSettle();

    expect(find.text('Ödəniş keçmədi'), findsOneWidget);
    expect(find.text(az.supportStOpen), findsOneWidget);
    await t.tap(find.text('Ödəniş keçmədi'));
    await t.pumpAndSettle();
    expect(find.text('TICKET $tid'), findsOneWidget);
  });

  testWidgets('empty list invites the user to write', (t) async {
    tall(t);
    final c = make(FakeSupport(), FakeRealtime());
    addTearDown(c.dispose);
    await t.pumpWidget(app(c, const SupportPage()));
    await t.pumpAndSettle();

    expect(find.text(az.supportEmpty), findsOneWidget);
  });

  testWidgets(
    'new request: validates, then sends with category and reference',
    (t) async {
      tall(t);
      final repo = FakeSupport();
      final c = make(repo, FakeRealtime());
      addTearDown(c.dispose);
      await t.pumpWidget(
        app(
          c,
          const NewTicketPage(
            category: SupportCategory.listing,
            subject: 'Elan: BMW',
            reference: 'Elan: BMW (ID: 7)',
          ),
        ),
      );
      await t.pumpAndSettle();

      // Pre-filled subject and reference are visible.
      expect(find.text('Elan: BMW (ID: 7)'), findsOneWidget);

      // Message missing: error, nothing sent.
      await t.tap(find.text(az.supportSend));
      await t.pump();
      expect(find.text(az.supportErrBody), findsOneWidget);
      expect(repo.created, isEmpty);

      await t.enterText(find.byType(TextField).last, 'Qiymət səhvdir');
      await t.pump();
      await t.tap(find.text(az.supportSend));
      await t.pumpAndSettle();

      expect(repo.created, hasLength(1));
      expect(repo.created.single.category, SupportCategory.listing);
      expect(repo.created.single.subject, 'Elan: BMW');
      expect(
        repo.created.single.message,
        'Qiymət səhvdir\n\n— Elan: BMW (ID: 7)',
      );
      // Continues in the new conversation.
      expect(find.text('TICKET $tid'), findsOneWidget);
    },
  );

  testWidgets('a failed send keeps the form and tells the user', (t) async {
    tall(t);
    final repo = FakeSupport()..failCreate = true;
    final c = make(repo, FakeRealtime());
    addTearDown(c.dispose);
    await t.pumpWidget(app(c, const NewTicketPage()));
    await t.pumpAndSettle();

    await t.enterText(find.byType(TextField).first, 'Başlıq');
    await t.enterText(find.byType(TextField).last, 'Mətn');
    await t.pump();
    await t.tap(find.text(az.supportSend));
    await t.pumpAndSettle();

    expect(find.text(az.errorGeneric), findsOneWidget);
    expect(find.text('Mətn'), findsOneWidget);
    expect(find.text('TICKET $tid'), findsNothing);
  });

  testWidgets('conversation: shows both sides and sends a reply', (t) async {
    tall(t);
    final repo = FakeSupport()
      ..current = ticket(
        messages: [
          msg('1', 'Ödəniş keçmədi', minAgo: 10),
          msg(
            '2',
            'Baxırıq, zəhmət olmasa tarixi yazın',
            admin: true,
            minAgo: 5,
          ),
        ],
      );
    final c = make(repo, FakeRealtime());
    addTearDown(c.dispose);
    await t.pumpWidget(app(c, TicketPage(ticketId: tid)));
    await t.pumpAndSettle();

    expect(find.text(az.supportTeam), findsOneWidget);
    expect(find.text('Baxırıq, zəhmət olmasa tarixi yazın'), findsOneWidget);

    await t.enterText(find.byType(TextField).last, 'Dünən saat 15:00');
    await t.pump();
    await t.tap(find.bySemanticsLabel(az.sendAction));
    await t.pumpAndSettle();

    expect(repo.replies, ['Dünən saat 15:00']);
    expect(find.text('Dünən saat 15:00'), findsOneWidget);
  });

  testWidgets('a closed request cannot be answered', (t) async {
    tall(t);
    final repo = FakeSupport()
      ..current = ticket(
        status: SupportStatus.closed,
        messages: [msg('1', 'Salam')],
      );
    final c = make(repo, FakeRealtime());
    addTearDown(c.dispose);
    await t.pumpWidget(app(c, TicketPage(ticketId: tid)));
    await t.pumpAndSettle();

    expect(find.text(az.supportClosedInfo), findsOneWidget);
    expect(find.text(az.supportReplyHint), findsNothing);
  });

  testWidgets('a resolved request says writing reopens it', (t) async {
    tall(t);
    final repo = FakeSupport()
      ..current = ticket(
        status: SupportStatus.resolved,
        messages: [msg('1', 'Salam')],
      );
    final c = make(repo, FakeRealtime());
    addTearDown(c.dispose);
    await t.pumpWidget(app(c, TicketPage(ticketId: tid)));
    await t.pumpAndSettle();

    expect(find.text(az.supportResolvedInfo), findsOneWidget);
  });

  testWidgets('a failed reply keeps the text', (t) async {
    tall(t);
    final repo = FakeSupport()
      ..failReply = true
      ..current = ticket(messages: [msg('1', 'Salam')]);
    final c = make(repo, FakeRealtime());
    addTearDown(c.dispose);
    await t.pumpWidget(app(c, TicketPage(ticketId: tid)));
    await t.pumpAndSettle();

    await t.enterText(find.byType(TextField).last, 'Yenə yazıram');
    await t.pump();
    await t.tap(find.bySemanticsLabel(az.sendAction));
    await t.pumpAndSettle();

    expect(find.text('Yenə yazıram'), findsOneWidget);
    expect(find.text(az.errorGeneric), findsOneWidget);
  });
}
