import 'package:flutter/widgets.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/l10n/locale_controller.dart';
import '../../auth/domain/auth_models.dart';
import '../../auth/presentation/auth_controller.dart';
import '../../listing_create/domain/photo.dart';
import '../data/account_repository.dart';
import '../domain/account_models.dart';

enum PhotoProblem { tooLarge, unsupported }

/// The signed-in user's profile (name, bio, photo, language).
class AccountController extends AsyncNotifier<Account?> {
  AccountRepository get _repo => ref.read(accountRepositoryProvider);

  @override
  Future<Account?> build() async {
    if (ref.watch(authControllerProvider).value == null) return null;
    return _repo.me();
  }

  Account get _current => state.requireValue!;

  /// Saves name and bio. Throws on failure so the form can show it.
  Future<void> save({required String fullName, required String bio}) async {
    final current = _current;
    final saved = await _repo.update(
      fullName: fullName,
      bio: bio,
      language: current.language,
    );
    state = AsyncData(
      saved.copyWith(
        profileImagePath: current.profileImagePath,
        photoVersion: current.photoVersion,
      ),
    );
  }

  /// Changes the interface language now and tells the server in the
  /// background (the server copy is only used for messages it sends).
  Future<void> setLanguage(PreferredLanguage language) async {
    ref.read(localeProvider.notifier).set(Locale(language.code));
    final current = state.value;
    if (current == null || current.language == language) return;
    state = AsyncData(current.copyWith(language: language));
    if (!current.hasName) return; // the server requires a name to update
    try {
      await _repo.update(
        fullName: current.fullName,
        bio: current.bio,
        language: language,
      );
    } catch (_) {
      // The language is already switched on the phone; syncing is optional.
    }
  }

  /// Uploads a new photo. Returns a problem when the file is unacceptable.
  Future<PhotoProblem?> setPhoto(PickedPhoto photo) async {
    if (photo.sizeBytes > AccountRules.photoMaxBytes) {
      return PhotoProblem.tooLarge;
    }
    final current = _current;
    final path = await _repo.uploadPhoto(photo);
    state = AsyncData(
      current.copyWith(
        profileImagePath: path ?? '/api/users/${current.userId}/profile-image',
        photoVersion: current.photoVersion + 1,
      ),
    );
    return null;
  }

  Future<void> removePhoto() async {
    final current = _current;
    await _repo.deletePhoto();
    state = AsyncData(
      current.copyWith(
        clearPhoto: true,
        photoVersion: current.photoVersion + 1,
      ),
    );
  }
}

final accountProvider = AsyncNotifierProvider<AccountController, Account?>(
  AccountController.new,
);
