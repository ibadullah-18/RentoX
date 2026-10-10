import 'package:dio/dio.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:shared_preferences/shared_preferences.dart';
import 'package:rentox/core/l10n/app_localizations.dart';
import 'package:rentox/features/catalog/data/catalog_repository.dart';
import 'package:rentox/features/catalog/domain/catalog_models.dart';
import 'package:rentox/features/search/domain/search_options.dart';
import 'package:rentox/features/search/presentation/search_page.dart';
import 'package:rentox/features/search/presentation/store_search_controller.dart';
import 'package:rentox/features/store/data/store_repository.dart';
import 'package:rentox/features/store/domain/store_models.dart';

FollowedStore store(String id, {String description = ''}) => FollowedStore(
  id: id,
  name: 'Store $id',
  slug: 'store-$id',
  description: description,
  activeListingCount: 3,
  followerCount: 2,
);

class FakeCatalog extends CatalogRepository {
  FakeCatalog() : super(Dio());

  @override
  Future<List<Category>> categories(String language) async => const [];

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
    int page = 1,
    int pageSize = 20,
  }) async => const Paged(items: [], page: 1, totalPages: 0, totalCount: 0);
}

class FakeStores extends StoreRepository {
  FakeStores() : super(Dio());

  final searches = <String>[];
  final pages = <int>[];

  @override
  Future<Paged<FollowedStore>> search({
    String search = '',
    int page = 1,
    int pageSize = 20,
  }) async {
    searches.add(search);
    pages.add(page);
    final items = {
      1: [store('a', description: 'Cars for rent'), store('b')],
      2: [store('b'), store('c')],
    }[page]!;
    return Paged(items: items, page: page, totalPages: 2, totalCount: 3);
  }
}

void main() {
  final az = lookupAppL10n(const Locale('az'));

  group('models', () {
    test('a store from the search keeps its description and counts', () {
      final s = FollowedStore.fromJson({
        'storeId': 'x',
        'name': 'Avto',
        'slug': 'avto',
        'description': 'Cars',
        'logoImageUrl': null,
        'activeListingCount': 5,
        'followerCount': 7,
      });
      expect(s.description, 'Cars');
      expect(s.activeListingCount, 5);
      expect(s.followerCount, 7);
      expect(s.logoUrl, isNull);
    });

    test('a listing owner with a store exposes it', () {
      final owner = ListingOwner.fromJson({
        'id': 'u',
        'fullName': 'Aysel',
        'phoneNumber': '+994',
        'store': {
          'id': 's',
          'name': 'Avto Dünya',
          'slug': 'avto-dunya',
          'logoImageUrl': null,
        },
      });
      expect(owner.store?.slug, 'avto-dunya');
      expect(owner.store?.name, 'Avto Dünya');
    });

    test('a listing owner without a store (or with a broken one) has none', () {
      expect(ListingOwner.fromJson({'id': 'u', 'fullName': 'A'}).store, isNull);
      expect(
        ListingOwner.fromJson({'id': 'u', 'fullName': 'A', 'store': null})
            .store,
        isNull,
      );
      expect(
        ListingOwner.fromJson({
          'id': 'u',
          'fullName': 'A',
          'store': {'name': 'x', 'slug': ''},
        }).store,
        isNull,
      );
    });
  });

  group('StoreSearchController', () {
    test(
      'loads the first page, then appends the next without duplicates',
      () async {
        final repo = FakeStores();
        final c = ProviderContainer(
          overrides: [storeRepositoryProvider.overrideWithValue(repo)],
        );
        addTearDown(c.dispose);
        c.listen(storeSearchProvider('avto'), (_, _) {});

        final first = await c.read(storeSearchProvider('avto').future);
        expect(first.items.map((s) => s.id), ['a', 'b']);
        expect(first.hasMore, isTrue);
        expect(repo.searches.single, 'avto');

        await c.read(storeSearchProvider('avto').notifier).loadMore();
        final all = c.read(storeSearchProvider('avto')).value!;
        expect(all.items.map((s) => s.id), ['a', 'b', 'c']); // "b" only once
        expect(all.page, 2);
        expect(all.loadingMore, isFalse);
      },
    );
  });

  group('SearchPage stores tab', () {
    testWidgets('switches to stores, searches them and back', (tester) async {
      SharedPreferences.setMockInitialValues({});
      final stores = FakeStores();

      await tester.pumpWidget(
        ProviderScope(
          overrides: [
            catalogRepositoryProvider.overrideWithValue(FakeCatalog()),
            storeRepositoryProvider.overrideWithValue(stores),
          ],
          child: MaterialApp(
            locale: const Locale('az'),
            localizationsDelegates: AppL10n.localizationsDelegates,
            supportedLocales: AppL10n.supportedLocales,
            home: const SearchPage(initialQuery: 'avto'),
          ),
        ),
      );
      await tester.pump(const Duration(milliseconds: 50));

      // Stores are not requested until their tab is opened.
      expect(stores.searches, isEmpty);

      await tester.tap(find.text(az.searchTabStores));
      await tester.pump();
      await tester.pump(const Duration(milliseconds: 50));

      expect(tester.takeException(), isNull);
      expect(stores.searches.single, 'avto');
      expect(find.text(az.storesFound(3)), findsOneWidget);
      expect(find.text('Store a'), findsOneWidget);
      expect(find.text('Cars for rent'), findsOneWidget);

      await tester.enterText(find.byType(TextField), 'texno');
      await tester.pump(const Duration(milliseconds: 500));
      await tester.pump();
      expect(stores.searches.last, 'texno');

      await tester.tap(find.text(az.searchTabListings));
      await tester.pump();
      await tester.pump(const Duration(milliseconds: 50));
      expect(find.text(az.noResults), findsOneWidget);
      expect(tester.takeException(), isNull);
    });
  });
}
