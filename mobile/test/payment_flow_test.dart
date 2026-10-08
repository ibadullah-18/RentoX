import 'package:dio/dio.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';

import 'support/create_fakes.dart' show FakeCatalogBase;

import 'package:rentox/core/l10n/app_localizations.dart';
import 'package:rentox/features/auth/domain/auth_models.dart';
import 'package:rentox/features/auth/presentation/auth_controller.dart';
import 'package:rentox/features/catalog/data/catalog_repository.dart';
import 'package:rentox/features/catalog/domain/catalog_models.dart';
import 'package:rentox/features/my_listings/data/my_listings_repository.dart';
import 'package:rentox/features/my_listings/domain/owned_listing_models.dart';
import 'package:rentox/features/my_listings/presentation/my_listing_details_page.dart';
import 'package:rentox/features/wallet/data/wallet_repository.dart';
import 'package:rentox/features/wallet/domain/wallet_models.dart';
import 'package:rentox/features/wallet/presentation/payment_sheet.dart';

class _SignedIn extends AuthController {
  @override
  Future<AuthSession?> build() async =>
      const AuthSession(userId: 'u1', phoneNumber: '+994501234567');
}

class _FakeWallet extends WalletRepository {
  _FakeWallet(this.balanceValue) : super(Dio());

  double balanceValue;
  final topUpKeys = <String>[];

  @override
  Future<WalletBalance> balance() async =>
      WalletBalance(balance: balanceValue, currency: 'AZN');

  @override
  Future<WalletBalance> topUp(
    double amount, {
    required String idempotencyKey,
  }) async {
    topUpKeys.add(idempotencyKey);
    balanceValue += amount;
    return WalletBalance(balance: balanceValue, currency: 'AZN');
  }
}

class _FakeListings extends MyListingsRepository {
  _FakeListings(this.status) : super(Dio());

  ListingStatus status;
  final promoteKeys = <String>[];
  int payCalls = 0;
  int failPromoteTimes = 0;

  @override
  Future<OwnedListingDetails> details(String id, String language) async =>
      OwnedListingDetails(
        id: id,
        categoryId: 'c',
        title: 'Toyota Camry 2023',
        description: 'Clean car',
        price: 85,
        currency: 'AZN',
        unit: RentalPeriodUnit.day,
        status: status,
        images: const [],
        fields: const [],
      );

  @override
  Future<PaymentResult> payAndActivate(String id) async {
    payCalls++;
    status = ListingStatus.active;
    return const PaymentResult(
      chargedAmount: 1,
      remainingBalance: 0,
      wasAlreadyProcessed: false,
    );
  }

  @override
  Future<PaymentResult> promote(
    String id, {
    required PromotionType type,
    required String idempotencyKey,
  }) async {
    promoteKeys.add(idempotencyKey);
    if (failPromoteTimes > 0) {
      failPromoteTimes--;
      throw Exception('timeout');
    }
    return const PaymentResult(
      chargedAmount: 10,
      remainingBalance: 5,
      wasAlreadyProcessed: false,
    );
  }
}

class _VipCatalog extends FakeCatalogBase {
  _VipCatalog(this.vip);

  final bool vip;

  @override
  Future<ListingDetails> details(String id, String language) async =>
      ListingDetails.fromJson({
        'id': id,
        'title': 'Toyota Camry 2023',
        'description': 'x',
        'price': 85,
        'currency': 'AZN',
        'rentalPeriodUnit': 2,
        'isVip': vip,
        'owner': {'id': 'u1', 'fullName': 'Me'},
        'images': <Object>[],
        'fields': <Object>[],
      });
}

Widget _app(
  Widget home, {
  required _FakeWallet wallet,
  _FakeListings? listings,
  bool vip = false,
}) => ProviderScope(
  overrides: [
    authControllerProvider.overrideWith(_SignedIn.new),
    walletRepositoryProvider.overrideWithValue(wallet),
    catalogRepositoryProvider.overrideWithValue(_VipCatalog(vip)),
    if (listings != null)
      myListingsRepositoryProvider.overrideWithValue(listings),
  ],
  child: MaterialApp(
    locale: const Locale('az'),
    localizationsDelegates: AppL10n.localizationsDelegates,
    supportedLocales: AppL10n.supportedLocales,
    home: home,
  ),
);

void main() {
  final az = lookupAppL10n(const Locale('az'));

  setUp(() {});

  testWidgets('VIP payment: top up when short, retry reuses the same key', (
    tester,
  ) async {
    tester.view.physicalSize = const Size(1080, 2400);
    tester.view.devicePixelRatio = 3;
    addTearDown(tester.view.reset);

    final wallet = _FakeWallet(5); // VIP costs 10
    final keys = <String>[];
    var failFirst = true;
    bool? result;

    await tester.pumpWidget(
      _app(
        Builder(
          builder: (context) => Scaffold(
            body: Center(
              child: ElevatedButton(
                onPressed: () async {
                  result = await showPaymentSheet(
                    context,
                    kind: PaymentKind.vip,
                    pay: (key) async {
                      keys.add(key);
                      if (failFirst) {
                        failFirst = false;
                        throw Exception('network');
                      }
                      return const PaymentResult(
                        chargedAmount: 10,
                        remainingBalance: 5,
                        wasAlreadyProcessed: false,
                      );
                    },
                  );
                },
                child: const Text('open'),
              ),
            ),
          ),
        ),
        wallet: wallet,
      ),
    );

    await tester.tap(find.text('open'));
    await tester.pumpAndSettle();

    // Not enough money: warning shown and the pay button is disabled.
    expect(find.text(az.insufficientBalance), findsOneWidget);
    FilledButton payButton() =>
        tester.widget<FilledButton>(find.byType(FilledButton));
    expect(payButton().onPressed, isNull);

    // Demo top-up of 10 -> balance 15 -> enough.
    await tester.tap(find.text(az.topUpDemo));
    await tester.pumpAndSettle();
    expect(wallet.balanceValue, 15);
    expect(find.text(az.insufficientBalance), findsNothing);
    expect(payButton().onPressed, isNotNull);

    // First attempt fails, second succeeds - both with the SAME key.
    await tester.tap(find.byType(FilledButton));
    await tester.pumpAndSettle();
    expect(find.text(az.paymentFailed), findsOneWidget);
    expect(result, isNull); // sheet still open

    await tester.tap(find.byType(FilledButton));
    await tester.pumpAndSettle();

    expect(keys, hasLength(2));
    expect(keys[0], keys[1]);
    expect(keys[0], startsWith('rx-'));
    expect(result, isTrue);
    expect(tester.takeException(), isNull);
  });

  testWidgets('owner page: pay & activate a payment-required listing', (
    tester,
  ) async {
    tester.view.physicalSize = const Size(1080, 2400);
    tester.view.devicePixelRatio = 3;
    addTearDown(tester.view.reset);

    final wallet = _FakeWallet(20);
    final listings = _FakeListings(ListingStatus.paymentRequired);

    await tester.pumpWidget(
      _app(
        const MyListingDetailsPage(listingId: 'L1'),
        wallet: wallet,
        listings: listings,
      ),
    );
    await tester.pumpAndSettle();

    expect(find.text('Toyota Camry 2023'), findsOneWidget);
    expect(find.text(az.statusPaymentRequired), findsOneWidget);
    expect(find.textContaining(az.makeVip), findsNothing); // not live yet

    await tester.tap(find.textContaining(az.payActivate));
    await tester.pumpAndSettle();
    await tester.tap(
      find.descendant(
        of: find.byType(BottomSheet),
        matching: find.byType(FilledButton),
      ),
    );
    await tester.pumpAndSettle();

    expect(listings.payCalls, 1);
    // The page re-reads the listing: now active, VIP can be offered.
    expect(find.text(az.statusActive), findsOneWidget);
    expect(find.textContaining(az.makeVip), findsOneWidget);
    expect(find.text(az.paymentSuccessActivation), findsOneWidget);
    expect(tester.takeException(), isNull);
  });

  testWidgets('owner page: VIP purchase uses an idempotency key', (
    tester,
  ) async {
    tester.view.physicalSize = const Size(1080, 2400);
    tester.view.devicePixelRatio = 3;
    addTearDown(tester.view.reset);

    final wallet = _FakeWallet(50);
    final listings = _FakeListings(ListingStatus.active);

    await tester.pumpWidget(
      _app(
        const MyListingDetailsPage(listingId: 'L1'),
        wallet: wallet,
        listings: listings,
      ),
    );
    await tester.pumpAndSettle();

    await tester.tap(find.textContaining(az.makeVip));
    await tester.pumpAndSettle();
    await tester.tap(
      find.descendant(
        of: find.byType(BottomSheet),
        matching: find.byType(FilledButton),
      ),
    );
    await tester.pumpAndSettle();

    expect(listings.promoteKeys, hasLength(1));
    expect(listings.promoteKeys.single, startsWith('rx-'));
    expect(find.text(az.paymentSuccessVip), findsOneWidget);
    expect(tester.takeException(), isNull);
  });

  testWidgets('owner page: an already-VIP listing cannot be bought twice', (
    tester,
  ) async {
    tester.view.physicalSize = const Size(1080, 2400);
    tester.view.devicePixelRatio = 3;
    addTearDown(tester.view.reset);

    final listings = _FakeListings(ListingStatus.active);
    await tester.pumpWidget(
      _app(
        const MyListingDetailsPage(listingId: 'L1'),
        wallet: _FakeWallet(50),
        listings: listings,
        vip: true,
      ),
    );
    await tester.pumpAndSettle();

    expect(find.text(az.vipActive), findsOneWidget);
    expect(find.textContaining(az.makeVip), findsNothing);
    expect(find.text(az.deactivateAction), findsOneWidget);
  });
}
