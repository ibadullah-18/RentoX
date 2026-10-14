import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:go_router/go_router.dart';
import 'package:rentox/core/l10n/app_localizations.dart';
import 'package:rentox/features/catalog/domain/catalog_models.dart';
import 'package:rentox/shared/widgets/listing_grid.dart';

ListingSummary _l(int i) => ListingSummary(
  id: 'l$i',
  title: 'Mərkəzdə 80 kv.m ofis $i',
  price: 1500,
  currency: 'AZN',
  unit: RentalPeriodUnit.month,
  isVip: i.isEven,
  isFavorite: false,
  viewCount: 0,
  favoriteCount: 0,
  location: 'Bakı, Nizami rayonu',
);

void main() {
  for (final scale in [1.0, 1.3, 1.6]) {
    for (final width in [320.0, 360.0, 412.0, 600.0]) {
      testWidgets('cards fit at width $width, text x$scale', (tester) async {
        tester.view.physicalSize = Size(width * 3, 2400);
        tester.view.devicePixelRatio = 3;
        addTearDown(tester.view.reset);
        await tester.pumpWidget(
          ProviderScope(
            child: MaterialApp.router(
              locale: const Locale('az'),
              localizationsDelegates: AppL10n.localizationsDelegates,
              supportedLocales: AppL10n.supportedLocales,
              builder: (context, child) => MediaQuery(
                data: MediaQuery.of(context)
                    .copyWith(textScaler: TextScaler.linear(scale)),
                child: child!,
              ),
              routerConfig: GoRouter(
                routes: [
                  GoRoute(
                    path: '/',
                    builder: (_, _) => Scaffold(
                      body: CustomScrollView(
                        slivers: [
                          SliverListingGrid(
                            items: [for (var i = 0; i < 6; i++) _l(i)],
                          ),
                        ],
                      ),
                    ),
                  ),
                ],
              ),
            ),
          ),
        );
        await tester.pump(const Duration(milliseconds: 600));
        expect(tester.takeException(), isNull); // no RenderFlex overflow
      });
    }
  }
}
