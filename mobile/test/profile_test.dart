import 'dart:typed_data';

import 'package:dio/dio.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:rentox/core/l10n/app_localizations.dart';
import 'package:rentox/core/l10n/locale_controller.dart';
import 'package:rentox/features/auth/domain/auth_models.dart';
import 'package:rentox/features/auth/presentation/auth_controller.dart';
import 'package:rentox/features/listing_create/domain/photo.dart';
import 'package:rentox/features/profile/data/account_repository.dart';
import 'package:rentox/features/profile/domain/account_models.dart';
import 'package:rentox/features/profile/presentation/account_controller.dart';
import 'package:rentox/features/profile/presentation/edit_profile_page.dart';

class _SignedIn extends AuthController {
  @override
  Future<AuthSession?> build() async =>
      const AuthSession(userId: 'u1', phoneNumber: '+994501234567');
}

class FakeAccounts extends AccountRepository {
  FakeAccounts() : super(Dio());

  Account account = const Account(
    userId: 'u1',
    phoneNumber: '+994501234567',
    fullName: '',
    bio: '',
    language: PreferredLanguage.azerbaijani,
  );
  final updates = <({String name, String bio, int language})>[];
  int uploads = 0;
  int deletes = 0;
  bool failUpdate = false;

  @override
  Future<Account> me() async => account;

  @override
  Future<Account> update({
    required String fullName,
    required String bio,
    required PreferredLanguage language,
  }) async {
    if (failUpdate) throw Exception('boom');
    updates.add((
      name: fullName.trim(),
      bio: bio.trim(),
      language: language.id,
    ));
    account = account.copyWith(
      fullName: fullName.trim(),
      bio: bio.trim(),
      language: language,
    );
    return account;
  }

  @override
  Future<String?> uploadPhoto(PickedPhoto photo) async {
    uploads++;
    return '/api/users/u1/profile-image';
  }

  @override
  Future<void> deletePhoto() async => deletes++;
}

ProviderContainer makeContainer(FakeAccounts repo) => ProviderContainer(
  overrides: [
    authControllerProvider.overrideWith(_SignedIn.new),
    accountRepositoryProvider.overrideWithValue(repo),
  ],
);

PickedPhoto photo(int bytes) =>
    PickedPhoto(name: 'a.jpg', bytes: Uint8List(bytes), mimeType: 'image/jpeg');

void main() {
  group('validateName', () {
    test('mirrors the backend rule (2 to 100 characters)', () {
      expect(validateName(''), NameIssue.empty);
      expect(validateName('   '), NameIssue.empty);
      expect(validateName('A'), NameIssue.tooShort);
      expect(validateName(' Al '), isNull);
      expect(validateName('x' * 100), isNull);
      expect(validateName('x' * 101), NameIssue.tooLong);
    });
  });

  group('Account', () {
    test('parses the API response and busts the photo cache', () {
      final a = Account.fromJson({
        'userId': 'u1',
        'phoneNumber': '+994501234567',
        'fullName': 'Murad Əliyev',
        'bio': null,
        'preferredLanguage': 3,
        'profileImageUrl': '/api/users/u1/profile-image',
      });
      expect(a.language, PreferredLanguage.english);
      expect(a.bio, '');
      expect(a.photoUrl, endsWith('/api/users/u1/profile-image'));
      expect(a.copyWith(photoVersion: 2).photoUrl, endsWith('?v=2'));
      expect(a.copyWith(clearPhoto: true).photoUrl, isNull);
    });
  });

  group('AccountController', () {
    test('saving keeps the language and the photo', () async {
      final repo = FakeAccounts()..account = repoWithPhoto();
      final c = makeContainer(repo);
      addTearDown(c.dispose);
      c.listen(accountProvider, (_, _) {});
      await c.read(accountProvider.future);

      await c
          .read(accountProvider.notifier)
          .save(fullName: '  Murad ', bio: 'Salam');
      final a = c.read(accountProvider).requireValue!;
      expect(a.fullName, 'Murad');
      expect(a.bio, 'Salam');
      expect(a.profileImagePath, isNotNull);
      expect(repo.updates.single.language, 1);
    });

    test('a failed save throws and changes nothing', () async {
      final repo = FakeAccounts()..failUpdate = true;
      final c = makeContainer(repo);
      addTearDown(c.dispose);
      c.listen(accountProvider, (_, _) {});
      await c.read(accountProvider.future);

      await expectLater(
        c.read(accountProvider.notifier).save(fullName: 'Murad', bio: ''),
        throwsException,
      );
      expect(c.read(accountProvider).requireValue!.fullName, '');
    });

    test(
      'photo upload bumps the cache version, oversized is refused',
      () async {
        final repo = FakeAccounts();
        final c = makeContainer(repo);
        addTearDown(c.dispose);
        c.listen(accountProvider, (_, _) {});
        await c.read(accountProvider.future);
        final n = c.read(accountProvider.notifier);

        expect(
          await n.setPhoto(photo(AccountRules.photoMaxBytes + 1)),
          PhotoProblem.tooLarge,
        );
        expect(repo.uploads, 0);

        expect(await n.setPhoto(photo(100)), isNull);
        var a = c.read(accountProvider).requireValue!;
        expect(a.photoUrl, endsWith('?v=1'));

        await n.removePhoto();
        a = c.read(accountProvider).requireValue!;
        expect(a.photoUrl, isNull);
        expect(a.photoVersion, 2);
        expect(repo.deletes, 1);
      },
    );

    test('changing language switches the app and syncs when named', () async {
      final repo = FakeAccounts()..account = repoWithPhoto(name: 'Murad');
      final c = makeContainer(repo);
      addTearDown(c.dispose);
      c.listen(accountProvider, (_, _) {});
      c.listen(localeProvider, (_, _) {});
      await c.read(accountProvider.future);

      await c
          .read(accountProvider.notifier)
          .setLanguage(PreferredLanguage.russian);
      expect(c.read(localeProvider).languageCode, 'ru');
      expect(repo.updates.single.language, 2);
    });

    test('language still switches for a user without a name', () async {
      final repo = FakeAccounts();
      final c = makeContainer(repo);
      addTearDown(c.dispose);
      c.listen(accountProvider, (_, _) {});
      c.listen(localeProvider, (_, _) {});
      await c.read(accountProvider.future);

      await c
          .read(accountProvider.notifier)
          .setLanguage(PreferredLanguage.english);
      expect(c.read(localeProvider).languageCode, 'en');
      expect(repo.updates, isEmpty); // the server needs a name first
    });
  });

  testWidgets('edit page validates, saves and resets the dirty state', (
    tester,
  ) async {
    tester.view.physicalSize = const Size(800, 2200);
    tester.view.devicePixelRatio = 1;
    addTearDown(tester.view.reset);
    final repo = FakeAccounts();
    final c = makeContainer(repo);
    addTearDown(c.dispose);

    await tester.pumpWidget(
      UncontrolledProviderScope(
        container: c,
        child: const MaterialApp(
          locale: Locale('az'),
          localizationsDelegates: AppL10n.localizationsDelegates,
          supportedLocales: AppL10n.supportedLocales,
          home: EditProfilePage(),
        ),
      ),
    );
    await tester.pump(const Duration(milliseconds: 100));
    await tester.pump(const Duration(milliseconds: 100));

    FilledButton saveButton() => tester.widget<FilledButton>(
      find.widgetWithText(FilledButton, 'Yadda saxla'),
    );

    // Nothing changed yet: saving is off.
    expect(saveButton().onPressed, isNull);

    // One character is too short: error shown, nothing sent.
    await tester.enterText(find.byType(TextField).first, 'A');
    await tester.pump();
    expect(find.text('Ad 2 ilə 100 simvol arasında olmalıdır'), findsOneWidget);
    await tester.tap(find.text('Yadda saxla'));
    await tester.pump();
    expect(repo.updates, isEmpty);

    await tester.enterText(find.byType(TextField).first, 'Murad Əliyev');
    await tester.pump();
    expect(find.text('Ad 2 ilə 100 simvol arasında olmalıdır'), findsNothing);
    await tester.tap(find.text('Yadda saxla'));
    await tester.pump();
    await tester.pump(const Duration(milliseconds: 100));

    expect(repo.updates.single.name, 'Murad Əliyev');
    expect(find.text('Profil yeniləndi'), findsOneWidget);
    await tester.pump(const Duration(milliseconds: 100));
    expect(saveButton().onPressed, isNull);
  });
}

Account repoWithPhoto({String name = ''}) => Account(
  userId: 'u1',
  phoneNumber: '+994501234567',
  fullName: name,
  bio: '',
  language: PreferredLanguage.azerbaijani,
  profileImagePath: '/api/users/u1/profile-image',
);
