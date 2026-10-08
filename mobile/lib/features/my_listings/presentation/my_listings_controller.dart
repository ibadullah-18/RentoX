import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../auth/presentation/auth_controller.dart';
import '../data/my_listings_repository.dart';
import '../domain/owned_listing_models.dart';

class PagedOwned {
  const PagedOwned({
    this.items = const [],
    this.page = 0,
    this.totalPages = 0,
    this.totalCount = 0,
    this.loadingMore = false,
  });

  final List<OwnedListingSummary> items;
  final int page;
  final int totalPages;
  final int totalCount;
  final bool loadingMore;

  bool get hasMore => page < totalPages;

  PagedOwned appended(
    List<OwnedListingSummary> more,
    int page,
    int totalPages,
    int totalCount,
  ) {
    final known = items.map((e) => e.id).toSet();
    return PagedOwned(
      items: [...items, ...more.where((e) => !known.contains(e.id))],
      page: page,
      totalPages: totalPages,
      totalCount: totalCount,
    );
  }

  PagedOwned copyWith({bool? loadingMore}) => PagedOwned(
    items: items,
    page: page,
    totalPages: totalPages,
    totalCount: totalCount,
    loadingMore: loadingMore ?? this.loadingMore,
  );
}

/// The signed-in user's own listings, newest first, loaded page by page.
class MyListingsController extends AsyncNotifier<PagedOwned> {
  static const _pageSize = 20;

  @override
  Future<PagedOwned> build() async {
    if (ref.watch(authControllerProvider).value == null) {
      return const PagedOwned();
    }
    final page = await ref
        .read(myListingsRepositoryProvider)
        .mine(pageSize: _pageSize);
    return PagedOwned(
      items: page.items,
      page: page.page,
      totalPages: page.totalPages,
      totalCount: page.totalCount,
    );
  }

  Future<void> loadMore() async {
    final current = state.value;
    if (current == null || !current.hasMore || current.loadingMore) return;

    state = AsyncData(current.copyWith(loadingMore: true));
    try {
      final next = await ref
          .read(myListingsRepositoryProvider)
          .mine(page: current.page + 1, pageSize: _pageSize);
      state = AsyncData(
        current.appended(
          next.items,
          next.page,
          next.totalPages,
          next.totalCount,
        ),
      );
    } catch (_) {
      state = AsyncData(current.copyWith(loadingMore: false));
    }
  }
}

final myListingsProvider =
    AsyncNotifierProvider<MyListingsController, PagedOwned>(
      MyListingsController.new,
    );
