import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/l10n/locale_controller.dart';
import '../../catalog/data/catalog_repository.dart';
import '../../catalog/domain/paged_listings.dart';

/// What the user is looking for. Immutable; every change creates a new value,
/// which makes the results provider reload.
class SearchFilters {
  const SearchFilters({
    this.query = '',
    this.categoryId,
    this.minPrice,
    this.maxPrice,
  });

  final String query;
  final String? categoryId;
  final double? minPrice;
  final double? maxPrice;

  bool get hasPrice => minPrice != null || maxPrice != null;

  /// Category or price chosen (the text query is not counted).
  bool get hasActiveFilters => categoryId != null || hasPrice;

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
  );

  @override
  bool operator ==(Object other) =>
      other is SearchFilters &&
      other.query == query &&
      other.categoryId == categoryId &&
      other.minPrice == minPrice &&
      other.maxPrice == maxPrice;

  @override
  int get hashCode => Object.hash(query, categoryId, minPrice, maxPrice);
}

/// What a search page is opened with. It is the family argument of the search
/// providers, so each page instance gets its own (auto-disposed) state and no
/// provider ever has to be modified while the page is building.
class SearchSeed {
  const SearchSeed({this.query = '', this.categoryId});

  final String query;
  final String? categoryId;

  @override
  bool operator ==(Object other) =>
      other is SearchSeed &&
      other.query == query &&
      other.categoryId == categoryId;

  @override
  int get hashCode => Object.hash(query, categoryId);
}

class SearchFiltersNotifier extends Notifier<SearchFilters> {
  SearchFiltersNotifier(this.seed);

  final SearchSeed seed;

  @override
  SearchFilters build() =>
      SearchFilters(query: seed.query.trim(), categoryId: seed.categoryId);

  void setQuery(String query) => state = state.copyWith(query: query.trim());

  void setCategory(String? id) => state = id == null
      ? state.copyWith(clearCategory: true)
      : state.copyWith(categoryId: id);

  void setPrice({double? min, double? max}) =>
      state = (min == null && max == null)
      ? state.copyWith(clearPrice: true)
      : SearchFilters(
          query: state.query,
          categoryId: state.categoryId,
          minPrice: min,
          maxPrice: max,
        );

  /// Drops category and price but keeps what was typed.
  void clearFilters() =>
      state = state.copyWith(clearCategory: true, clearPrice: true);
}

final searchFiltersProvider = NotifierProvider.autoDispose
    .family<SearchFiltersNotifier, SearchFilters, SearchSeed>(
      SearchFiltersNotifier.new,
    );

/// Listings matching the current [SearchFilters], loaded page by page.
class SearchResultsController extends AsyncNotifier<PagedListings> {
  SearchResultsController(this.seed);

  final SearchSeed seed;

  static const _pageSize = 20;

  Future<PagedListings> _fetch(SearchFilters f, {int page = 1}) async {
    final result = await ref
        .read(catalogRepositoryProvider)
        .listings(
          language: ref.read(localeProvider).languageCode,
          categoryId: f.categoryId,
          search: f.query,
          minPrice: f.minPrice,
          maxPrice: f.maxPrice,
          page: page,
          pageSize: _pageSize,
        );
    return PagedListings.first(result);
  }

  @override
  Future<PagedListings> build() {
    final filters = ref.watch(searchFiltersProvider(seed));
    ref.watch(localeProvider);
    return _fetch(filters);
  }

  Future<void> loadMore() async {
    final current = state.value;
    if (current == null || !current.hasMore || current.loadingMore) return;

    final filters = ref.read(searchFiltersProvider(seed));
    state = AsyncData(current.copyWith(loadingMore: true));
    try {
      final next = await ref
          .read(catalogRepositoryProvider)
          .listings(
            language: ref.read(localeProvider).languageCode,
            categoryId: filters.categoryId,
            search: filters.query,
            minPrice: filters.minPrice,
            maxPrice: filters.maxPrice,
            page: current.page + 1,
            pageSize: _pageSize,
          );
      // Ignore the answer if the filters changed while it was in flight.
      if (ref.read(searchFiltersProvider(seed)) != filters) return;
      state = AsyncData(current.appended(next));
    } catch (_) {
      if (state.value != null) {
        state = AsyncData(current.copyWith(loadingMore: false));
      }
    }
  }
}

final searchResultsProvider = AsyncNotifierProvider.autoDispose
    .family<SearchResultsController, PagedListings, SearchSeed>(
      SearchResultsController.new,
    );
