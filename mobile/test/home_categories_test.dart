import 'package:dio/dio.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:go_router/go_router.dart';
import 'package:shared_preferences/shared_preferences.dart';
import 'package:rentox/core/l10n/app_localizations.dart';
import 'package:rentox/features/auth/domain/auth_models.dart';
import 'package:rentox/features/auth/presentation/auth_controller.dart';
import 'package:rentox/features/catalog/data/catalog_repository.dart';
import 'package:rentox/features/catalog/domain/catalog_models.dart';
import 'package:rentox/features/home/presentation/home_page.dart';
import 'package:rentox/features/home/presentation/home_state.dart';
import 'package:rentox/features/listing_create/data/listing_create_repository.dart';
import 'package:rentox/features/locations/data/locations_repository.dart';
import 'package:rentox/features/search/domain/search_options.dart';
import 'package:rentox/features/store/data/store_repository.dart';
import 'package:rentox/features/store/domain/store_models.dart';

import 'support/create_fakes.dart';

class _Guest extends AuthController {
  @override
  Future<AuthSession?> build() async => null;
}

class _Catalog extends FakeCatalogBase {
  final asked = <String?>[];
  final maxPrices = <double?>[];

  @override
  Future<List<Category>> categories(String language) async => const [
    Category(
      id: 'transport',
      slug: 'neqliyyat',
      name: 'Nəqliyyat',
      children: [
        Category(
          id: 'cars',
          slug: 'cars',
          name: 'Avtomobil',
          children: [
            Category(id: 'sport', slug: 'sport', name: 'Sport'),
            Category(id: 'comfort', slug: 'comfort', name: 'Komfort'),
          ],
        ),
        Category(id: 'motos', slug: 'motos', name: 'Motosiklet'),
      ],
    ),
    Category(id: 'homes', slug: 'emlak', name: 'Əmlak'),
  ];

  @override
  Future<Paged<ListingSummary>> listings({
    required String language,
    String? categoryId,
    String? search,
    double? minPrice,
    double? maxPrice,
    List<Map<String, dynamic>>? fieldFilters,
    SellerType seller = SellerType.all,
    SearchSort sort = SearchSort.date,
    String? cityId,
    String? districtId,
    RentalPeriodUnit? rentalPeriod,
    int page = 1,
    int pageSize = 20,
  }) async {
    asked.add(categoryId);
    maxPrices.add(maxPrice);
    return Paged(
      items: [
        ListingSummary(
          id: 'l-${categoryId ?? 'all'}',
          title: 'Listing in ${categoryId ?? 'all'}',
          price: 10,
          currency: 'AZN',
          unit: RentalPeriodUnit.day,
          isVip: false,
          isFavorite: false,
          viewCount: 0,
          favoriteCount: 0,
        ),
      ],
      page: 1,
      totalPages: 1,
      totalCount: 1,
    );
  }
}

class _Stores extends StoreRepository {
  _Stores() : super(Dio());

  @override
  Future<Paged<FollowedStore>> search({
    String search = '',
    int page = 1,
    int pageSize = 20,
  }) async => const Paged(items: [], page: 1, totalPages: 0, totalCount: 0);
}

void main() {
  final az = lookupAppL10n(const Locale('az'));

  Future<_Catalog> pumpHome(
    WidgetTester tester, {
    List<String>? visited,
  }) async {
    tester.view.physicalSize = const Size(1500, 2400);
    tester.view.devicePixelRatio = 3;
    addTearDown(tester.view.reset);
    SharedPreferences.setMockInitialValues({});
    final catalog = _Catalog();

    final router = GoRouter(
      routes: [
        GoRoute(path: '/', builder: (_, _) => const HomePage()),
        GoRoute(
          path: '/search',
          builder: (_, state) {
            visited?.add(state.uri.toString());
            return const Scaffold(body: Text('SEARCH PAGE'));
          },
        ),
      ],
    );

    await tester.pumpWidget(
      ProviderScope(
        overrides: [
          catalogRepositoryProvider.overrideWithValue(catalog),
          storeRepositoryProvider.overrideWithValue(_Stores()),
          authControllerProvider.overrideWith(_Guest.new),
          locationsRepositoryProvider.overrideWithValue(FakeLocations()),
          listingCreateRepositoryProvider.overrideWithValue(FakeCreateRepo()),
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
    return catalog;
  }

  testWidgets('picking a category replaces the strip with what is inside it', (
    tester,
  ) async {
    final visited = <String>[];
    final catalog = await pumpHome(tester, visited: visited);

    // The top level: every root category, no back card.
    expect(find.text('Nəqliyyat'), findsOneWidget);
    expect(find.text('Əmlak'), findsOneWidget);
    expect(find.byKey(const ValueKey('category-back')), findsNothing);
    expect(find.text('Listing in all'), findsOneWidget);

    // Pick "Nəqliyyat": it goes, its children come. We stay on the home page.
    await tester.tap(find.text('Nəqliyyat'));
    await tester.pumpAndSettle();
    expect(find.text('SEARCH PAGE'), findsNothing);
    expect(catalog.asked.last, 'transport');
    expect(find.text('Avtomobil'), findsOneWidget);
    expect(find.text('Motosiklet'), findsOneWidget);
    expect(find.text('Əmlak'), findsNothing); // no longer in the strip
    expect(find.byKey(const ValueKey('category-back')), findsOneWidget);
    // The title of the section and of the feed name the pick.
    expect(find.text('Nəqliyyat'), findsNWidgets(2));

    // Pick "Avtomobil": its own children replace the strip again.
    await tester.tap(find.text('Avtomobil'));
    await tester.pumpAndSettle();
    expect(catalog.asked.last, 'cars');
    expect(find.text('Sport'), findsOneWidget);
    expect(find.text('Komfort'), findsOneWidget);
    expect(find.text('Motosiklet'), findsNothing);
    expect(find.text('Nəqliyyat › Avtomobil'), findsOneWidget);

    // "Sport" has nothing inside: its siblings stay, "Sport" is marked.
    await tester.tap(find.text('Sport'));
    await tester.pumpAndSettle();
    expect(catalog.asked.last, 'sport');
    expect(find.text('Listing in sport'), findsOneWidget);
    expect(find.text('Komfort'), findsOneWidget);

    // A sibling replaces it; tapping the marked one un-picks it.
    await tester.tap(find.text('Komfort'));
    await tester.pumpAndSettle();
    expect(catalog.asked.last, 'comfort');
    // (the name is also the feed's title: the first one is the strip card)
    await tester.tap(find.text('Komfort').first);
    await tester.pumpAndSettle();
    expect(catalog.asked.last, 'cars');
    expect(find.text('Sport'), findsOneWidget);

    // Back goes one level up, level by level, to the top.
    await tester.tap(find.byKey(const ValueKey('category-back')));
    await tester.pumpAndSettle();
    expect(catalog.asked.last, 'transport');
    expect(find.text('Avtomobil'), findsOneWidget);

    await tester.tap(find.byKey(const ValueKey('category-back')));
    await tester.pumpAndSettle();
    expect(catalog.asked.last, isNull);
    expect(find.text('Əmlak'), findsOneWidget);
    expect(find.byKey(const ValueKey('category-back')), findsNothing);
    expect(visited, isEmpty);
    expect(tester.takeException(), isNull);
  });

  testWidgets('a search word shows in the bar and can be cleared', (
    tester,
  ) async {
    final catalog = await pumpHome(tester);
    final container = ProviderScope.containerOf(
      tester.element(find.byType(HomePage)),
    );

    container.read(homeQueryProvider.notifier).set('kamera');
    container.read(homeCategoryPathProvider.notifier).set([
      'transport',
      'cars',
    ]);
    await tester.pumpAndSettle();

    expect(find.text('kamera'), findsOneWidget);
    expect(find.text('Nəqliyyat › Avtomobil'), findsOneWidget);
    expect(find.text('Sport'), findsOneWidget); // inside the picked category
    expect(catalog.asked.last, 'cars');

    await tester.tap(find.byKey(const ValueKey('home-search-clear')));
    await tester.pumpAndSettle();
    expect(find.text('kamera'), findsNothing);
    expect(container.read(homeQueryProvider), '');
  });

  testWidgets('the filter button filters the home feed and stays here', (
    tester,
  ) async {
    final visited = <String>[];
    final catalog = await pumpHome(tester, visited: visited);

    await tester.tap(find.text('Nəqliyyat'));
    await tester.pumpAndSettle();

    await tester.tap(find.bySemanticsLabel(az.filters).first);
    await tester.pumpAndSettle();
    expect(find.text('SEARCH PAGE'), findsNothing);
    expect(find.text(az.priceFilter), findsOneWidget); // the sheet is here

    await tester.enterText(find.widgetWithText(TextField, az.priceMax), '50');
    await tester.pump();
    await tester.tap(find.text(az.applyAction));
    await tester.pumpAndSettle();

    // The feed was asked again: same category, now with the price limit.
    expect(catalog.asked.last, 'transport');
    expect(catalog.maxPrices.last, 50);
    expect(visited, isEmpty);

    // Changing the category keeps the price limit (it is not about fields).
    await tester.tap(find.text('Avtomobil'));
    await tester.pumpAndSettle();
    expect(catalog.asked.last, 'cars');
    expect(catalog.maxPrices.last, 50);
  });
}
