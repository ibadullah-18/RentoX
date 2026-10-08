import 'package:dio/dio.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:rentox/features/auth/domain/auth_models.dart';
import 'package:rentox/features/auth/presentation/auth_controller.dart';
import 'package:rentox/features/catalog/domain/catalog_models.dart';
import 'package:rentox/features/favorites/data/favorites_repository.dart';
import 'package:rentox/features/favorites/presentation/favorites_controller.dart';

ListingSummary listing(String id, {bool favorite = false}) => ListingSummary(
  id: id,
  title: 'Listing $id',
  price: 10,
  currency: 'AZN',
  unit: RentalPeriodUnit.day,
  isVip: false,
  isFavorite: favorite,
  viewCount: 0,
  favoriteCount: 0,
);

class FakeAuth extends AuthController {
  @override
  Future<AuthSession?> build() async =>
      const AuthSession(userId: 'u1', phoneNumber: '+994501234567');
}

class FakeRepo extends FavoritesRepository {
  FakeRepo({this.failWrites = false}) : super(Dio());

  final bool failWrites;
  final added = <String>[];
  final removed = <String>[];
  final pagesRequested = <int>[];

  /// 3 pages: 2 + 2 + 1 items; page 2 repeats an id from page 1 on purpose.
  static final _pages = {
    1: [listing('a', favorite: true), listing('b', favorite: true)],
    2: [listing('b', favorite: true), listing('c', favorite: true)],
    3: [listing('d', favorite: true)],
  };

  @override
  Future<Paged<ListingSummary>> list({
    required String language,
    int page = 1,
    int pageSize = 20,
  }) async {
    pagesRequested.add(page);
    return Paged(
      items: _pages[page]!,
      page: page,
      totalPages: 3,
      totalCount: 5,
    );
  }

  @override
  Future<void> add(String listingId) async {
    if (failWrites) throw Exception('offline');
    added.add(listingId);
  }

  @override
  Future<void> remove(String listingId) async {
    if (failWrites) throw Exception('offline');
    removed.add(listingId);
  }
}

ProviderContainer containerWith(FakeRepo repo) {
  final container = ProviderContainer(
    overrides: [
      authControllerProvider.overrideWith(FakeAuth.new),
      favoritesRepositoryProvider.overrideWithValue(repo),
    ],
  );
  addTearDown(container.dispose);
  return container;
}

void main() {
  group('FavoriteOverrides', () {
    test('falls back to the server value until the user toggles', () async {
      final c = containerWith(FakeRepo());
      await c.read(authControllerProvider.future);

      expect(c.read(isFavoriteProvider(('x', false))), isFalse);
      expect(c.read(isFavoriteProvider(('y', true))), isTrue);
    });

    test('toggle is optimistic and calls the right endpoint', () async {
      final repo = FakeRepo();
      final c = containerWith(repo);
      await c.read(authControllerProvider.future);

      final pending = c
          .read(favoriteOverridesProvider.notifier)
          .toggle('x', current: false);
      // Flipped immediately, before the request completes.
      expect(c.read(isFavoriteProvider(('x', false))), isTrue);
      await pending;

      expect(repo.added, ['x']);
      expect(repo.removed, isEmpty);

      await c
          .read(favoriteOverridesProvider.notifier)
          .toggle('x', current: true);
      expect(repo.removed, ['x']);
      expect(c.read(isFavoriteProvider(('x', false))), isFalse);
    });

    test('rolls back and rethrows when the request fails', () async {
      final c = containerWith(FakeRepo(failWrites: true));
      await c.read(authControllerProvider.future);

      await expectLater(
        c.read(favoriteOverridesProvider.notifier).toggle('x', current: false),
        throwsException,
      );
      expect(c.read(isFavoriteProvider(('x', false))), isFalse);
    });
  });

  group('FavoritesController paging', () {
    test('loads page 1, appends later pages without duplicates', () async {
      final repo = FakeRepo();
      final c = containerWith(repo);
      await c.read(authControllerProvider.future);

      var state = await c.read(favoritesProvider.future);
      expect(state.items.map((e) => e.id), ['a', 'b']);
      expect(state.hasMore, isTrue);

      await c.read(favoritesProvider.notifier).loadMore();
      state = c.read(favoritesProvider).requireValue;
      expect(state.items.map((e) => e.id), ['a', 'b', 'c']); // 'b' deduped

      await c.read(favoritesProvider.notifier).loadMore();
      state = c.read(favoritesProvider).requireValue;
      expect(state.items.map((e) => e.id), ['a', 'b', 'c', 'd']);
      expect(state.hasMore, isFalse);

      // Nothing left: no extra request.
      await c.read(favoritesProvider.notifier).loadMore();
      expect(repo.pagesRequested, [1, 2, 3]);
    });
  });
}
