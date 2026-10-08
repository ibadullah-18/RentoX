import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/l10n/locale_controller.dart';
import '../../auth/presentation/auth_controller.dart';
import '../../catalog/domain/paged_listings.dart';
import '../data/favorites_repository.dart';

/// Optimistic, per-listing favourite state.
///
/// Every heart in the app (home, favourites, later search/details) reads and
/// writes through this one place, so they can never disagree. Values the user
/// has changed live in the map; everything else falls back to what the server
/// sent with the listing. The map resets when the account changes.
class FavoriteOverrides extends Notifier<Map<String, bool>> {
  @override
  Map<String, bool> build() {
    ref.watch(authControllerProvider.select((a) => a.value?.userId));
    return const {};
  }

  /// Flips the favourite flag of [listing]. Rolls back and rethrows on failure
  /// so the caller can tell the user.
  Future<void> toggle(String listingId, {required bool current}) async {
    final next = !current;
    state = {...state, listingId: next};
    final repo = ref.read(favoritesRepositoryProvider);
    try {
      if (next) {
        await repo.add(listingId);
      } else {
        await repo.remove(listingId);
      }
      ref.invalidate(favoritesProvider);
    } catch (_) {
      state = {...state, listingId: current};
      rethrow;
    }
  }
}

final favoriteOverridesProvider =
    NotifierProvider<FavoriteOverrides, Map<String, bool>>(
      FavoriteOverrides.new,
    );

/// Whether a listing is currently a favourite. Keyed by `(id, serverValue)`.
final isFavoriteProvider = Provider.family<bool, (String, bool)>((ref, key) {
  final (id, serverValue) = key;
  return ref.watch(favoriteOverridesProvider.select((m) => m[id])) ??
      serverValue;
});

/// The signed-in user's favourites, loaded page by page.
class FavoritesController extends AsyncNotifier<PagedListings> {
  static const _pageSize = 20;

  String get _language => ref.read(localeProvider).languageCode;

  @override
  Future<PagedListings> build() async {
    final session = ref.watch(authControllerProvider).value;
    ref.watch(localeProvider);
    if (session == null) return const PagedListings();

    final page = await ref
        .read(favoritesRepositoryProvider)
        .list(language: _language, pageSize: _pageSize);
    return PagedListings.first(page);
  }

  /// Appends the next page. Safe to call repeatedly (e.g. from a scroll
  /// listener): it ignores calls while loading or when everything is loaded.
  Future<void> loadMore() async {
    final current = state.value;
    if (current == null || !current.hasMore || current.loadingMore) return;

    state = AsyncData(current.copyWith(loadingMore: true));
    try {
      final next = await ref
          .read(favoritesRepositoryProvider)
          .list(
            language: _language,
            page: current.page + 1,
            pageSize: _pageSize,
          );
      state = AsyncData(current.appended(next));
    } catch (_) {
      // Keep what we have; the next scroll will retry.
      state = AsyncData(current.copyWith(loadingMore: false));
    }
  }
}

final favoritesProvider =
    AsyncNotifierProvider<FavoritesController, PagedListings>(
      FavoritesController.new,
    );
