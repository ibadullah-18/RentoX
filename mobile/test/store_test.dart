import 'dart:typed_data';

import 'package:dio/dio.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:go_router/go_router.dart';
import 'package:rentox/core/l10n/app_localizations.dart';
import 'package:rentox/features/auth/domain/auth_models.dart';
import 'package:rentox/features/auth/presentation/auth_controller.dart';
import 'package:rentox/features/catalog/domain/catalog_models.dart';
import 'package:rentox/features/listing_create/domain/photo.dart';
import 'package:rentox/features/store/data/store_repository.dart';
import 'package:rentox/features/store/domain/store_models.dart';
import 'package:rentox/features/store/presentation/my_store_page.dart';
import 'package:rentox/features/store/presentation/store_controller.dart';
import 'package:rentox/features/store/presentation/store_form_page.dart';
import 'package:rentox/features/store/presentation/store_page.dart';

class _SignedIn extends AuthController {
  @override
  Future<AuthSession?> build() async =>
      const AuthSession(userId: 'u1', phoneNumber: '+994501234567');
}

MyStore store({
  StoreStatus status = StoreStatus.draft,
  String? logo,
  String? reason,
}) => MyStore(
  id: 's1',
  name: 'Rent Baku',
  slug: 'rent-baku',
  description: 'Maşın və texnika kirayəsi',
  phone: '+994501234567',
  status: status,
  logoPath: logo,
  rejectionReason: reason,
);

class FakeStores extends StoreRepository {
  FakeStores() : super(Dio());

  MyStore? mineValue;
  final calls = <String>[];
  StoreForm? lastForm;
  bool failFollow = false;
  FollowStatus follow0 = const FollowStatus(
    isFollowing: false,
    followerCount: 3,
  );

  @override
  Future<MyStore?> mine() async => mineValue;

  @override
  Future<void> create(StoreForm form) async {
    calls.add('create');
    lastForm = form;
    mineValue = store();
  }

  @override
  Future<void> update(StoreForm form) async {
    calls.add('update');
    lastForm = form;
  }

  @override
  Future<void> submit() async {
    calls.add('submit');
    mineValue = store(status: StoreStatus.pendingReview, logo: '/logo');
  }

  @override
  Future<void> uploadImage(StoreImageKind kind, PickedPhoto photo) async {
    calls.add('upload-${kind.name}');
    mineValue = store(logo: '/api/stores/s1/logo');
  }

  @override
  Future<void> deleteImage(StoreImageKind kind) async {
    calls.add('delete-${kind.name}');
    mineValue = store();
  }

  @override
  Future<PublicStore> bySlug(String slug) async => const PublicStore(
    id: 'other',
    name: 'Auto Rent',
    slug: 'auto-rent',
    description: 'Premium cars',
    phone: '+994551112233',
    website: 'https://autorent.az',
    instagram: 'https://instagram.com/autorent',
    activeListingCount: 4,
    totalViewCount: 120,
  );

  @override
  Future<Paged<ListingSummary>> listings(
    String slug, {
    required String language,
    int page = 1,
    int pageSize = 20,
  }) async => const Paged(items: [], page: 1, totalPages: 1, totalCount: 0);

  @override
  Future<FollowStatus> followStatus(String storeId) async => follow0;

  @override
  Future<void> follow(String storeId) async {
    calls.add('follow');
    if (failFollow) throw Exception('boom');
  }

  @override
  Future<void> unfollow(String storeId) async {
    calls.add('unfollow');
    if (failFollow) throw Exception('boom');
  }

  @override
  Future<Paged<FollowedStore>> following({
    int page = 1,
    int pageSize = 20,
  }) async => const Paged(items: [], page: 1, totalPages: 1, totalCount: 0);
}

ProviderContainer makeContainer(FakeStores repo) => ProviderContainer(
  overrides: [
    authControllerProvider.overrideWith(_SignedIn.new),
    storeRepositoryProvider.overrideWithValue(repo),
  ],
);

Widget app(ProviderContainer c, Widget home) => UncontrolledProviderScope(
  container: c,
  child: MaterialApp.router(
    locale: const Locale('az'),
    localizationsDelegates: AppL10n.localizationsDelegates,
    supportedLocales: AppL10n.supportedLocales,
    routerConfig: GoRouter(
      routes: [
        GoRoute(path: '/', builder: (_, _) => home),
        GoRoute(
          path: '/store/mine/edit',
          builder: (_, _) => const Scaffold(body: Text('FORM PAGE')),
        ),
        GoRoute(
          path: '/store/:slug',
          builder: (_, _) => const Scaffold(body: Text('PUBLIC PAGE')),
        ),
      ],
    ),
  ),
);

void tall(WidgetTester t) {
  t.view.physicalSize = const Size(1080, 3200);
  t.view.devicePixelRatio = 3;
  addTearDown(t.view.reset);
}

void main() {
  final az = lookupAppL10n(const Locale('az'));

  group('StoreForm validation (mirrors the backend)', () {
    const ok = StoreForm(
      name: 'Rent Baku',
      description: 'Maşın və texnika kirayəsi',
      phone: '+994501234567',
    );

    test('a minimal valid form passes', () {
      expect(ok.validate(), isEmpty);
    });

    test('required fields and their limits', () {
      final e = const StoreForm(
        name: 'A',
        description: 'qısa',
        phone: '123',
      ).validate();
      expect(e[StoreField.name], StoreIssue.tooShort);
      expect(e[StoreField.description], StoreIssue.tooShort);
      expect(e[StoreField.phone], StoreIssue.tooShort);
      expect(
        const StoreForm().validate()[StoreField.name],
        StoreIssue.required,
      );
      expect(
        StoreForm(
          name: 'x' * 101,
          description: ok.description,
          phone: ok.phone,
        ).validate()[StoreField.name],
        StoreIssue.tooLong,
      );
    });

    test('optional email and links are checked only when filled in', () {
      expect(
        StoreForm(
          name: ok.name,
          description: ok.description,
          phone: ok.phone,
          email: 'nope',
          website: 'autorent.az',
          instagram: 'https://instagram.com/x',
        ).validate(),
        {
          StoreField.email: StoreIssue.invalidEmail,
          StoreField.website: StoreIssue.invalidUrl,
        },
      );
    });

    test('empty optional fields are sent as null', () {
      final json = ok.toJson();
      expect(json['email'], isNull);
      expect(json['instagramUrl'], isNull);
      expect(json['name'], 'Rent Baku');
    });
  });

  test('only drafts and rejected stores can be edited or submitted', () {
    for (final s in StoreStatus.values) {
      expect(
        s.canEdit,
        s == StoreStatus.draft || s == StoreStatus.rejected,
        reason: '$s',
      );
    }
  });

  group('MyStoreController', () {
    test('no store yet -> null; create reloads the store', () async {
      final repo = FakeStores();
      final c = makeContainer(repo);
      addTearDown(c.dispose);
      c.listen(myStoreProvider, (_, _) {});
      expect(await c.read(myStoreProvider.future), isNull);

      await c
          .read(myStoreProvider.notifier)
          .create(
            const StoreForm(
              name: 'Rent Baku',
              description: 'Maşın və texnika kirayəsi',
              phone: '+994501234567',
            ),
          );
      expect(c.read(myStoreProvider).value?.name, 'Rent Baku');
      expect(repo.calls, ['create']);
    });

    test(
      'logo upload bumps the cache version; oversized logo is refused',
      () async {
        final repo = FakeStores()..mineValue = store();
        final c = makeContainer(repo);
        addTearDown(c.dispose);
        c.listen(myStoreProvider, (_, _) {});
        await c.read(myStoreProvider.future);
        final n = c.read(myStoreProvider.notifier);

        PickedPhoto photo(int bytes) => PickedPhoto(
          name: 'l.jpg',
          bytes: Uint8List(bytes),
          mimeType: 'image/jpeg',
        );

        expect(
          await n.setImage(
            StoreImageKind.logo,
            photo(StoreRules.logoMaxBytes + 1),
          ),
          StoreImageProblem.tooLarge,
        );
        expect(repo.calls, isEmpty);

        // The same size is fine for a cover (limit 10 MB).
        expect(
          await n.setImage(
            StoreImageKind.cover,
            photo(StoreRules.logoMaxBytes + 1),
          ),
          isNull,
        );
        final s = c.read(myStoreProvider).requireValue!;
        expect(s.imageVersion, 1);
        expect(s.logoUrl, endsWith('?v=1'));

        await n.removeImage(StoreImageKind.logo);
        expect(c.read(myStoreProvider).requireValue!.hasLogo, isFalse);
      },
    );
  });

  group('FollowController', () {
    test('toggle is optimistic and updates the count', () async {
      final repo = FakeStores();
      final c = makeContainer(repo);
      addTearDown(c.dispose);
      c.listen(followProvider('other'), (_, _) {});
      await c.read(followProvider('other').future);

      await c.read(followProvider('other').notifier).toggle();
      var s = c.read(followProvider('other')).requireValue;
      expect(s.isFollowing, isTrue);
      expect(s.followerCount, 4);

      await c.read(followProvider('other').notifier).toggle();
      s = c.read(followProvider('other')).requireValue;
      expect(s.isFollowing, isFalse);
      expect(s.followerCount, 3);
      expect(repo.calls, ['follow', 'unfollow']);
    });

    test('a failed request rolls back and rethrows', () async {
      final repo = FakeStores()..failFollow = true;
      final c = makeContainer(repo);
      addTearDown(c.dispose);
      c.listen(followProvider('other'), (_, _) {});
      await c.read(followProvider('other').future);

      await expectLater(
        c.read(followProvider('other').notifier).toggle(),
        throwsException,
      );
      final s = c.read(followProvider('other')).requireValue;
      expect(s.isFollowing, isFalse);
      expect(s.followerCount, 3);
    });
  });

  testWidgets('no store: invites the user to open one', (tester) async {
    tall(tester);
    final c = makeContainer(FakeStores());
    addTearDown(c.dispose);
    await tester.pumpWidget(app(c, const MyStorePage()));
    await tester.pumpAndSettle();

    expect(find.text(az.storeNone), findsOneWidget);
    await tester.tap(find.text(az.storeOpen));
    await tester.pumpAndSettle();
    expect(find.text('FORM PAGE'), findsOneWidget);
  });

  testWidgets('draft without logo cannot be submitted', (tester) async {
    tall(tester);
    final repo = FakeStores()..mineValue = store();
    final c = makeContainer(repo);
    addTearDown(c.dispose);
    await tester.pumpWidget(app(c, const MyStorePage()));
    await tester.pumpAndSettle();

    expect(find.text(az.storeSubmitNeedsLogo), findsOneWidget);
    final submit = tester.widget<FilledButton>(
      find.widgetWithText(FilledButton, az.submitForReview),
    );
    expect(submit.onPressed, isNull);
  });

  testWidgets('draft with logo: submit sends it for review', (tester) async {
    tall(tester);
    final repo = FakeStores()..mineValue = store(logo: '/api/stores/s1/logo');
    final c = makeContainer(repo);
    addTearDown(c.dispose);
    await tester.pumpWidget(app(c, const MyStorePage()));
    await tester.pumpAndSettle();

    await tester.tap(find.widgetWithText(FilledButton, az.submitForReview));
    await tester.pumpAndSettle();
    expect(repo.calls, ['submit']);
    expect(find.text(az.storePendingInfo), findsOneWidget);
    // While in review nothing can be edited or sent again.
    expect(find.text(az.editAction), findsNothing);
    expect(find.text(az.submitForReview), findsNothing);
  });

  testWidgets('rejected store shows the reason and can be edited', (
    tester,
  ) async {
    tall(tester);
    final repo = FakeStores()
      ..mineValue = store(
        status: StoreStatus.rejected,
        logo: '/l',
        reason: 'Logo aydın deyil',
      );
    final c = makeContainer(repo);
    addTearDown(c.dispose);
    await tester.pumpWidget(app(c, const MyStorePage()));
    await tester.pumpAndSettle();

    expect(find.text('Logo aydın deyil'), findsOneWidget);
    expect(find.text(az.editAction), findsOneWidget);
  });

  testWidgets('active store offers "view store"', (tester) async {
    tall(tester);
    final repo = FakeStores()
      ..mineValue = store(status: StoreStatus.active, logo: '/l');
    final c = makeContainer(repo);
    addTearDown(c.dispose);
    await tester.pumpWidget(app(c, const MyStorePage()));
    await tester.pumpAndSettle();

    expect(find.text(az.editAction), findsNothing);
    await tester.tap(find.text(az.storeView));
    await tester.pumpAndSettle();
    expect(find.text('PUBLIC PAGE'), findsOneWidget);
  });

  testWidgets('form shows errors and sends nothing until valid', (
    tester,
  ) async {
    tall(tester);
    final repo = FakeStores();
    final c = makeContainer(repo);
    addTearDown(c.dispose);
    await tester.pumpWidget(app(c, const StoreFormPage()));
    await tester.pumpAndSettle();

    await tester.tap(find.text(az.storeSave));
    await tester.pump();
    expect(find.text(az.fieldRequired), findsWidgets);
    expect(repo.calls, isEmpty);

    final fields = find.byType(TextField);
    await tester.enterText(fields.at(0), 'Rent Baku');
    await tester.enterText(fields.at(1), 'Maşın və texnika kirayəsi');
    await tester.enterText(fields.at(2), '+994501234567');
    await tester.pump();
    await tester.tap(find.text(az.storeSave));
    await tester.pumpAndSettle();

    expect(repo.calls, ['create']);
    expect(repo.lastForm!.name, 'Rent Baku');
  });

  testWidgets('public page: details, contacts, follow', (tester) async {
    tall(tester);
    final repo = FakeStores();
    final c = makeContainer(repo);
    addTearDown(c.dispose);
    await tester.pumpWidget(app(c, const StorePage(slug: 'auto-rent')));
    await tester.pumpAndSettle();

    expect(find.text('Auto Rent'), findsOneWidget);
    expect(find.text('Premium cars'), findsOneWidget);
    expect(find.text('+994551112233'), findsOneWidget);
    expect(find.text('Website'), findsOneWidget);
    expect(find.text('Instagram'), findsOneWidget);
    expect(find.text(az.storeNoListings), findsOneWidget);

    await tester.tap(find.text(az.storeFollow));
    await tester.pumpAndSettle();
    expect(repo.calls, ['follow']);
    expect(find.text(az.storeFollowingNow), findsOneWidget);
  });

  testWidgets('own store has no follow button', (tester) async {
    tall(tester);
    // The user's store id is 'other', same as the public one being viewed.
    final repo = FakeStores()
      ..mineValue = MyStore(
        id: 'other',
        name: 'Auto Rent',
        slug: 'auto-rent',
        description: 'x' * 20,
        phone: '+994551112233',
        status: StoreStatus.active,
      );
    final c = makeContainer(repo);
    addTearDown(c.dispose);
    await c.read(authControllerProvider.future);
    c.listen(myStoreProvider, (_, _) {});
    await c.read(myStoreProvider.future);
    await tester.pumpWidget(app(c, const StorePage(slug: 'auto-rent')));
    await tester.pumpAndSettle();

    expect(find.text('Auto Rent'), findsOneWidget);
    expect(find.text(az.storeFollow), findsNothing);
  });
}
