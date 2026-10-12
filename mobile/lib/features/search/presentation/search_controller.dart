import 'package:flutter/foundation.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../domain/field_filter.dart';
import '../../locations/domain/location_models.dart';
import '../../catalog/domain/catalog_models.dart' show RentalPeriodUnit;
import '../domain/search_options.dart';

/// What the user is looking for. Immutable; every change creates a new value,
/// which makes the results provider reload.
class SearchFilters {
  const SearchFilters({
    this.query = '',
    this.categoryId,
    this.minPrice,
    this.maxPrice,
    this.fields = const {},
    this.seller = SellerType.all,
    this.sort = SearchSort.date,
    this.place = PlaceChoice.none,
    this.period,
  });

  final String query;
  final String? categoryId;
  final double? minPrice;
  final double? maxPrice;

  /// Conditions on the category's own fields (brand, year...), by field id.
  final Map<String, FieldFilterValue> fields;
  final SellerType seller;
  final SearchSort sort;

  /// City (and district) to look in; [PlaceChoice.none] = everywhere.
  final PlaceChoice place;

  /// Daily / monthly / ... only; `null` = any.
  final RentalPeriodUnit? period;

  bool get hasPrice => minPrice != null || maxPrice != null;

  /// Price or category-field conditions (what the filter sheet edits).
  bool get hasSheetFilters =>
      hasPrice ||
      fields.isNotEmpty ||
      seller != SellerType.all ||
      sort != SearchSort.date ||
      !place.isNone ||
      period != null;

  /// Category, price or fields chosen (the text query is not counted).
  bool get hasActiveFilters => categoryId != null || hasSheetFilters;

  bool get isEmpty => query.isEmpty && !hasActiveFilters;

  SearchFilters copyWith({
    String? query,
    String? categoryId,
    bool clearCategory = false,
    double? minPrice,
    double? maxPrice,
    bool clearPrice = false,
  }) => SearchFilters(
    query: query ?? this.query,
    categoryId: clearCategory ? null : (categoryId ?? this.categoryId),
    minPrice: clearPrice ? null : (minPrice ?? this.minPrice),
    maxPrice: clearPrice ? null : (maxPrice ?? this.maxPrice),
    // Fields belong to one category: changing the category drops them.
    fields:
        clearCategory || (categoryId != null && categoryId != this.categoryId)
        ? const {}
        : fields,
    seller: seller,
    sort: sort,
    place: place,
    period: period,
  );

  @override
  bool operator ==(Object other) =>
      other is SearchFilters &&
      other.query == query &&
      other.categoryId == categoryId &&
      other.minPrice == minPrice &&
      other.maxPrice == maxPrice &&
      other.seller == seller &&
      other.sort == sort &&
      other.place == place &&
      other.period == period &&
      mapEquals(other.fields, fields);

  @override
  int get hashCode => Object.hash(
    query,
    categoryId,
    minPrice,
    maxPrice,
    seller,
    sort,
    place,
    period,
    Object.hashAllUnordered(
      fields.entries.map((e) => Object.hash(e.key, e.value)),
    ),
  );
}

/// What a search page is opened with. It is the family argument of the search
/// providers, so each page instance gets its own (auto-disposed) state and no
/// provider ever has to be modified while the page is building.
class SearchSeed {
  const SearchSeed({this.query = ''});

  final String query;

  @override
  bool operator ==(Object other) => other is SearchSeed && other.query == query;

  @override
  int get hashCode => query.hashCode;
}

class SearchFiltersNotifier extends Notifier<SearchFilters> {
  SearchFiltersNotifier(this.seed);

  final SearchSeed seed;

  @override
  SearchFilters build() => SearchFilters(query: seed.query.trim());

  void setQuery(String query) => state = state.copyWith(query: query.trim());

  void setCategory(String? id) => state = id == null
      ? state.copyWith(clearCategory: true)
      : state.copyWith(categoryId: id);

  void setPrice({double? min, double? max}) => state = SearchFilters(
    query: state.query,
    categoryId: state.categoryId,
    minPrice: min,
    maxPrice: max,
    fields: state.fields,
    seller: state.seller,
    sort: state.sort,
    place: state.place,
    period: state.period,
  );

  /// What the filter sheet returned: price and category-field conditions.
  void setSheetFilters({
    double? min,
    double? max,
    Map<String, FieldFilterValue> fields = const {},
    SellerType seller = SellerType.all,
    SearchSort sort = SearchSort.date,
    PlaceChoice place = PlaceChoice.none,
    RentalPeriodUnit? period,
  }) => state = SearchFilters(
    query: state.query,
    categoryId: state.categoryId,
    minPrice: min,
    maxPrice: max,
    fields: {
      for (final e in fields.entries)
        if (!e.value.isEmpty) e.key: e.value,
    },
    seller: seller,
    sort: sort,
    place: place,
    period: period,
  );

  /// Drops category, price and fields but keeps what was typed.
  void clearFilters() =>
      state = state.copyWith(clearCategory: true, clearPrice: true);
}

final searchFiltersProvider = NotifierProvider.autoDispose
    .family<SearchFiltersNotifier, SearchFilters, SearchSeed>(
      SearchFiltersNotifier.new,
    );
