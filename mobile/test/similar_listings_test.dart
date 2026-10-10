import 'package:dio/dio.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:go_router/go_router.dart';
import 'package:shared_preferences/shared_preferences.dart';
import 'package:rentox/core/l10n/app_localizations.dart';
import 'package:rentox/features/catalog/data/catalog_repository.dart';
import 'package:rentox/features/catalog/domain/catalog_models.dart';
import 'package:rentox/features/listing_details/presentation/similar_listings.dart';

ListingSummary summary(String id) => ListingSummary(
  id: id,
  title: 'Listing $id',
  price: 10,
  currency: 'AZN',
  unit: RentalPeriodUnit.day,
  isVip: false,
  isFavorite: false,
  viewCount: 0,
  favoriteCount: 0,
);

class FakeCatalog extends CatalogRepository {
  FakeCatalog(this.result) : super(Dio());

  final List<ListingSummary> result;
  final asked = <String>[];

  @override
  Future<List<ListingSummary>> similar(String id, String language) async {
    asked.add(id);
    return result;
  }
}

Future<void> pump(
  WidgetTester tester,
  FakeCatalog repo, {
  String? categoryId = 'cat-1',
  String? opened,
}) async {
  SharedPreferences.setMockInitialValues({});
  final router = GoRouter(
    routes: [
      GoRoute(
        path: '/',
        builder: (_, _) => Scaffold(
          body: CustomScrollView(
            slivers: [
              SimilarListingsSliver(listingId: 'src', categoryId: categoryId),
            ],
          ),
        ),
      ),
      GoRoute(
        path: '/search',
        builder: (_, state) => Scaffold(
          body: Text('search:${state.uri.queryParameters['categoryId']}'),
        ),
      ),
    ],
  );
  await tester.pumpWidget(
    ProviderScope(
      overrides: [catalogRepositoryProvider.overrideWithValue(repo)],
      child: MaterialApp.router(
        routerConfig: router,
        locale: const Locale('az'),
        localizationsDelegates: AppL10n.localizationsDelegates,
        supportedLocales: AppL10n.supportedLocales,
      ),
    ),
  );
  await tester.pump();
  await tester.pump(const Duration(milliseconds: 100));
}

void main() {
  final az = lookupAppL10n(const Locale('az'));

  testWidgets('shows similar listings with a button to see more', (
    tester,
  ) async {
    final repo = FakeCatalog([summary('a'), summary('b')]);
    await pump(tester, repo);

    expect(repo.asked, ['src']);
    expect(find.text(az.similarListings), findsOneWidget);
    expect(find.text('Listing a'), findsOneWidget);
    expect(find.text('Listing b'), findsOneWidget);

    await tester.scrollUntilVisible(
      find.text(az.similarMore),
      200,
      scrollable: find.byType(Scrollable).first,
    );
    await tester.tap(find.text(az.similarMore));
    await tester.pumpAndSettle();

    expect(find.text('search:cat-1'), findsOneWidget);
  });

  testWidgets('shows nothing when there is nothing similar', (tester) async {
    await pump(tester, FakeCatalog(const []));

    expect(find.text(az.similarListings), findsNothing);
    expect(tester.takeException(), isNull);
  });

  testWidgets('without a category there is no "see more" button', (
    tester,
  ) async {
    await pump(tester, FakeCatalog([summary('a')]), categoryId: null);

    expect(find.text(az.similarListings), findsOneWidget);
    expect(find.text(az.similarMore), findsNothing);
  });
}
