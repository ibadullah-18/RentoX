import 'dart:convert';
import 'dart:typed_data';

import 'package:dio/dio.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:shared_preferences/shared_preferences.dart';
import 'package:rentox/core/l10n/app_localizations.dart';
import 'package:rentox/features/catalog/data/catalog_repository.dart';
import 'package:rentox/features/catalog/domain/catalog_models.dart';
import 'package:rentox/features/listing_create/data/listing_create_repository.dart';
import 'package:rentox/features/listing_create/domain/field_models.dart';
import 'package:rentox/features/search/domain/field_filter.dart';
import 'package:rentox/features/search/domain/search_options.dart';
import 'package:rentox/features/search/presentation/filters_sheet.dart';
import 'package:rentox/features/search/presentation/search_controller.dart';

import 'support/create_fakes.dart';

FieldDefinition filterable(
  String id,
  FieldType type, {
  List<String> options = const [],
}) => FieldDefinition(
  id: id,
  label: 'Label $id',
  type: type,
  isRequired: false,
  allowCustomValue: false,
  displayOrder: 0,
  isFilterable: true,
  options: [for (final o in options) FieldOption(id: o, value: o, label: o)],
);

class _CapturingAdapter implements HttpClientAdapter {
  RequestOptions? last;

  @override
  Future<ResponseBody> fetch(
    RequestOptions options,
    Stream<Uint8List>? requestStream,
    Future<void>? cancelFuture,
  ) async {
    last = options;
    return ResponseBody.fromString(
      jsonEncode({
        'items': [],
        'page': 1,
        'pageSize': 20,
        'totalCount': 0,
        'totalPages': 0,
      }),
      200,
      headers: {
        Headers.contentTypeHeader: ['application/json'],
      },
    );
  }

  @override
  void close({bool force = false}) {}
}

Future<void> reveal(WidgetTester tester, Finder target) =>
    tester.scrollUntilVisible(
      target,
      200,
      scrollable: find
          .descendant(
            of: find.byType(ListView),
            matching: find.byType(Scrollable),
          )
          .first,
    );

void main() {
  final az = lookupAppL10n(const Locale('az'));

  group('FieldFilterValue', () {
    test('encodes only what was chosen, in a stable order', () {
      final encoded = encodeFieldFilters({
        'b': const FieldFilterValue(min: 2020, max: 2025),
        'a': const FieldFilterValue(optionIds: {'x', 'w'}),
        'c': const FieldFilterValue(flag: true),
        'd': FieldFilterValue(from: DateTime(2026, 1, 5)),
        'empty': const FieldFilterValue(),
      });

      expect(encoded, [
        {
          'fieldId': 'a',
          'optionIds': ['w', 'x'],
        },
        {'fieldId': 'b', 'min': 2020.0, 'max': 2025.0},
        {'fieldId': 'c', 'flag': true},
        {'fieldId': 'd', 'from': '2026-01-05'},
      ]);
    });

    test('nothing chosen means no filters parameter at all', () {
      expect(encodeFieldFilters({}), isNull);
      expect(encodeFieldFilters({'a': const FieldFilterValue()}), isNull);
    });

    test('equal values are equal regardless of option order', () {
      expect(
        const FieldFilterValue(optionIds: {'a', 'b'}),
        const FieldFilterValue(optionIds: {'b', 'a'}),
      );
    });
  });

  group('search request', () {
    test(
      'sends seller type and sort only when they are not the defaults',
      () async {
        final adapter = _CapturingAdapter();
        final repo = CatalogRepository(Dio()..httpClientAdapter = adapter);

        await repo.listings(language: 'az');
        expect(
          adapter.last!.queryParameters.containsKey('sellerType'),
          isFalse,
        );
        expect(adapter.last!.queryParameters.containsKey('sort'), isFalse);

        await repo.listings(
          language: 'az',
          seller: SellerType.individual,
          sort: SearchSort.priceDesc,
        );
        expect(adapter.last!.queryParameters['sellerType'], 'individual');
        expect(adapter.last!.queryParameters['sort'], 'price_desc');
      },
    );

    test(
      'seller type and sort count as sheet filters and take part in equality',
      () {
        const sorted = SearchFilters(sort: SearchSort.priceAsc);
        expect(sorted.hasSheetFilters, isTrue);
        expect(const SearchFilters().hasSheetFilters, isFalse);
        expect(sorted == const SearchFilters(), isFalse);
        expect(
          const SearchFilters(seller: SellerType.store) ==
              const SearchFilters(seller: SellerType.store),
          isTrue,
        );
      },
    );
  });

  group('SearchFilters fields', () {
    test('changing the category drops the field conditions', () {
      const f = SearchFilters(
        categoryId: 'cars',
        fields: {
          'brand': FieldFilterValue(optionIds: {'toyota'}),
        },
      );
      expect(f.hasSheetFilters, isTrue);

      expect(f.copyWith(categoryId: 'cars').fields, isNotEmpty);
      expect(f.copyWith(categoryId: 'homes').fields, isEmpty);
      expect(f.copyWith(clearCategory: true).fields, isEmpty);
      expect(f.copyWith(query: 'toyota').fields, isNotEmpty);
    });

    test('fields take part in equality', () {
      const a = SearchFilters(
        fields: {
          'brand': FieldFilterValue(optionIds: {'x'}),
        },
      );
      const b = SearchFilters(
        fields: {
          'brand': FieldFilterValue(optionIds: {'x'}),
        },
      );
      expect(a, b);
      expect(a.hashCode, b.hashCode);
      expect(a == const SearchFilters(), isFalse);
    });

    test('the notifier keeps only non-empty conditions', () {
      const seed = SearchSeed(categoryId: 'cars');
      final c = ProviderContainer();
      addTearDown(c.dispose);
      c.listen(searchFiltersProvider(seed), (_, _) {});

      c
          .read(searchFiltersProvider(seed).notifier)
          .setSheetFilters(
            min: 10,
            fields: {
              'brand': const FieldFilterValue(optionIds: {'x'}),
              'year': const FieldFilterValue(),
            },
          );

      final f = c.read(searchFiltersProvider(seed));
      expect(f.minPrice, 10);
      expect(f.fields.keys, ['brand']);
    });
  });

  group('filters sheet', () {
    Future<SheetFilters?> open(
      WidgetTester tester, {
      required SearchFilters current,
      required List<FieldDefinition> defs,
    }) async {
      SharedPreferences.setMockInitialValues({});
      SheetFilters? result;
      var closed = false;

      await tester.pumpWidget(
        ProviderScope(
          overrides: [
            listingCreateRepositoryProvider.overrideWithValue(
              FakeCreateRepo(defs: defs),
            ),
          ],
          child: MaterialApp(
            locale: const Locale('az'),
            localizationsDelegates: AppL10n.localizationsDelegates,
            supportedLocales: AppL10n.supportedLocales,
            home: Builder(
              builder: (context) => Scaffold(
                body: Center(
                  child: ElevatedButton(
                    onPressed: () async {
                      result = await showFiltersSheet(
                        context,
                        current: current,
                      );
                      closed = true;
                    },
                    child: const Text('open'),
                  ),
                ),
              ),
            ),
          ),
        ),
      );
      await tester.tap(find.text('open'));
      await tester.pump();
      await tester.pump(const Duration(milliseconds: 400));
      addTearDown(() {
        if (!closed) result = null;
      });
      return result;
    }

    testWidgets('without a category only the price is offered', (tester) async {
      await open(tester, current: const SearchFilters(), defs: const []);

      expect(find.text(az.priceFilter), findsOneWidget);
      expect(find.text(az.filtersPickCategory), findsOneWidget);
      expect(tester.takeException(), isNull);
    });

    testWidgets('shows the category fields and returns every choice', (
      tester,
    ) async {
      SheetFilters? result;
      final defs = [
        filterable('brand', FieldType.singleSelect, options: ['Toyota', 'BMW']),
        filterable('gear', FieldType.singleSelect, options: ['Auto', 'Manual']),
        filterable('year', FieldType.wholeNumber),
        filterable('electric', FieldType.boolean),
        FieldDefinition(
          id: 'hidden',
          label: 'Not filterable',
          type: FieldType.singleSelect,
          isRequired: false,
          allowCustomValue: false,
          displayOrder: 0,
        ),
      ];

      SharedPreferences.setMockInitialValues({});
      await tester.pumpWidget(
        ProviderScope(
          overrides: [
            listingCreateRepositoryProvider.overrideWithValue(
              FakeCreateRepo(defs: defs),
            ),
          ],
          child: MaterialApp(
            locale: const Locale('az'),
            localizationsDelegates: AppL10n.localizationsDelegates,
            supportedLocales: AppL10n.supportedLocales,
            home: Builder(
              builder: (context) => Scaffold(
                body: ElevatedButton(
                  onPressed: () async => result = await showFiltersSheet(
                    context,
                    current: const SearchFilters(categoryId: 'cars'),
                  ),
                  child: const Text('open'),
                ),
              ),
            ),
          ),
        ),
      );
      await tester.tap(find.text('open'));
      await tester.pump();
      await tester.pump(const Duration(milliseconds: 400));

      expect(find.text('Label brand'), findsOneWidget);
      expect(find.text('Not filterable'), findsNothing);

      // Seller type and sorting are always offered.
      await tester.tap(find.text(az.sellerStore));
      await tester.pump();
      await tester.tap(find.text(az.sortPriceAsc));
      await tester.pump();

      // Two options of one field + an option of another + a yes/no.
      await reveal(tester, find.text('Toyota'));
      await tester.tap(find.text('Toyota'));
      await tester.pump();
      await reveal(tester, find.text('BMW'));
      await tester.tap(find.text('BMW'));
      await tester.pump();
      await reveal(tester, find.text('Auto'));
      await tester.tap(find.text('Auto'));
      await tester.scrollUntilVisible(
        find.text(az.yes),
        200,
        scrollable: find
            .descendant(
              of: find.byType(ListView),
              matching: find.byType(Scrollable),
            )
            .first,
      );
      await tester.tap(find.text(az.yes));
      await tester.pump();

      final year = find.widgetWithText(TextField, az.priceMin).last;
      await tester.scrollUntilVisible(
        year,
        200,
        scrollable: find
            .descendant(
              of: find.byType(ListView),
              matching: find.byType(Scrollable),
            )
            .first,
      );
      await tester.enterText(year, '2020');
      await tester.pump();

      await tester.tap(find.text(az.applyAction));
      await tester.pump();
      await tester.pump(const Duration(milliseconds: 400));

      expect(result, isNotNull);
      expect(result!.fields['brand']!.optionIds, {'Toyota', 'BMW'});
      expect(result!.fields['gear']!.optionIds, {'Auto'});
      expect(result!.fields['year']!.min, 2020);
      expect(result!.fields['electric']!.flag, isTrue);
      expect(result!.seller, SellerType.store);
      expect(result!.sort, SearchSort.priceAsc);
      expect(find.text('Label year'), findsNothing); // sheet closed
      expect(tester.takeException(), isNull);
    });
  });
}
