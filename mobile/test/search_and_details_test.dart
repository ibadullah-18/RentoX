import 'package:dio/dio.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:shared_preferences/shared_preferences.dart';
import 'package:rentox/core/l10n/app_localizations.dart';
import 'package:rentox/features/catalog/data/catalog_repository.dart';
import 'package:rentox/features/catalog/domain/catalog_models.dart';
import 'package:rentox/features/listing_create/domain/field_models.dart';
import 'package:rentox/features/listing_create/presentation/field_editors.dart';
import 'package:rentox/features/listing_details/presentation/listing_formatting.dart';
import 'package:rentox/shared/widgets/listing_card.dart';
import 'package:rentox/features/search/presentation/price_filter_sheet.dart';
import 'package:rentox/features/search/presentation/search_page.dart';
import 'package:rentox/features/search/presentation/search_controller.dart';

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

class _Call {
  _Call(this.search, this.categoryId, this.minPrice, this.maxPrice, this.page);
  final String? search;
  final String? categoryId;
  final double? minPrice;
  final double? maxPrice;
  final int page;
}

class FakeCatalog extends CatalogRepository {
  FakeCatalog() : super(Dio());

  final calls = <_Call>[];

  @override
  Future<List<Category>> categories(String language) async => const [];

  @override
  Future<Paged<ListingSummary>> listings({
    required String language,
    String? categoryId,
    String? search,
    double? minPrice,
    double? maxPrice,
    int page = 1,
    int pageSize = 20,
  }) async {
    calls.add(_Call(search, categoryId, minPrice, maxPrice, page));
    final items = {
      1: [summary('a'), summary('b')],
      2: [summary('b'), summary('c')],
    }[page]!;
    return Paged(items: items, page: page, totalPages: 2, totalCount: 4);
  }
}

void main() {
  final az = lookupAppL10n(const Locale('az'));

  group('parsePrice', () {
    test('accepts dot and comma decimals', () {
      expect(parsePrice('12'), 12);
      expect(parsePrice('12.5'), 12.5);
      expect(parsePrice('12,5'), 12.5);
    });

    test('empty or invalid input means "no limit"', () {
      expect(parsePrice(''), isNull);
      expect(parsePrice('  '), isNull);
      expect(parsePrice('abc'), isNull);
      expect(parsePrice('-5'), isNull);
    });
  });

  group('SearchFilters', () {
    test('value equality and active-filter flags', () {
      const a = SearchFilters(query: 'car', categoryId: 'c1', minPrice: 5);
      const b = SearchFilters(query: 'car', categoryId: 'c1', minPrice: 5);
      expect(a, b);
      expect(a.hasActiveFilters, isTrue);
      expect(const SearchFilters(query: 'car').hasActiveFilters, isFalse);
      expect(const SearchFilters().isEmpty, isTrue);
    });

    test('starts from the seed, then updates, trims and clears', () {
      const seed = SearchSeed(query: '  toyota ', categoryId: 'cat');
      final c = ProviderContainer();
      addTearDown(c.dispose);
      c.listen(searchFiltersProvider(seed), (_, _) {});

      expect(c.read(searchFiltersProvider(seed)).query, 'toyota');
      expect(c.read(searchFiltersProvider(seed)).categoryId, 'cat');

      final n = c.read(searchFiltersProvider(seed).notifier);
      n.setPrice(min: 10, max: 50);
      expect(c.read(searchFiltersProvider(seed)).minPrice, 10);
      expect(c.read(searchFiltersProvider(seed)).maxPrice, 50);

      n.clearFilters();
      final f = c.read(searchFiltersProvider(seed));
      expect(f.query, 'toyota'); // typed text is kept
      expect(f.hasActiveFilters, isFalse);
    });
  });

  group('SearchResultsController', () {
    const seed = SearchSeed();

    ProviderContainer container(FakeCatalog repo) {
      final c = ProviderContainer(
        overrides: [catalogRepositoryProvider.overrideWithValue(repo)],
      );
      addTearDown(c.dispose);
      c.listen(searchFiltersProvider(seed), (_, _) {});
      c.listen(searchResultsProvider(seed), (_, _) {});
      return c;
    }

    test('refetches page 1 with the new filters', () async {
      final repo = FakeCatalog();
      final c = container(repo);

      await c.read(searchResultsProvider(seed).future);
      expect(repo.calls.last.search, '');

      c.read(searchFiltersProvider(seed).notifier)
        ..setQuery('camry')
        ..setPrice(min: 20);
      final state = await c.read(searchResultsProvider(seed).future);

      expect(repo.calls.last.search, 'camry');
      expect(repo.calls.last.minPrice, 20);
      expect(repo.calls.last.page, 1);
      expect(state.items.map((e) => e.id), ['a', 'b']);
    });

    test('loadMore appends the next page without duplicates', () async {
      final repo = FakeCatalog();
      final c = container(repo);
      await c.read(searchResultsProvider(seed).future);

      await c.read(searchResultsProvider(seed).notifier).loadMore();
      final state = c.read(searchResultsProvider(seed)).requireValue;

      expect(state.items.map((e) => e.id), ['a', 'b', 'c']);
      expect(state.hasMore, isFalse);

      await c.read(searchResultsProvider(seed).notifier).loadMore();
      expect(repo.calls.map((e) => e.page), [1, 2]); // nothing left to load
    });
  });

  group('ListingDetails.fromJson', () {
    final json = {
      'id': 'x1',
      'title': 'Toyota Camry 2023',
      'description': 'Clean car',
      'price': 85,
      'currency': 'AZN',
      'rentalPeriodUnit': 2,
      'viewCount': 12,
      'favoriteCount': 3,
      'isFavorite': true,
      'publishedAtUtc': '2026-10-01T10:00:00Z',
      'isVip': true,
      'categoryName': 'Cars',
      'owner': {
        'id': 'o1',
        'fullName': 'Murad',
        'phoneNumber': '+994501234567',
      },
      'images': [
        {
          'id': 'i2',
          'url': '/api/listing-images/i2',
          'displayOrder': 2,
          'isCover': false,
        },
        {
          'id': 'i1',
          'url': '/api/listing-images/i1',
          'displayOrder': 1,
          'isCover': true,
        },
      ],
      'fields': [
        {'label': 'Year', 'type': 2, 'numericValue': 2023},
        {
          'label': 'Fuel',
          'type': 5,
          'selections': [
            {'optionId': 'o', 'value': 'petrol', 'label': 'Benzin'},
          ],
        },
        {'label': 'Empty', 'type': 1, 'textValue': '  '},
      ],
    };

    test('parses fields and orders images by displayOrder', () {
      final d = ListingDetails.fromJson(json);

      expect(d.title, 'Toyota Camry 2023');
      expect(d.unit, RentalPeriodUnit.day);
      expect(d.isVip, isTrue);
      expect(d.owner.fullName, 'Murad');
      expect(d.images.map((i) => i.id), ['i1', 'i2']);
      expect(d.images.first.url, endsWith('/api/listing-images/i1'));
      expect(d.fields, hasLength(3));
    });

    test('formats field values and skips empty ones', () {
      final d = ListingDetails.fromJson(json);
      final values = d.fields
          .map((f) => fieldDisplayValue(az, 'az', f))
          .toList();

      expect(values[0], '2023'); // a year: no thousands separator
      expect(values[1], 'Benzin');
      expect(values[2], isNull);
    });

    test('groups large whole numbers by locale', () {
      const km = ListingFieldValue(
        label: 'Km',
        type: FieldType.wholeNumber,
        numericValue: 125000,
      );
      expect(fieldDisplayValue(az, 'az', km), '125.000');
    });

    test('formats booleans with the active language', () {
      const yes = ListingFieldValue(
        label: 'AC',
        type: FieldType.boolean,
        flagValue: true,
      );
      expect(fieldDisplayValue(az, 'az', yes), az.yes);
    });
  });

  group('publishedLabel', () {
    final now = DateTime(2026, 10, 8, 12);

    test('today, yesterday and N days ago', () {
      expect(
        publishedLabel(az, DateTime(2026, 10, 8, 1), now: now),
        az.publishedToday,
      );
      expect(
        publishedLabel(az, DateTime(2026, 10, 7, 23), now: now),
        az.publishedYesterday,
      );
      expect(
        publishedLabel(az, DateTime(2026, 10, 3, 9), now: now),
        az.publishedDaysAgo(5),
      );
    });
  });

  group('SearchPage', () {
    testWidgets('opens cleanly, shows results and searches as you type', (
      tester,
    ) async {
      SharedPreferences.setMockInitialValues({});
      final repo = FakeCatalog();

      await tester.pumpWidget(
        ProviderScope(
          overrides: [catalogRepositoryProvider.overrideWithValue(repo)],
          child: MaterialApp(
            locale: const Locale('az'),
            localizationsDelegates: AppL10n.localizationsDelegates,
            supportedLocales: AppL10n.supportedLocales,
            home: const SearchPage(initialQuery: 'cam'),
          ),
        ),
      );
      await tester.pump(); // first frame must not throw
      await tester.pump(const Duration(milliseconds: 50));

      expect(tester.takeException(), isNull);
      expect(repo.calls.first.search, 'cam'); // seeded from the route
      expect(find.text('Listing a'), findsOneWidget);
      expect(find.text(az.resultsFound(4)), findsOneWidget);

      await tester.enterText(find.byType(TextField), 'camry');
      await tester.pump(const Duration(milliseconds: 500)); // debounce
      await tester.pump();

      expect(repo.calls.last.search, 'camry');
      expect(tester.takeException(), isNull);
    });
  });

  group('number and price formatting', () {
    test('years are never grouped, big numbers are', () {
      expect(formatWholeNumber('az', 2023), '2023');
      expect(formatWholeNumber('az', 9999), '9999');
      expect(formatWholeNumber('az', 125000), '125.000');
    });

    test('review step shows a year the same way as the details page', () {
      final def = FieldDefinition(
        id: 'y',
        label: 'Year',
        type: FieldType.wholeNumber,
        isRequired: true,
        allowCustomValue: false,
        displayOrder: 0,
      );
      expect(
        answerDisplay(az, 'az', def, const FieldAnswer(text: '2023')),
        '2023',
      );
    });

    test('prices use the active language decimal separator', () {
      expect(
        formatPriceValue(
          az,
          price: 85.5,
          currency: 'AZN',
          unit: RentalPeriodUnit.day,
        ),
        '85,5 AZN / ${az.unitDay}',
      );
      expect(
        formatPriceValue(
          az,
          price: 1500,
          currency: 'AZN',
          unit: RentalPeriodUnit.negotiable,
        ),
        '1.500 AZN',
      );
    });
  });
}
