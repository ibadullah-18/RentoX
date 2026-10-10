import 'dart:convert';
import 'dart:typed_data';

import 'package:dio/dio.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_riverpod/misc.dart' show Override;
import 'package:flutter_test/flutter_test.dart';
import 'package:go_router/go_router.dart';
import 'package:rentox/core/l10n/app_localizations.dart';
import 'package:rentox/core/network/api_exception.dart';
import 'package:rentox/features/auth/domain/auth_models.dart';
import 'package:rentox/features/auth/presentation/auth_controller.dart';
import 'package:rentox/features/profile/data/account_repository.dart';
import 'package:rentox/features/profile/presentation/delete_account_dialog.dart';
import 'package:rentox/features/report/data/report_repository.dart';
import 'package:rentox/features/report/domain/report_models.dart';
import 'package:rentox/features/report/presentation/report_sheet.dart';

class _SignedIn extends AuthController {
  bool forgotten = false;

  @override
  Future<AuthSession?> build() async =>
      const AuthSession(userId: 'u1', phoneNumber: '+994501234567');

  @override
  Future<void> accountDeleted() async {
    forgotten = true;
    state = const AsyncData(null);
  }
}

class _Guest extends AuthController {
  @override
  Future<AuthSession?> build() async => null;
}

class _Reports extends ReportRepository {
  _Reports() : super(Dio());

  final sent =
      <
        ({ReportTarget target, String id, ReportReason reason, String details})
      >[];
  Object? fail;

  @override
  Future<void> submit({
    required ReportTarget target,
    required String targetId,
    required ReportReason reason,
    String details = '',
  }) async {
    if (fail != null) throw fail!;
    sent.add((
      target: target,
      id: targetId,
      reason: reason,
      details: details.trim(),
    ));
  }
}

class _Accounts extends AccountRepository {
  _Accounts() : super(Dio());

  int deletes = 0;
  bool fail = false;

  @override
  Future<void> deleteAccount() async {
    if (fail) throw const ApiException('boom');
    deletes++;
  }
}

class _Adapter implements HttpClientAdapter {
  RequestOptions? last;

  @override
  Future<ResponseBody> fetch(
    RequestOptions options,
    Stream<Uint8List>? requestStream,
    Future<void>? cancelFuture,
  ) async {
    last = options;
    return ResponseBody.fromString('', 204);
  }

  @override
  void close({bool force = false}) {}
}

Widget host(
  Widget child, {
  required List<Override> overrides,
  GoRouter? router,
}) {
  final app = router == null
      ? MaterialApp(
          locale: const Locale('az'),
          localizationsDelegates: AppL10n.localizationsDelegates,
          supportedLocales: AppL10n.supportedLocales,
          home: Scaffold(body: Center(child: child)),
        )
      : MaterialApp.router(
          routerConfig: router,
          locale: const Locale('az'),
          localizationsDelegates: AppL10n.localizationsDelegates,
          supportedLocales: AppL10n.supportedLocales,
        );
  return ProviderScope(overrides: overrides, child: app);
}

void main() {
  final az = lookupAppL10n(const Locale('az'));

  group('ReportRepository', () {
    test('posts the reason and trimmed details to the right path', () async {
      final adapter = _Adapter();
      final repo = ReportRepository(Dio()..httpClientAdapter = adapter);

      await repo.submit(
        target: ReportTarget.listing,
        targetId: 'l1',
        reason: ReportReason.fraud,
        details: '  scam  ',
      );
      expect(adapter.last!.path, '/api/listings/l1/reports');
      expect(adapter.last!.method, 'POST');
      expect(adapter.last!.data, {'reason': 2, 'details': 'scam'});

      await repo.submit(
        target: ReportTarget.store,
        targetId: 's1',
        reason: ReportReason.spam,
      );
      expect(adapter.last!.path, '/api/stores/s1/reports');
      expect(adapter.last!.data, {'reason': 1, 'details': null});
    });

    test('a store has no "wrong category" reason', () {
      expect(
        ReportReason.forTarget(ReportTarget.store),
        isNot(contains(ReportReason.wrongCategory)),
      );
      expect(
        ReportReason.forTarget(ReportTarget.listing),
        contains(ReportReason.wrongCategory),
      );
    });

    test('recognises the already-reported answer', () {
      expect(
        isAlreadyReported(
          const ApiException(
            'You have already reported this. We are reviewing it.',
            statusCode: 400,
          ),
        ),
        isTrue,
      );
      expect(
        isAlreadyReported(const ApiException('other', statusCode: 400)),
        isFalse,
      );
      expect(isAlreadyReported(Exception('x')), isFalse);
    });
  });

  group('ReportSheet', () {
    Future<_Reports> open(WidgetTester tester, {Object? fail}) async {
      final repo = _Reports()..fail = fail;
      bool? result;
      await tester.pumpWidget(
        host(
          Builder(
            builder: (context) => ElevatedButton(
              onPressed: () async => result = await showModalBottomSheet<bool>(
                context: context,
                isScrollControlled: true,
                builder: (_) => const ReportSheet(
                  target: ReportTarget.listing,
                  targetId: 'l1',
                ),
              ),
              child: Text('open ${result ?? ''}'),
            ),
          ),
          overrides: [reportRepositoryProvider.overrideWithValue(repo)],
        ),
      );
      await tester.tap(find.byType(ElevatedButton));
      await tester.pumpAndSettle();
      return repo;
    }

    testWidgets('sending needs a reason, and "other" needs an explanation', (
      tester,
    ) async {
      final repo = await open(tester);

      FilledButton send() => tester.widget<FilledButton>(
        find.widgetWithText(FilledButton, az.reportSend),
      );
      expect(send().onPressed, isNull);

      await tester.tap(find.text(az.reportReasonOther));
      await tester.pump();
      expect(send().onPressed, isNull); // explanation missing

      await tester.enterText(find.byType(TextField), 'Photos are fake');
      await tester.pump();
      expect(send().onPressed, isNotNull);

      await tester.tap(find.text(az.reportSend));
      await tester.pumpAndSettle();

      expect(repo.sent.single.reason, ReportReason.other);
      expect(repo.sent.single.details, 'Photos are fake');
      expect(repo.sent.single.target, ReportTarget.listing);
      expect(repo.sent.single.id, 'l1');
      expect(find.text(az.reportSend), findsNothing); // sheet closed
    });

    testWidgets('a plain reason can be sent without details', (tester) async {
      final repo = await open(tester);

      await tester.tap(find.text(az.reportReasonSpam));
      await tester.pump();
      await tester.tap(find.text(az.reportSend));
      await tester.pumpAndSettle();

      expect(repo.sent.single.reason, ReportReason.spam);
      expect(repo.sent.single.details, isEmpty);
    });

    testWidgets('an already filed report is explained and keeps the sheet', (
      tester,
    ) async {
      await open(
        tester,
        fail: const ApiException(
          'You have already reported this. We are reviewing it.',
          statusCode: 400,
        ),
      );

      await tester.tap(find.text(az.reportReasonFraud));
      await tester.pump();
      await tester.tap(find.text(az.reportSend));
      await tester.pumpAndSettle();

      expect(find.text(az.reportAlready), findsOneWidget);
      expect(find.text(az.reportSend), findsOneWidget);
    });

    testWidgets('any other failure says it was not sent', (tester) async {
      await open(tester, fail: const ApiException('x', isNetwork: true));

      await tester.tap(find.text(az.reportReasonSpam));
      await tester.pump();
      await tester.tap(find.text(az.reportSend));
      await tester.pumpAndSettle();

      expect(find.text(az.reportFailed), findsOneWidget);
    });
  });

  testWidgets('a guest is sent to sign in instead of reporting', (
    tester,
  ) async {
    final router = GoRouter(
      routes: [
        GoRoute(
          path: '/',
          builder: (context, _) => Consumer(
            builder: (context, ref, _) => Scaffold(
              body: ElevatedButton(
                onPressed: () => startReport(
                  context,
                  ref,
                  target: ReportTarget.store,
                  targetId: 's1',
                ),
                child: const Text('report'),
              ),
            ),
          ),
        ),
        GoRoute(
          path: '/auth/phone',
          builder: (_, state) => Scaffold(
            body: Text('login:${state.uri.queryParameters['from']}'),
          ),
        ),
      ],
    );
    await tester.pumpWidget(
      host(
        const SizedBox(),
        router: router,
        overrides: [
          authControllerProvider.overrideWith(_Guest.new),
          reportRepositoryProvider.overrideWithValue(_Reports()),
        ],
      ),
    );
    await tester.pumpAndSettle();

    await tester.tap(find.text('report'));
    await tester.pumpAndSettle();

    expect(find.textContaining('login:'), findsOneWidget);
    expect(find.text(az.reportSend), findsNothing);
  });

  group('delete account', () {
    Future<_Accounts> open(WidgetTester tester, {bool fail = false}) async {
      final accounts = _Accounts()..fail = fail;
      await tester.pumpWidget(
        host(
          Builder(
            builder: (context) => ElevatedButton(
              onPressed: () => confirmAndDeleteAccount(context),
              child: const Text('delete'),
            ),
          ),
          overrides: [
            accountRepositoryProvider.overrideWithValue(accounts),
            authControllerProvider.overrideWith(_SignedIn.new),
          ],
        ),
      );
      await tester.pump();
      await tester.tap(find.text('delete'));
      await tester.pumpAndSettle();
      return accounts;
    }

    TextButton confirm(WidgetTester tester) => tester.widget<TextButton>(
      find.widgetWithText(TextButton, az.deleteAccountConfirm),
    );

    testWidgets('needs the confirmation word, then deletes and signs out', (
      tester,
    ) async {
      final accounts = await open(tester);

      expect(find.text(az.deleteAccountTitle), findsOneWidget);
      expect(confirm(tester).onPressed, isNull);

      await tester.enterText(find.byType(TextField), 'nope');
      await tester.pump();
      expect(confirm(tester).onPressed, isNull);

      // Case does not matter, spaces around do not either.
      await tester.enterText(
        find.byType(TextField),
        ' ${az.deleteAccountWord.toLowerCase()} ',
      );
      await tester.pump();
      expect(confirm(tester).onPressed, isNotNull);

      await tester.tap(find.text(az.deleteAccountConfirm));
      await tester.pumpAndSettle();

      expect(accounts.deletes, 1);
      expect(find.text(az.deleteAccountTitle), findsNothing);
    });

    testWidgets('cancelling deletes nothing', (tester) async {
      final accounts = await open(tester);

      await tester.tap(find.text(az.cancelAction));
      await tester.pumpAndSettle();

      expect(accounts.deletes, 0);
      expect(find.text(az.deleteAccountTitle), findsNothing);
    });

    testWidgets('a failure keeps the dialog and the session', (tester) async {
      final accounts = await open(tester, fail: true);

      await tester.enterText(find.byType(TextField), az.deleteAccountWord);
      await tester.pump();
      await tester.tap(find.text(az.deleteAccountConfirm));
      await tester.pumpAndSettle();

      expect(accounts.deletes, 0);
      expect(find.text(az.deleteAccountFailed), findsOneWidget);
      expect(find.text(az.deleteAccountTitle), findsOneWidget);
    });
  });

  test('the request really sends the confirmation', () async {
    final adapter = _Adapter();
    final repo = AccountRepository(Dio()..httpClientAdapter = adapter);

    await repo.deleteAccount();

    expect(adapter.last!.method, 'DELETE');
    expect(adapter.last!.path, '/api/account/me');
    expect(jsonEncode(adapter.last!.data), '{"confirm":true}');
  });
}
