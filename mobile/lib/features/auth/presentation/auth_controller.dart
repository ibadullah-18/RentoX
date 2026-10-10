import 'dart:async';

import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../notifications/data/push_registrar.dart';
import '../data/auth_repository.dart';
import '../domain/auth_models.dart';

/// Holds the signed-in session. `null` data means signed out.
class AuthController extends AsyncNotifier<AuthSession?> {
  @override
  Future<AuthSession?> build() => ref.read(authRepositoryProvider).restore();

  void signedIn(AuthSession session) => state = AsyncData(session);

  Future<void> signOut() async {
    // While the session is still valid: stop pushes for this phone.
    await ref.read(pushRegistrarProvider).unregister();
    await ref.read(authRepositoryProvider).logout();
    state = const AsyncData(null);
  }

  /// Signs out of every device. Throws if the server can't be reached.
  Future<void> signOutEverywhere() async {
    await ref.read(pushRegistrarProvider).unregister();
    await ref.read(authRepositoryProvider).logoutAll();
    state = const AsyncData(null);
  }

  /// The account was deleted on the server: just drop the local session.
  Future<void> accountDeleted() async {
    await ref.read(authRepositoryProvider).clearLocal();
    state = const AsyncData(null);
  }

  /// Called by the network layer when the refresh token is rejected.
  void sessionExpired() => state = const AsyncData(null);
}

final authControllerProvider =
    AsyncNotifierProvider<AuthController, AuthSession?>(AuthController.new);
