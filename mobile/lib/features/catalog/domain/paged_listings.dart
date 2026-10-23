import 'catalog_models.dart';

/// Accumulated state of a paginated listing feed (favourites, search...).
class PagedListings {
  const PagedListings({
    this.items = const [],
    this.page = 0,
    this.totalPages = 0,
    this.totalCount = 0,
    this.loadingMore = false,
  });

  final List<ListingSummary> items;
  final int page;
  final int totalPages;
  final int totalCount;
  final bool loadingMore;

  bool get hasMore => page < totalPages;

  factory PagedListings.first(Paged<ListingSummary> page) => PagedListings(
    items: page.items,
    page: page.page,
    totalPages: page.totalPages,
    totalCount: page.totalCount,
  );

  /// Appends [next] and clears the loading flag. A listing seen already is not added again, except a VIP one: VIP
  /// listings come round again on purpose as the feed goes on.
  PagedListings appended(Paged<ListingSummary> next) {
    final known = items.map((e) => e.id).toSet();
    return PagedListings(
      items: [
        ...items,
        ...next.items.where((e) => e.isVip || !known.contains(e.id)),
      ],
      page: next.page,
      totalPages: next.totalPages,
      totalCount: next.totalCount,
    );
  }

  PagedListings copyWith({bool? loadingMore}) => PagedListings(
    items: items,
    page: page,
    totalPages: totalPages,
    totalCount: totalCount,
    loadingMore: loadingMore ?? this.loadingMore,
  );
}
