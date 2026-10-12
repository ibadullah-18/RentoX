import 'package:dio/dio.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:go_router/go_router.dart';
import 'package:shared_preferences/shared_preferences.dart';
import 'package:rentox/core/l10n/app_localizations.dart';
import 'package:rentox/features/auth/presentation/auth_controller.dart';
import 'package:rentox/features/auth/domain/auth_models.dart';
import 'package:rentox/features/catalog/data/catalog_repository.dart';
import 'package:rentox/features/catalog/domain/catalog_models.dart';
import 'package:rentox/features/listing_details/presentation/listing_details_page.dart';
import 'package:rentox/shared/utils/share_gateway.dart';

class _Guest extends AuthController {
  @override
  Future<AuthSession?> build() async => null;
}

class _Share implements ShareGateway {
  final shared = <String>[];
  final opened = <Uri>[];
  final copied = <String>[];
  bool works = true;
  bool canOpen = true;

  @override
  Future<bool> shareText(String text, {String? subject, Rect? origin}) async {
    shared.add(text);
    return works;
  }

  @override
  Future<bool> open(Uri uri) async {
    opened.add(uri);
    return canOpen;
  }

  @override
  Future<void> copy(String text) async => copied.add(text);
}

class _Catalog extends CatalogRepository {
  _Catalog({this.description = 'Mərkəzdə təmirli mənzil, hər şey var.'})
    : super(Dio());

  final String description;

  @override
  Future<ListingDetails> details(String id, String language) async =>
      ListingDetails.fromJson({
        'id': id,
        'categoryId': 'cat',
        'categoryName': 'Mənzillər',
        'title': 'Nizami küçəsində mənzil',
        'description': description,
        'price': 900,
        'currency': 'AZN',
        'rentalPeriodUnit': 4,
        'isVip': false,
        'isFavorite': false,
        'viewCount': 3,
        'favoriteCount': 0,
        'publishedAtUtc': DateTime.now().toUtc().toIso8601String(),
        'owner': {
          'id': 'o1',
          'fullName': 'Aysel Məmmədova',
          'phoneNumber': '+994',
        },
        'images': <Map<String, dynamic>>[],
        'fields': [
          {
            'fieldId': 'f1',
            'label': 'Təmir vəziyyəti',
            'type': 5,
            'selections': [
              {'optionId': 'o', 'label': 'Təmirli'},
            ],
          },
          {'fieldId': 'f2', 'label': 'Otaq sayı', 'type': 2, 'numericValue': 3},
          {'fieldId': 'f3', 'label': 'Əşyalıdır', 'type': 4, 'flagValue': true},
          {'fieldId': 'f4', 'label': 'Hovuz', 'type': 4, 'flagValue': false},
        ],
      });

  @override
  Future<List<ListingSummary>> similar(String id, String language) async => [
    ListingSummary(
      id: 's1',
      title: 'Oxşar mənzil',
      price: 800,
      currency: 'AZN',
      unit: RentalPeriodUnit.month,
      isVip: false,
      isFavorite: false,
      viewCount: 0,
      favoriteCount: 0,
    ),
  ];
}

Future<_Share> _pump(WidgetTester tester, {String? description}) async {
  tester.view.physicalSize = const Size(1080, 4200);
  tester.view.devicePixelRatio = 3;
  addTearDown(tester.view.reset);
  SharedPreferences.setMockInitialValues({});
  final share = _Share();

  await tester.pumpWidget(
    ProviderScope(
      overrides: [
        catalogRepositoryProvider.overrideWithValue(
          description == null ? _Catalog() : _Catalog(description: description),
        ),
        authControllerProvider.overrideWith(_Guest.new),
        shareGatewayProvider.overrideWithValue(share),
      ],
      child: MaterialApp.router(
        locale: const Locale('az'),
        localizationsDelegates: AppL10n.localizationsDelegates,
        supportedLocales: AppL10n.supportedLocales,
        routerConfig: GoRouter(
          routes: [
            GoRoute(
              path: '/',
              builder: (_, _) => const ListingDetailsPage(listingId: 'l1'),
            ),
          ],
        ),
      ),
    ),
  );
  await tester.pumpAndSettle();
  return share;
}

void main() {
  final az = lookupAppL10n(const Locale('az'));

  testWidgets('information is a table: label on the left, value on the right', (
    tester,
  ) async {
    await _pump(tester);

    Offset at(String text) => tester.getTopLeft(find.text(text));

    for (final (label, value) in [
      ('Təmir vəziyyəti', 'Təmirli'),
      ('Otaq sayı', '3'),
      ('Əşyalıdır', az.yes),
      ('Hovuz', az.no),
    ]) {
      expect(find.text(label), findsOneWidget);
      // Same row (same height), value to the right of its label.
      expect((at(label).dy - at(value).dy).abs(), lessThan(4));
      expect(at(value).dx, greaterThan(at(label).dx + 40));
    }
    expect(tester.takeException(), isNull);
  });

  testWidgets('a long description is cut with "read more"', (tester) async {
    await _pump(tester, description: List.filled(60, 'uzun mətn').join(' '));

    expect(find.text(az.readMore), findsOneWidget);
    expect(find.text(az.readLess), findsNothing);

    await tester.tap(find.text(az.readMore));
    await tester.pumpAndSettle();
    expect(find.text(az.readLess), findsOneWidget);
    expect(find.text(az.readMore), findsNothing);
  });

  testWidgets('a short description has no "read more"', (tester) async {
    await _pump(tester);
    expect(find.text(az.readMore), findsNothing);
  });

  group('share', () {
    Future<_Share> openSheet(WidgetTester tester) async {
      final share = await _pump(tester);
      await tester.tap(find.bySemanticsLabel(az.shareAction).first);
      await tester.pumpAndSettle();
      return share;
    }

    testWidgets('our own sheet offers the usual apps, not the phone default', (
      tester,
    ) async {
      await openSheet(tester);

      expect(find.text(az.shareTitle), findsOneWidget);
      for (final label in ['WhatsApp', 'Telegram', 'SMS', 'Facebook']) {
        expect(find.text(label), findsOneWidget);
      }
      expect(find.text(az.shareEmail), findsOneWidget);
      expect(find.text(az.shareCopyLink), findsOneWidget);
      expect(find.text(az.shareMore), findsOneWidget);
    });

    testWidgets('WhatsApp opens a message with the title, price and link', (
      tester,
    ) async {
      final share = await openSheet(tester);

      await tester.tap(find.text('WhatsApp'));
      await tester.pumpAndSettle();

      final uri = share.opened.single;
      expect(uri.host, 'wa.me');
      final text = uri.queryParameters['text']!;
      expect(text, contains('Nizami küçəsində mənzil'));
      expect(text, contains('AZN'));
      expect(text, endsWith('/listing/l1'));
      expect(find.text(az.shareTitle), findsNothing); // sheet closed
    });

    testWidgets('Telegram and mail get their own kind of link', (tester) async {
      final share = await openSheet(tester);
      await tester.tap(find.text('Telegram'));
      await tester.pumpAndSettle();
      expect(share.opened.last.host, 't.me');
      expect(share.opened.last.queryParameters['url'], endsWith('/listing/l1'));

      await tester.tap(find.bySemanticsLabel(az.shareAction).first);
      await tester.pumpAndSettle();
      await tester.tap(find.text(az.shareEmail));
      await tester.pumpAndSettle();
      expect(share.opened.last.scheme, 'mailto');
    });

    testWidgets('an app that cannot be opened is explained', (tester) async {
      final share = await openSheet(tester);
      share.canOpen = false;

      await tester.tap(find.text('WhatsApp'));
      await tester.pumpAndSettle();

      expect(find.text(az.openLinkFailed), findsOneWidget);
    });

    testWidgets('copy puts just the link on the clipboard and says so', (
      tester,
    ) async {
      final share = await openSheet(tester);

      await tester.tap(find.text(az.shareCopyLink));
      await tester.pumpAndSettle();

      expect(share.copied.single, endsWith('/listing/l1'));
      expect(share.copied.single, isNot(contains('\n')));
      expect(find.text(az.linkCopied), findsOneWidget);
    });

    testWidgets('"more" opens the phone sheet; no sheet means copied', (
      tester,
    ) async {
      final share = await openSheet(tester);

      await tester.tap(find.text(az.shareMore));
      await tester.pumpAndSettle();
      expect(share.shared.single, contains('Nizami küçəsində mənzil'));
      expect(find.text(az.linkCopied), findsNothing);

      share.works = false;
      await tester.tap(find.bySemanticsLabel(az.shareAction).first);
      await tester.pumpAndSettle();
      await tester.tap(find.text(az.shareMore));
      await tester.pumpAndSettle();
      expect(find.text(az.linkCopied), findsOneWidget);
    });
  });

  testWidgets(
    'order: title, price, information, description, owner, report, similar',
    (tester) async {
      await _pump(tester);

      double y(Finder f) {
        expect(f, findsOneWidget);
        return tester.getTopLeft(f).dy;
      }

      final title = y(find.text('Nizami küçəsində mənzil'));
      final facts = y(find.text('Təmir vəziyyəti'));
      final description = y(find.text('Mərkəzdə təmirli mənzil, hər şey var.'));
      final owner = y(find.text('Aysel Məmmədova'));
      final report = y(find.text(az.reportListingHint));
      final similar = y(find.text(az.similarListings));

      expect(title, lessThan(facts));
      expect(facts, lessThan(description));
      expect(description, lessThan(owner));
      expect(owner, lessThan(report));
      expect(report, lessThan(similar));

      // The old big specifications card is gone.
      expect(find.text(az.specsTitle), findsNothing);
      expect(tester.takeException(), isNull); // no overflow at 360 dp
    },
  );
}
