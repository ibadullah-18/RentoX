import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../catalog/domain/catalog_models.dart';
import '../../store/data/store_repository.dart';
import '../../store/domain/store_models.dart';

/// Accumulated state of the store search, loaded page by page.
class PagedStores {
  const PagedStores({
    this.items = const [],
    this.page = 0,
    this.totalPages = 0,
    this.totalCount = 0,
    this.loadingMore = false,
  });

  final List<FollowedStore> items;
  final int page;
  final int totalPages;
  final int totalCount;
  final bool loadingMore;

  bool get hasMore => page < totalPages;

  factory PagedStores.first(Paged<FollowedStore> p) => PagedStores(
    items: p.items,
    page: p.page,
    totalPages: p.totalPages,
    totalCount: p.totalCount,
  );

  PagedStores appended(Paged<FollowedStore> next) {
    final known = items.map((e) => e.id).toSet();
    return PagedStores(
      items: [...items, ...next.items.where((e) => !known.contains(e.id))],
      page: next.page,
      totalPages: next.totalPages,
      totalCount: next.totalCount,
    );
  }

  PagedStores copyWith({bool? loadingMore}) => PagedStores(
    items: items,
    page: page,
    totalPages: totalPages,
    totalCount: totalCount,
    loadingMore: loadingMore ?? this.loadingMore,
  );
}

/// Stores matching the text the user typed (all active stores when empty).
class StoreSearchController extends AsyncNotifier<PagedStores> {
  StoreSearchController(this.query);

  final String query;

  static const _pageSize = 20;

  @override
  Future<PagedStores> build() async {
    final result = await ref
        .read(storeRepositoryProvider)
        .search(search: query, pageSize: _pageSize);
    return PagedStores.first(result);
  }

  Future<void> loadMore() async {
    final current = state.value;
    if (current == null || !current.hasMore || current.loadingMore) return;

    state = AsyncData(current.copyWith(loadingMore: true));
    try {
      final next = await ref
          .read(storeRepositoryProvider)
          .search(search: query, page: current.page + 1, pageSize: _pageSize);
      state = AsyncData(current.appended(next));
    } catch (_) {
      if (state.value != null) {
        state = AsyncData(current.copyWith(loadingMore: false));
      }
    }
  }
}

final storeSearchProvider = AsyncNotifierProvider.autoDispose
    .family<StoreSearchController, PagedStores, String>(
      StoreSearchController.new,
    );
