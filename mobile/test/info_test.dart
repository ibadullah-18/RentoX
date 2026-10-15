import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_riverpod/misc.dart' show Override;
import 'package:flutter_test/flutter_test.dart';
import 'package:rentox/core/l10n/app_localizations.dart';
import 'package:rentox/features/info/data/info_repository.dart';
import 'package:rentox/features/info/domain/info_models.dart';
import 'package:rentox/features/info/presentation/info_page.dart';
import 'package:rentox/features/info/presentation/markdown_view.dart';

Widget _wrap(Widget child, {List<Override> overrides = const []}) =>
    ProviderScope(
      overrides: overrides,
      child: MaterialApp(
        locale: const Locale('az'),
        localizationsDelegates: AppL10n.localizationsDelegates,
        supportedLocales: AppL10n.supportedLocales,
        home: Scaffold(body: SingleChildScrollView(child: child)),
      ),
    );

void main() {
  group('SiteInfo', () {
    test(
      'empty and blank values become none, so nothing is shown for them',
      () {
        final info = SiteInfo.fromJson({
          'operatorName': '  Əli Əliyev ',
          'email': '',
          'phone': '   ',
          'instagram': null,
        });

        expect(info.operatorName, 'Əli Əliyev');
        expect(info.email, isNull);
        expect(info.phone, isNull);
        expect(info.instagram, isNull);
        expect(info.hasContacts, isFalse);
      },
    );

    test('a single channel is enough to show the contact block', () {
      expect(SiteInfo.fromJson({'hotline': '1234'}).hasContacts, isTrue);
      expect(
        SiteInfo.fromJson({'facebook': 'https://facebook.com/x'}).hasContacts,
        isTrue,
      );
      // The operator's name alone is not a way to reach anyone.
      expect(
        SiteInfo.fromJson({'operatorName': 'X', 'voen': '1'}).hasContacts,
        isFalse,
      );
    });
  });

  group('safeLink', () {
    test('only web, mail and phone addresses are followed', () {
      expect(safeLink('https://rentox.az/x'), isNotNull);
      expect(safeLink('http://rentox.az'), isNotNull);
      expect(safeLink('mailto:info@rentox.az'), isNotNull);
      expect(safeLink('tel:+994501234567'), isNotNull);
      expect(safeLink('javascript:alert(1)'), isNull);
      expect(safeLink('file:///etc/passwd'), isNull);
      expect(safeLink('/listing/1'), isNull);
    });
  });

  group('MarkdownView', () {
    testWidgets('headings, paragraphs, bold text and lists are shown as text', (
      tester,
    ) async {
      await tester.pumpWidget(
        _wrap(
          const MarkdownView(
            text: '## 1. Ümumi\n\nBu **vacib** mətndir.\nİkinci sətir.\n\n- birinci\n- ikinci\n\n1. bir\n2. iki\n\n## 2. Son',
          ),
        ),
      );

      expect(find.text('1. Ümumi'), findsOneWidget);
      expect(find.text('2. Son'), findsOneWidget);
      expect(
        find.textContaining('Bu vacib mətndir.', findRichText: true),
        findsOneWidget,
      );
      expect(
        find.textContaining('İkinci sətir.', findRichText: true),
        findsOneWidget,
      );
      expect(find.text('birinci'), findsOneWidget);
      expect(find.text('ikinci'), findsOneWidget);
      expect(find.text('•'), findsNWidgets(2));
      expect(find.text('1.'), findsOneWidget);
      expect(find.text('2.'), findsOneWidget);
    });

    testWidgets('a numbered clause like 1.1. is plain text, not a list', (
      tester,
    ) async {
      await tester.pumpWidget(
        _wrap(const MarkdownView(text: '1.1. RentoX elan platformasıdır.')),
      );

      expect(
        find.textContaining(
          '1.1. RentoX elan platformasıdır.',
          findRichText: true,
        ),
        findsOneWidget,
      );
      expect(find.text('1.'), findsNothing);
    });

    testWidgets('html in a text is shown as it is and never interpreted', (
      tester,
    ) async {
      await tester.pumpWidget(
        _wrap(
          const MarkdownView(
            text: '<script>alert(1)</script> [kliklə](javascript:alert(1))',
          ),
        ),
      );

      expect(
        find.textContaining('<script>alert(1)</script>', findRichText: true),
        findsOneWidget,
      );
      // The link to a script is shown as its words only.
      expect(find.textContaining('kliklə', findRichText: true), findsOneWidget);
    });
  });

  group('InfoPage', () {
    List<Override> overrides(SiteInfo site) => [
      infoPagesProvider.overrideWith(
        (ref) async => const [
          InfoPageSummary(slug: 'terms', title: 'İstifadə şərtləri'),
          InfoPageSummary(slug: 'about', title: 'Haqqımızda'),
        ],
      ),
      siteInfoProvider.overrideWith((ref) async => site),
    ];

    testWidgets(
      'lists the pages and only the contact channels that are filled in',
      (tester) async {
        tester.view.physicalSize = const Size(800, 2400);
        tester.view.devicePixelRatio = 1;
        addTearDown(tester.view.reset);

        await tester.pumpWidget(
          ProviderScope(
            overrides: overrides(
              const SiteInfo(
                operatorName: 'Əli Əliyev',
                voen: '1234567890',
                email: 'info@rentox.az',
                hotline: '1234',
                instagram: 'https://www.instagram.com/rentox',
              ),
            ),
            child: MaterialApp(
              locale: const Locale('az'),
              localizationsDelegates: AppL10n.localizationsDelegates,
              supportedLocales: AppL10n.supportedLocales,
              home: const InfoPage(),
            ),
          ),
        );
        await tester.pump(const Duration(milliseconds: 100));
        await tester.pump(const Duration(milliseconds: 100));

        expect(find.text('İstifadə şərtləri'), findsOneWidget);
        expect(find.text('Haqqımızda'), findsOneWidget);
        expect(find.text('E-poçt'), findsOneWidget);
        expect(find.text('info@rentox.az'), findsOneWidget);
        expect(find.text('Qaynar xətt'), findsOneWidget);
        expect(find.text('Instagram'), findsOneWidget);
        // Not filled in: not shown.
        expect(find.text('Telefon'), findsNothing);
        expect(find.text('WhatsApp'), findsNothing);
        expect(find.text('TikTok'), findsNothing);
        expect(find.text('Facebook'), findsNothing);
        expect(find.text('Əli Əliyev · VÖEN 1234567890'), findsOneWidget);
      },
    );

    testWidgets(
      'without any contact channel there is no contact block at all',
      (tester) async {
        await tester.pumpWidget(
          ProviderScope(
            overrides: overrides(const SiteInfo()),
            child: MaterialApp(
              locale: const Locale('az'),
              localizationsDelegates: AppL10n.localizationsDelegates,
              supportedLocales: AppL10n.supportedLocales,
              home: const InfoPage(),
            ),
          ),
        );
        await tester.pump(const Duration(milliseconds: 100));
        await tester.pump(const Duration(milliseconds: 100));

        expect(find.text('İstifadə şərtləri'), findsOneWidget);
        expect(find.text('Əlaqə'), findsNothing);
      },
    );
  });
}
