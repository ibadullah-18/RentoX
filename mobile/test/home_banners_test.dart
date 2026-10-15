import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:rentox/features/catalog/data/catalog_repository.dart';
import 'package:rentox/features/home/domain/home_banner.dart';
import 'package:rentox/features/home/presentation/home_banners.dart';

Widget _app(List<HomeBanner> banners) => ProviderScope(
  overrides: [bannersProvider.overrideWith((ref) async => banners)],
  child: const MaterialApp(home: Scaffold(body: HomeBanners())),
);

void main() {
  test(
    'a banner is read from the API answer, with the picture address made whole',
    () {
      final banner = HomeBanner.fromJson({
        'id': 'b1',
        'title': 'Yay endirimi',
        'subtitle': null,
        'imageUrl': '/api/banners/b1/image',
        'linkUrl': '/listing/abc',
      });

      expect(banner.title, 'Yay endirimi');
      expect(banner.subtitle, isNull);
      expect(banner.linkUrl, '/listing/abc');
      expect(banner.imageUrl, endsWith('/api/banners/b1/image'));
      expect(banner.imageUrl, startsWith('http'));
    },
  );

  testWidgets('nothing is shown when there are no banners', (tester) async {
    await tester.pumpWidget(_app(const []));
    await tester.pump();

    expect(find.byType(PageView), findsNothing);
  });

  testWidgets('the banners show their titles and dots when there are several', (
    tester,
  ) async {
    await tester.pumpWidget(
      _app(const [
        HomeBanner(
          id: '1',
          title: 'Birinci',
          imageUrl: 'http://localhost:1/a.png',
          subtitle: 'Alt yazı',
        ),
        HomeBanner(
          id: '2',
          title: 'İkinci',
          imageUrl: 'http://localhost:1/b.png',
        ),
      ]),
    );
    await tester.pump();
    await tester.pump(const Duration(milliseconds: 50));

    expect(find.text('Birinci'), findsOneWidget);
    expect(find.text('Alt yazı'), findsOneWidget);
    expect(find.byType(AnimatedContainer), findsNWidgets(2));
  });

  testWidgets('a single banner has no dots', (tester) async {
    await tester.pumpWidget(
      _app(const [
        HomeBanner(
          id: '1',
          title: 'Yeganə',
          imageUrl: 'http://localhost:1/a.png',
        ),
      ]),
    );
    await tester.pump();
    await tester.pump(const Duration(milliseconds: 50));

    expect(find.text('Yeganə'), findsOneWidget);
    expect(find.byType(AnimatedContainer), findsNothing);
  });
}
