import 'package:dio/dio.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:rentox/core/l10n/app_localizations.dart';
import 'package:rentox/features/catalog/domain/catalog_models.dart';
import 'package:rentox/features/store/data/store_repository.dart';
import 'package:rentox/features/store/domain/store_models.dart';
import 'package:rentox/features/store/presentation/store_page.dart';

/// The weekday after today in Baku (1 = Monday ... 7 = Sunday).
int _tomorrow() =>
    (DateTime.now().toUtc().add(const Duration(hours: 4)).weekday % 7) + 1;

ListingSummary _listing(String id, {required bool vip}) => ListingSummary(
  id: id,
  title: 'Elan $id',
  price: 50,
  currency: 'AZN',
  unit: RentalPeriodUnit.day,
  isVip: vip,
  isFavorite: false,
  viewCount: 0,
  favoriteCount: 0,
);

class _Stores extends StoreRepository {
  _Stores() : super(Dio());

  final events = <String>[];

  @override
  Future<PublicStore> bySlug(String slug) async => PublicStore(
    id: 's9',
    name: 'Nümunə mağaza',
    slug: 'numune',
    description: 'Mağaza təsviri burada yazılıb',
    phone: '+994551112233',
    whatsApp: '994501112233',
    whatsAppMessage: 'Salam! Ünvanı deyin.',
    youtube: 'https://youtube.com/@numune',
    followerCount: 12,
    // Never on today (Baku time): the page shows today's hours at once, and this test is about the closed week.
    hours: [
      StoreHours(day: _tomorrow(), openMinute: 540, closeMinute: 1080),
      StoreHours(day: (_tomorrow() % 7) + 1, openMinute: 600, closeMinute: 840),
    ],
  );

  @override
  Future<Paged<ListingSummary>> listings(
    String slug, {
    required String language,
    int page = 1,
    int pageSize = 20,
    String search = '',
    double? minPrice,
    double? maxPrice,
  }) async => Paged(
    items: [
      for (var i = 0; i < 7; i++) _listing('v$i', vip: true),
      for (var i = 0; i < 9; i++) _listing('r$i', vip: false),
    ],
    page: 1,
    totalPages: 1,
    totalCount: 16,
  );

  @override
  Future<FollowStatus> followStatus(String storeId) async =>
      const FollowStatus(isFollowing: false, followerCount: 12);

  @override
  Future<MyStore?> mine() async => null;

  @override
  Future<void> reportEvent(String slug, String kind) async => events.add(kind);
}

void main() {
  group('store hours', () {
    const hours = [StoreHours(day: 3, openMinute: 540, closeMinute: 1080)];

    test(
      'open inside the hours of the day in Baku time, closed outside them',
      () {
        // Wednesday 14 October 2026: 10:00 in Baku is 06:00 UTC.
        expect(StoreHours.openAt(hours, DateTime.utc(2026, 10, 14, 6)), isTrue);
        // 08:30 in Baku: not yet open.
        expect(
          StoreHours.openAt(hours, DateTime.utc(2026, 10, 14, 4, 30)),
          isFalse,
        );
        // 18:00 in Baku (14:00 UTC): closing time itself is closed.
        expect(
          StoreHours.openAt(hours, DateTime.utc(2026, 10, 14, 14)),
          isFalse,
        );
        // Thursday is not listed.
        expect(
          StoreHours.openAt(hours, DateTime.utc(2026, 10, 15, 6)),
          isFalse,
        );
      },
    );

    test('no hours means nothing to say', () {
      expect(
        StoreHours.openAt(const [], DateTime.utc(2026, 10, 14, 6)),
        isNull,
      );
    });

    test('a clock reads as 09:05', () {
      expect(StoreHours.clock(545), '09:05');
    });
  });

  group('store contact', () {
    test('a store without a WhatsApp number has no link, one with it opens with the owner text', () {
      expect(
        const PublicStore(
          id: 'a',
          name: 'A',
          slug: 'a',
          description: 'd',
          phone: '1',
        ).whatsAppUri,
        isNull,
      );

      final uri = const PublicStore(
        id: 'a',
        name: 'A',
        slug: 'a',
        description: 'd',
        phone: '1',
        whatsApp: '994501112233',
        whatsAppMessage: 'Salam! Ünvanı deyin.',
      ).whatsAppUri!;
      expect(uri.host, 'wa.me');
      expect(uri.path, '/994501112233');
      expect(uri.queryParameters['text'], 'Salam! Ünvanı deyin.');
    });

    test('only the networks with a link are listed', () {
      const s = PublicStore(
        id: 'a',
        name: 'A',
        slug: 'a',
        description: 'd',
        phone: '1',
        instagram: 'https://instagram.com/a',
        youtube: 'https://youtube.com/@a',
      );
      expect(s.links.map((e) => e.$1), [
        SocialKind.instagram,
        SocialKind.youtube,
      ]);
    });

    test('the contact form checks the number, the links and the hours', () {
      final bad = const StoreContactForm(
        phone: '123',
        email: 'not-an-email',
        whatsApp: '12',
        youtube: 'youtube',
        hours: [StoreHours(day: 1, openMinute: 900, closeMinute: 600)],
      ).validate();

      expect(bad['phone'], StoreIssue.tooShort);
      expect(bad['email'], StoreIssue.invalidEmail);
      expect(bad['whatsApp'], StoreIssue.invalidNumber);
      expect(bad['youtube'], StoreIssue.invalidUrl);
      expect(bad['hours'], StoreIssue.invalidHours);

      expect(
        const StoreContactForm(
          phone: '+994501234567',
          whatsApp: '050 111 22 33',
          youtube: 'https://youtube.com/@a',
          hours: [StoreHours(day: 1, openMinute: 540, closeMinute: 1080)],
        ).validate(),
        isEmpty,
      );
    });

    test('the contact form is sent with empty fields as null and the hours as a list', () {
      final json = const StoreContactForm(
        phone: ' +994501234567 ',
        whatsApp: '050 111 22 33',
        hours: [StoreHours(day: 2, openMinute: 600, closeMinute: 900)],
      ).toJson();

      expect(json['phoneNumber'], '+994501234567');
      expect(json['email'], isNull);
      expect(json['whatsAppNumber'], '050 111 22 33');
      expect(json['workingHours'], [
        {'day': 2, 'openMinute': 600, 'closeMinute': 900},
      ]);
    });

    test('the VIP place offer is read from the answer', () {
      final o = StoreVipOffer.fromJson({
        'name': 'VIP mağaza',
        'price': 12.5,
        'durationDays': 30,
        'offerMessage': 'Ana səhifədə görünün',
        'isOnSale': true,
        'isVip': true,
        'vipUntilUtc': '2026-11-20T08:00:00+00:00',
        'currency': 'AZN',
      });
      expect(o.price, 12.5);
      expect(o.durationDays, 30);
      expect(o.isOnSale && o.isVip, isTrue);
      expect(o.vipUntil, isNotNull);
      expect(StoreVipOffer.fromJson({'name': 'x'}).isOnSale, isFalse);
    });

    test('the stats are read from the answer', () {
      final s = StoreStats.fromJson({
        'followers': 4,
        'profileViews': 90,
        'profileViewsLast7Days': 12,
        'calls': 3,
        'last30Days': [
          {'day': '2026-10-13', 'views': 2},
          {'day': '2026-10-14', 'views': 5},
        ],
      });
      expect(s.followers, 4);
      expect(s.profileViews, 90);
      expect(s.viewsLast7Days, 12);
      expect(s.last30Views, [2, 5]);
    });
  });

  testWidgets(
    'the public page: VIP first (4 of 7), the ordinary ones (6 of 9), WhatsApp, hours',
    (tester) async {
      tester.view.physicalSize = const Size(900, 4200);
      tester.view.devicePixelRatio = 1;
      addTearDown(tester.view.reset);

      final repo = _Stores();
      final container = ProviderContainer(
        overrides: [storeRepositoryProvider.overrideWithValue(repo)],
      );
      addTearDown(container.dispose);
      final az = await AppL10n.delegate.load(const Locale('az'));

      await tester.pumpWidget(
        UncontrolledProviderScope(
          container: container,
          child: MaterialApp(
            localizationsDelegates: AppL10n.localizationsDelegates,
            supportedLocales: AppL10n.supportedLocales,
            locale: const Locale('az'),
            home: const StorePage(slug: 'numune'),
          ),
        ),
      );
      await tester.pumpAndSettle();

      expect(find.text('${az.storeVipTitle} (7)'), findsOneWidget);
      expect(find.text('${az.storeListingsTitle} (9)'), findsOneWidget);
      // 4 VIP and 6 ordinary at first.
      expect(find.textContaining('Elan v'), findsNWidgets(4));
      expect(find.textContaining('Elan r'), findsNWidgets(6));

      await tester.ensureVisible(find.text(az.storeShowAll).first);
      await tester.tap(find.text(az.storeShowAll).first);
      await tester.pumpAndSettle();
      expect(find.textContaining('Elan v'), findsNWidgets(7));

      // The contact side: WhatsApp and YouTube have buttons, TikTok has none, the week is listed.
      expect(find.bySemanticsLabel('WhatsApp'), findsOneWidget);
      expect(find.bySemanticsLabel('YouTube'), findsOneWidget);
      expect(find.bySemanticsLabel('TikTok'), findsNothing);
      // The hours are one line; the week opens when it is tapped.
      expect(find.text(az.storeHoursTitle), findsOneWidget);
      expect(find.text('09:00 – 18:00'), findsNothing);
      await tester.ensureVisible(find.text(az.storeHoursTitle));
      await tester.tap(find.text(az.storeHoursTitle));
      await tester.pumpAndSettle();
      expect(find.text('09:00 – 18:00'), findsOneWidget);
      expect(find.text(az.storeCall), findsOneWidget);
      expect(find.text(az.storeFollowers(12)), findsOneWidget);
      expect(repo.events, ['view']);
    },
  );
}
