import 'package:dio/dio.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:go_router/go_router.dart';
import 'package:rentox/core/l10n/app_localizations.dart';
import 'package:rentox/features/auth/domain/auth_models.dart';
import 'package:rentox/features/auth/presentation/auth_controller.dart';
import 'package:rentox/features/catalog/domain/catalog_models.dart';
import 'package:rentox/features/notifications/presentation/notifications_controller.dart';
import 'package:rentox/features/profile/data/account_repository.dart';
import 'package:rentox/features/profile/domain/account_models.dart';
import 'package:rentox/features/profile/presentation/profile_page.dart';
import 'package:rentox/features/store/domain/store_models.dart';
import 'package:rentox/features/store/presentation/store_controller.dart';
import 'package:rentox/features/wallet/data/wallet_repository.dart';
import 'package:rentox/features/wallet/domain/wallet_models.dart';
import 'package:rentox/features/wallet/presentation/payment_sheet.dart';
import 'package:rentox/features/wallet/presentation/wallet_page.dart';

class _SignedIn extends AuthController {
  @override
  Future<AuthSession?> build() async =>
      const AuthSession(userId: 'u1', phoneNumber: '+994501234567');
}

class _Store extends MyStoreController {
  _Store(this.store);
  final MyStore? store;

  @override
  Future<MyStore?> build() async => store;
}

class _NoUnread extends UnreadNotificationsController {
  @override
  Future<int> build() async => 0;
}

class _Accounts extends AccountRepository {
  _Accounts() : super(Dio());

  @override
  Future<Account> me() async => const Account(
    userId: 'u1',
    phoneNumber: '+994501234567',
    fullName: 'Murad Əliyev',
    bio: '',
    language: PreferredLanguage.azerbaijani,
  );
}

class _Wallet extends WalletRepository {
  _Wallet(this.total) : super(Dio());

  final int total;
  final pages = <int>[];
  double balanceValue = 37.5;

  @override
  Future<WalletBalance> balance() async =>
      WalletBalance(balance: balanceValue, currency: 'AZN');

  @override
  Future<Paged<WalletTransaction>> transactions({
    int page = 1,
    int pageSize = 20,
  }) async {
    pages.add(page);
    final all = [
      for (var i = 0; i < total; i++)
        WalletTransaction(
          id: 't$i',
          isCredit: i.isEven,
          type: i.isEven ? WalletTxType.topUp : WalletTxType.promotionFee,
          amount: 10 + i.toDouble(),
          balanceAfter: 100,
          occurredAt: DateTime.now().subtract(Duration(minutes: i)),
        ),
    ];
    final from = (page - 1) * pageSize;
    final items = all.skip(from).take(pageSize).toList();
    return Paged(
      items: items,
      page: page,
      totalPages: total == 0 ? 1 : (total / pageSize).ceil(),
      totalCount: total,
    );
  }

  @override
  Future<WalletBalance> topUp(
    double amount, {
    required String idempotencyKey,
  }) async {
    balanceValue += amount;
    return WalletBalance(balance: balanceValue, currency: 'AZN');
  }
}

void main() {
  final az = lookupAppL10n(const Locale('az'));
  String money(double v) => formatMoney('az', v);

  Future<void> pumpApp(
    WidgetTester tester, {
    required Widget home,
    required _Wallet wallet,
    MyStore? store,
  }) async {
    tester.view.physicalSize = const Size(1080, 2600);
    tester.view.devicePixelRatio = 3;
    addTearDown(tester.view.reset);
    final router = GoRouter(
      routes: [
        GoRoute(path: '/', builder: (_, _) => home),
        GoRoute(path: '/wallet', builder: (_, _) => const WalletPage()),
        GoRoute(
          path: '/store/mine',
          builder: (_, _) => const Scaffold(body: Text('MY STORE PAGE')),
        ),
        GoRoute(
          path: '/profile/edit',
          builder: (_, _) => const Scaffold(body: Text('EDIT PROFILE')),
        ),
      ],
    );
    await tester.pumpWidget(
      ProviderScope(
        overrides: [
          authControllerProvider.overrideWith(_SignedIn.new),
          accountRepositoryProvider.overrideWithValue(_Accounts()),
          walletRepositoryProvider.overrideWithValue(wallet),
          myStoreProvider.overrideWith(() => _Store(store)),
          unreadNotificationsProvider.overrideWith(_NoUnread.new),
        ],
        child: MaterialApp.router(
          locale: const Locale('az'),
          localizationsDelegates: AppL10n.localizationsDelegates,
          supportedLocales: AppL10n.supportedLocales,
          routerConfig: router,
        ),
      ),
    );
    await tester.pumpAndSettle();
  }

  MyStore store() => const MyStore(
    id: 's1',
    name: 'Rent Baku',
    slug: 'rent-baku',
    description: 'Maşın və texnika kirayəsi',
    phone: '+994551112233',
    address: 'Bakı, Nizami küç. 5',
    status: StoreStatus.active,
  );

  group('profile', () {
    testWidgets('without a store: personal card, then the wallet', (
      tester,
    ) async {
      await pumpApp(tester, home: const ProfilePage(), wallet: _Wallet(0));
      expect(find.text('Murad Əliyev'), findsOneWidget);
      expect(find.text(az.walletTitle), findsOneWidget);
      expect(find.text(money(37.5)), findsOneWidget);
      expect(find.text(az.walletTopUpAndHistory), findsOneWidget);
      expect(find.text(az.storeMine), findsOneWidget); // menu entry stays
    });

    testWidgets('with a store: the store takes the place of the profile', (
      tester,
    ) async {
      await pumpApp(
        tester,
        home: const ProfilePage(),
        wallet: _Wallet(0),
        store: store(),
      );
      expect(find.text('Rent Baku'), findsOneWidget);
      expect(find.text('Maşın və texnika kirayəsi'), findsOneWidget);
      expect(find.text('+994551112233'), findsOneWidget);
      expect(find.text('Bakı, Nizami küç. 5'), findsOneWidget);
      // The personal account is one slim line; the menu entry is gone.
      expect(find.text('Murad Əliyev'), findsOneWidget);
      expect(find.text(az.storeMine), findsNothing);

      // The wallet sits right under the store and the account line.
      final storeY = tester.getTopLeft(find.text('Rent Baku')).dy;
      final walletY = tester.getTopLeft(find.text(az.walletTitle)).dy;
      final menuY = tester.getTopLeft(find.text(az.myListingsTitle)).dy;
      expect(storeY, lessThan(walletY));
      expect(walletY, lessThan(menuY));

      await tester.tap(find.text('Rent Baku'));
      await tester.pumpAndSettle();
      expect(find.text('MY STORE PAGE'), findsOneWidget);
    });

    testWidgets('the wallet card opens the wallet page', (tester) async {
      await pumpApp(tester, home: const ProfilePage(), wallet: _Wallet(0));
      await tester.tap(find.text(az.walletTopUpAndHistory));
      await tester.pumpAndSettle();
      expect(find.text(az.walletHistoryTitle), findsOneWidget);
      expect(find.text(az.walletHistoryEmpty), findsOneWidget);
    });
  });

  group('wallet page', () {
    testWidgets('shows balance, signs, and loads more pages', (tester) async {
      final wallet = _Wallet(25);
      await pumpApp(tester, home: const WalletPage(), wallet: wallet);

      expect(find.text(money(37.5)), findsOneWidget);
      expect(find.text(az.chatToday), findsOneWidget);
      expect(find.text('+${money(10)}'), findsOneWidget); // credit
      expect(find.text('−${money(11)}'), findsOneWidget); // debit
      expect(find.text(az.txTopUp), findsWidgets);
      expect(wallet.pages, [1]);

      await tester.scrollUntilVisible(
        find.text(az.loadMore),
        400,
        scrollable: find.byType(Scrollable).first,
      );
      await tester.tap(find.text(az.loadMore));
      await tester.pumpAndSettle();
      expect(wallet.pages, [1, 2]);
      await tester.scrollUntilVisible(
        find.text('+${money(30)}'),
        400,
        scrollable: find.byType(Scrollable).first,
      );
      expect(find.text(az.loadMore), findsNothing); // all 25 are there
    });

    testWidgets('a top-up raises the balance and reloads the history', (
      tester,
    ) async {
      final wallet = _Wallet(2);
      await pumpApp(tester, home: const WalletPage(), wallet: wallet);

      await tester.tap(find.text(az.topUpDemo));
      await tester.pumpAndSettle();
      expect(find.text(money(47.5)), findsOneWidget);
      expect(wallet.pages.length, 2); // history asked again
    });
  });
}
