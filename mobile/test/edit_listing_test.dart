import 'package:dio/dio.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:go_router/go_router.dart';
import 'package:rentox/core/l10n/app_localizations.dart';
import 'package:rentox/features/auth/domain/auth_models.dart';
import 'package:rentox/features/auth/presentation/auth_controller.dart';
import 'package:rentox/features/catalog/data/catalog_repository.dart';
import 'package:rentox/features/catalog/domain/catalog_models.dart';
import 'package:rentox/features/listing_create/data/listing_create_repository.dart';
import 'package:rentox/features/my_listings/data/my_listings_repository.dart';
import 'package:rentox/features/my_listings/domain/owned_listing_models.dart';
import 'package:rentox/features/my_listings/presentation/edit_listing_page.dart';
import 'package:rentox/features/my_listings/presentation/my_listing_details_page.dart';
import 'package:rentox/features/wallet/data/wallet_repository.dart';
import 'package:rentox/features/wallet/domain/wallet_models.dart';

import 'support/create_fakes.dart';

class _SignedIn extends AuthController {
  @override
  Future<AuthSession?> build() async =>
      const AuthSession(userId: 'u1', phoneNumber: '+994501234567');
}

class _Wallet extends WalletRepository {
  _Wallet() : super(Dio());

  @override
  Future<WalletBalance> balance() async =>
      const WalletBalance(balance: 50, currency: 'AZN');
}

class _Catalog extends FakeCatalogBase {
  @override
  Future<ListingDetails> details(String id, String language) async =>
      ListingDetails.fromJson({
        'id': id,
        'title': 'x',
        'description': 'x',
        'price': 1,
        'currency': 'AZN',
        'rentalPeriodUnit': 2,
        'isVip': false,
        'owner': {'id': 'u1', 'fullName': 'Me'},
        'images': <Object>[],
        'fields': <Object>[],
      });
}

class _Listings extends MyListingsRepository {
  _Listings({
    this.status = ListingStatus.draft,
    this.expiresAt,
    List<ListingFieldValue>? fields,
  }) : fields =
           fields ??
           const [
             ListingFieldValue(
               fieldId: 'f-brand',
               label: 'Marka',
               type: FieldType.text,
               textValue: 'Toyota',
             ),
             ListingFieldValue(
               fieldId: 'f-year',
               label: 'Il',
               type: FieldType.wholeNumber,
               numericValue: 2023,
             ),
           ],
       super(Dio());

  ListingStatus status;
  DateTime? expiresAt;
  List<ListingFieldValue> fields;
  final calls = <String>[];
  Map<String, dynamic>? savedDetails;
  List<Map<String, dynamic>>? savedFields;

  @override
  Future<OwnedListingDetails> details(String id, String language) async =>
      OwnedListingDetails(
        id: id,
        categoryId: 'cat',
        title: 'Toyota Camry',
        description: 'Clean car',
        price: 85,
        currency: 'AZN',
        unit: RentalPeriodUnit.day,
        status: status,
        images: const [],
        fields: fields,
        expiresAt: expiresAt,
        rejectionReason: status == ListingStatus.rejected
            ? 'Photos are blurry'
            : null,
      );

  @override
  Future<void> updateDetails(
    String id, {
    required String title,
    required String description,
    required double price,
    required RentalPeriodUnit unit,
    String currency = 'AZN',
  }) async {
    calls.add('details');
    savedDetails = {
      'title': title,
      'description': description,
      'price': price,
      'unit': unit.id,
    };
  }

  @override
  Future<void> updateFields(String id, List<Map<String, dynamic>> body) async {
    calls.add('fields');
    savedFields = body;
  }

  @override
  Future<void> delete(String id) async => calls.add('delete');

  @override
  Future<PaymentResult> renew(String id) async {
    calls.add('renew');
    status = ListingStatus.active;
    expiresAt = DateTime.now().add(const Duration(days: 30));
    return const PaymentResult(
      chargedAmount: 1,
      remainingBalance: 49,
      wasAlreadyProcessed: false,
    );
  }
}

Widget _app(Widget home, _Listings listings) => ProviderScope(
  overrides: [
    authControllerProvider.overrideWith(_SignedIn.new),
    walletRepositoryProvider.overrideWithValue(_Wallet()),
    catalogRepositoryProvider.overrideWithValue(_Catalog()),
    myListingsRepositoryProvider.overrideWithValue(listings),
    listingCreateRepositoryProvider.overrideWithValue(
      FakeCreateRepo(
        defs: [
          def('f-brand', FieldType.text, required: true),
          def('f-year', FieldType.wholeNumber, required: true),
        ],
      ),
    ),
  ],
  child: MaterialApp.router(
    locale: const Locale('az'),
    localizationsDelegates: AppL10n.localizationsDelegates,
    supportedLocales: AppL10n.supportedLocales,
    routerConfig: GoRouter(
      routes: [
        GoRoute(path: '/', builder: (_, _) => home),
        GoRoute(
          path: '/my-listings',
          builder: (_, _) => const Scaffold(body: Text('LIST PAGE')),
        ),
      ],
    ),
  ),
);

void tall(WidgetTester tester) {
  tester.view.physicalSize = const Size(1080, 3600);
  tester.view.devicePixelRatio = 3;
  addTearDown(tester.view.reset);
}

void main() {
  final az = lookupAppL10n(const Locale('az'));

  group('status rules', () {
    test('only drafts and rejected listings are editable', () {
      for (final s in ListingStatus.values) {
        expect(
          s.canEdit,
          s == ListingStatus.draft || s == ListingStatus.rejected,
          reason: '$s',
        );
      }
    });

    test('everything but deleted can be deleted', () {
      expect(ListingStatus.deleted.canDelete, isFalse);
      expect(ListingStatus.active.canDelete, isTrue);
      expect(ListingStatus.pendingReview.canDelete, isTrue);
    });

    test('a listing past its end date counts as expired', () {
      final now = DateTime(2026, 10, 9);
      expect(
        effectiveStatus(
          ListingStatus.active,
          now.subtract(const Duration(days: 1)),
          now,
        ),
        ListingStatus.expired,
      );
      expect(
        effectiveStatus(
          ListingStatus.active,
          now.add(const Duration(days: 1)),
          now,
        ),
        ListingStatus.active,
      );
      // Drafts never expire, whatever the date says.
      expect(
        effectiveStatus(ListingStatus.draft, DateTime(2000), now),
        ListingStatus.draft,
      );
    });

    test('field values keep their ids so they can be edited', () {
      final v = ListingFieldValue.fromJson({
        'fieldId': 'f1',
        'label': 'Fuel',
        'type': 5,
        'selections': [
          {'optionId': 'o1', 'label': 'Petrol'},
        ],
      });
      expect(v.fieldId, 'f1');
      expect(v.selectionIds, ['o1']);
    });
  });

  testWidgets('editing a draft: prefilled, validated, saved in order', (
    tester,
  ) async {
    tall(tester);
    final listings = _Listings();
    await tester.pumpWidget(
      _app(const EditListingPage(listingId: 'l1'), listings),
    );
    await tester.pumpAndSettle();

    // Everything the server has is shown.
    expect(find.text('Toyota Camry'), findsOneWidget);
    expect(find.text('Clean car'), findsOneWidget);
    expect(find.text('Toyota'), findsOneWidget);
    expect(find.text('2023'), findsOneWidget);
    expect(find.text('85'), findsOneWidget);

    // An empty title blocks saving and nothing is sent.
    await tester.enterText(find.widgetWithText(TextField, 'Toyota Camry'), '');
    await tester.pump();
    await tester.tap(find.text(az.editSave));
    await tester.pump();
    expect(find.text(az.titleRequired), findsOneWidget);
    expect(listings.calls, isEmpty);

    // Fix it and save.
    await tester.enterText(find.byType(TextField).first, 'Toyota Camry 2024');
    await tester.enterText(find.widgetWithText(TextField, '85'), '90,5');
    await tester.pump();
    await tester.tap(find.text(az.editSave));
    await tester.pumpAndSettle();

    expect(listings.calls, ['details', 'fields']);
    expect(listings.savedDetails!['title'], 'Toyota Camry 2024');
    expect(listings.savedDetails!['price'], 90.5);
    expect(listings.savedDetails!['unit'], RentalPeriodUnit.day.id);
    expect(listings.savedFields, [
      {'fieldId': 'f-brand', 'textValue': 'Toyota'},
      {'fieldId': 'f-year', 'numericValue': 2023.0},
    ]);
  });

  testWidgets('a required field cleared by the user is caught', (tester) async {
    tall(tester);
    final listings = _Listings();
    await tester.pumpWidget(
      _app(const EditListingPage(listingId: 'l1'), listings),
    );
    await tester.pumpAndSettle();

    await tester.enterText(find.widgetWithText(TextField, 'Toyota'), '');
    await tester.pump();
    await tester.tap(find.text(az.editSave));
    await tester.pump();

    expect(find.text(az.fieldRequired), findsOneWidget);
    expect(listings.calls, isEmpty);
  });

  testWidgets('a live listing cannot be edited', (tester) async {
    tall(tester);
    await tester.pumpWidget(
      _app(
        const EditListingPage(listingId: 'l1'),
        _Listings(status: ListingStatus.active),
      ),
    );
    await tester.pumpAndSettle();

    expect(find.text(az.editNotAllowed), findsOneWidget);
    expect(find.text(az.editSave), findsNothing);
  });

  testWidgets('owner page offers the right actions per status', (tester) async {
    tall(tester);

    // Draft: submit + edit + delete, no promotion.
    await tester.pumpWidget(
      _app(const MyListingDetailsPage(listingId: 'l1'), _Listings()),
    );
    await tester.pumpAndSettle();
    expect(find.text(az.editAction), findsOneWidget);
    expect(find.text(az.submitForReview), findsOneWidget);
    expect(find.text(az.deleteListingAction), findsOneWidget);
    expect(find.text(az.renewAction), findsNothing);

    // Active: promotions, no edit.
    await tester.pumpWidget(
      _app(
        const MyListingDetailsPage(listingId: 'l2'),
        _Listings(
          status: ListingStatus.active,
          expiresAt: DateTime.now().add(const Duration(days: 10)),
        ),
      ),
    );
    await tester.pumpAndSettle();
    expect(find.text(az.editAction), findsNothing);
    expect(find.textContaining(az.bumpAction), findsOneWidget);
    expect(find.text(az.renewAction), findsNothing);

    // Active but past its date: renew only.
    await tester.pumpWidget(
      _app(
        const MyListingDetailsPage(listingId: 'l3'),
        _Listings(
          status: ListingStatus.active,
          expiresAt: DateTime.now().subtract(const Duration(days: 1)),
        ),
      ),
    );
    await tester.pumpAndSettle();
    expect(find.text(az.renewAction), findsOneWidget);
    expect(find.textContaining(az.bumpAction), findsNothing);
    expect(find.text(az.deactivateAction), findsNothing);
  });

  testWidgets('deleting asks first and then deletes', (tester) async {
    tall(tester);
    final listings = _Listings();
    await tester.pumpWidget(
      _app(const MyListingDetailsPage(listingId: 'l1'), listings),
    );
    await tester.pumpAndSettle();

    await tester.tap(find.text(az.deleteListingAction));
    await tester.pumpAndSettle();
    expect(find.text(az.deleteListingTitle), findsOneWidget);

    // Cancel: nothing happens.
    await tester.tap(find.text(az.cancelAction));
    await tester.pumpAndSettle();
    expect(listings.calls, isNot(contains('delete')));
    expect(find.text('LIST PAGE'), findsNothing);

    // Confirm: deleted, and we land on the list of my listings.
    await tester.tap(find.text(az.deleteListingAction));
    await tester.pumpAndSettle();
    await tester.tap(find.text(az.deleteListingConfirm));
    await tester.pumpAndSettle();
    expect(listings.calls, contains('delete'));
    expect(find.text('LIST PAGE'), findsOneWidget);
  });

  testWidgets('renewing an expired listing goes through the payment sheet', (
    tester,
  ) async {
    tall(tester);
    final listings = _Listings(
      status: ListingStatus.active,
      expiresAt: DateTime.now().subtract(const Duration(days: 2)),
    );
    await tester.pumpWidget(
      _app(const MyListingDetailsPage(listingId: 'l1'), listings),
    );
    await tester.pumpAndSettle();

    await tester.tap(find.text(az.renewAction));
    await tester.pumpAndSettle();
    expect(find.text(az.payForRenewal), findsOneWidget);

    await tester.tap(find.textContaining('Ödə:'));
    await tester.pumpAndSettle();
    expect(listings.calls, contains('renew'));
    expect(find.text(az.paymentSuccessRenew), findsOneWidget);
  });
}
