import 'dart:ui';

import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/l10n/locale_controller.dart';
import '../../catalog/data/catalog_repository.dart';
import '../../catalog/domain/catalog_models.dart';
import '../../catalog/domain/paged_listings.dart';
import '../../search/domain/field_filter.dart';
import '../../search/presentation/filters_sheet.dart';
import '../../search/presentation/search_controller.dart';

/// The categories picked on the home page, top level first. Picking stays
/// on the home page: the strip shows what is inside the last pick and the
/// feed below follows it.
class HomeCategoryPath extends Notifier<List<String>> {
  @override
  List<String> build() => const [];

  /// Picks [id] at depth [level] (0 = top level), dropping deeper picks.
  void select(int level, String id) {
    state = [...state.take(level), id];
    _fieldsBelongToTheOldCategory();
  }

  /// Jumps straight to a whole chain (a search suggestion's category).
  void set(List<String> ids) {
    state = List.unmodifiable(ids);
    _fieldsBelongToTheOldCategory();
  }

  /// Un-picks everything from depth [level] down.
  void clearFrom(int level) {
    state = state.take(level).toList();
    _fieldsBelongToTheOldCategory();
  }

  // Conditions on a category's own fields (brand, rooms...) make no sense
  // once another category is shown.
  void _fieldsBelongToTheOldCategory() =>
      ref.read(homeFiltersProvider.notifier).dropFields();
}

final homeCategoryPathProvider =
    NotifierProvider<HomeCategoryPath, List<String>>(HomeCategoryPath.new);

/// What the home page's filter button chose: price, place, period, seller,
/// sorting and the category's own fields. (The category itself is the
/// [homeCategoryPathProvider].)
class HomeFilters extends Notifier<SearchFilters> {
  @override
  SearchFilters build() => const SearchFilters();

  void apply(SheetFilters r) => state = SearchFilters(
    minPrice: r.min,
    maxPrice: r.max,
    fields: {
      for (final e in r.fields.entries)
        if (!e.value.isEmpty) e.key: e.value,
    },
    seller: r.seller,
    sort: r.sort,
    place: r.place,
    period: r.period,
  );

  void dropFields() {
    if (state.fields.isEmpty) return;
    state = SearchFilters(
      minPrice: state.minPrice,
      maxPrice: state.maxPrice,
      seller: state.seller,
      sort: state.sort,
      place: state.place,
      period: state.period,
    );
  }
}

final homeFiltersProvider = NotifierProvider<HomeFilters, SearchFilters>(
  HomeFilters.new,
);

/// The words searched on the home page ("" = not searching). A search
/// result opens the home page with this set, so the feed, the category chain
/// and the filters all work the same for a search and for browsing.
class HomeQuery extends Notifier<String> {
  @override
  String build() => '';

  void set(String query) => state = query.trim();
}

final homeQueryProvider = NotifierProvider<HomeQuery, String>(HomeQuery.new);

/// The home feed: the picked category's listings, narrowed by the search
/// words and the filters, loaded page by page.
class HomeFeedController extends AsyncNotifier<PagedListings> {
  static const _pageSize = 24;

  Future<Paged<ListingSummary>> _fetch(int page) {
    final Locale locale = ref.read(localeProvider);
    final path = ref.read(homeCategoryPathProvider);
    final f = ref.read(homeFiltersProvider);
    return ref
        .read(catalogRepositoryProvider)
        .listings(
          language: locale.languageCode,
          categoryId: path.isEmpty ? null : path.last,
          search: ref.read(homeQueryProvider),
          minPrice: f.minPrice,
          maxPrice: f.maxPrice,
          fieldFilters: encodeFieldFilters(f.fields),
          seller: f.seller,
          sort: f.sort,
          cityId: f.place.city?.id,
          districtId: f.place.district?.id,
          rentalPeriod: f.period,
          page: page,
          pageSize: _pageSize,
        );
  }

  @override
  Future<PagedListings> build() async {
    ref.watch(localeProvider);
    ref.watch(homeCategoryPathProvider);
    ref.watch(homeFiltersProvider);
    ref.watch(homeQueryProvider);
    return PagedListings.first(await _fetch(1));
  }

  Future<void> loadMore() async {
    final current = state.value;
    if (current == null || !current.hasMore || current.loadingMore) return;
    state = AsyncData(current.copyWith(loadingMore: true));
    try {
      final next = await _fetch(current.page + 1);
      if (!ref.mounted) return;
      state = AsyncData(current.appended(next));
    } catch (_) {
      if (ref.mounted && state.value != null) {
        state = AsyncData(current.copyWith(loadingMore: false));
      }
    }
  }
}

final homeListingsProvider =
    AsyncNotifierProvider<HomeFeedController, PagedListings>(
      HomeFeedController.new,
    );
