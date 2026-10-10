import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/l10n/locale_controller.dart';
import '../../auth/presentation/auth_controller.dart';
import '../../catalog/domain/paged_listings.dart';
import '../../listing_create/domain/photo.dart';
import '../data/store_repository.dart';
import '../domain/store_models.dart';

enum StoreImageProblem { tooLarge }

/// The signed-in user's own store (null when they don't have one).
class MyStoreController extends AsyncNotifier<MyStore?> {
  StoreRepository get _repo => ref.read(storeRepositoryProvider);

  @override
  Future<MyStore?> build() async {
    if (ref.watch(authControllerProvider).value == null) return null;
    return _repo.mine();
  }

  /// Opens a store. Throws on failure (the form shows it).
  Future<void> create(StoreForm form) async {
    await _repo.create(form);
    state = AsyncData(await _repo.mine());
  }

  Future<void> save(StoreForm form) async {
    final before = state.value;
    await _repo.update(form);
    final fresh = await _repo.mine();
    state = AsyncData(fresh?.withImageVersion(before?.imageVersion ?? 0));
  }

  Future<void> submit() async {
    await _repo.submit();
    final before = state.value;
    final fresh = await _repo.mine();
    state = AsyncData(fresh?.withImageVersion(before?.imageVersion ?? 0));
  }

  /// Uploads a logo/cover. Returns a problem when the file is too big.
  Future<StoreImageProblem?> setImage(
    StoreImageKind kind,
    PickedPhoto photo,
  ) async {
    final max = kind == StoreImageKind.logo
        ? StoreRules.logoMaxBytes
        : StoreRules.coverMaxBytes;
    if (photo.sizeBytes > max) return StoreImageProblem.tooLarge;
    final before = state.value;
    await _repo.uploadImage(kind, photo);
    final fresh = await _repo.mine();
    state = AsyncData(fresh?.withImageVersion((before?.imageVersion ?? 0) + 1));
    return null;
  }

  Future<void> removeImage(StoreImageKind kind) async {
    final before = state.value;
    await _repo.deleteImage(kind);
    final fresh = await _repo.mine();
    state = AsyncData(fresh?.withImageVersion((before?.imageVersion ?? 0) + 1));
  }
}

final myStoreProvider = AsyncNotifierProvider<MyStoreController, MyStore?>(
  MyStoreController.new,
);

/// A live store by its slug (public).
final publicStoreProvider = FutureProvider.autoDispose
    .family<PublicStore, String>(
      (ref, slug) => ref.watch(storeRepositoryProvider).bySlug(slug),
    );

/// The listings of a store, loaded page by page.
class StoreListingsController extends AsyncNotifier<PagedListings> {
  StoreListingsController(this.slug);

  final String slug;
  static const _pageSize = 20;

  String get _language => ref.read(localeProvider).languageCode;

  @override
  Future<PagedListings> build() async {
    ref.watch(localeProvider);
    final page = await ref
        .read(storeRepositoryProvider)
        .listings(slug, language: _language, pageSize: _pageSize);
    return PagedListings.first(page);
  }

  Future<void> loadMore() async {
    final current = state.value;
    if (current == null || !current.hasMore || current.loadingMore) return;
    state = AsyncData(current.copyWith(loadingMore: true));
    try {
      final next = await ref
          .read(storeRepositoryProvider)
          .listings(
            slug,
            language: _language,
            page: current.page + 1,
            pageSize: _pageSize,
          );
      state = AsyncData(current.appended(next));
    } catch (_) {
      state = AsyncData(current.copyWith(loadingMore: false));
    }
  }
}

final storeListingsProvider = AsyncNotifierProvider.autoDispose
    .family<StoreListingsController, PagedListings, String>(
      StoreListingsController.new,
    );

/// Whether the signed-in user follows a store, with the follower count.
/// Toggling is optimistic and rolls back (rethrowing) when the server refuses.
class FollowController extends AsyncNotifier<FollowStatus> {
  FollowController(this.storeId);

  final String storeId;

  @override
  Future<FollowStatus> build() =>
      ref.watch(storeRepositoryProvider).followStatus(storeId);

  Future<void> toggle() async {
    final current = state.value;
    if (current == null) return;
    final repo = ref.read(storeRepositoryProvider);
    state = AsyncData(current.toggled());
    try {
      if (current.isFollowing) {
        await repo.unfollow(storeId);
      } else {
        await repo.follow(storeId);
      }
      ref.invalidate(followedStoresProvider);
    } catch (_) {
      state = AsyncData(current);
      rethrow;
    }
  }
}

final followProvider = AsyncNotifierProvider.autoDispose
    .family<FollowController, FollowStatus, String>(FollowController.new);

/// Stores the user follows.
final followedStoresProvider = FutureProvider.autoDispose<List<FollowedStore>>((
  ref,
) async {
  if (ref.watch(authControllerProvider).value == null) return const [];
  final page = await ref.watch(storeRepositoryProvider).following(pageSize: 50);
  return page.items;
});

/// A few stores for the home page strip (most active first).
final homeStoresProvider = FutureProvider.autoDispose<List<FollowedStore>>((
  ref,
) async {
  final page = await ref.watch(storeRepositoryProvider).search(pageSize: 10);
  return page.items;
});
