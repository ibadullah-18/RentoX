import 'dart:async';

import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../data/auth_repository.dart';
import '../domain/auth_models.dart';

/// Holds the signed-in session. `null` data means signed out.
class AuthController extends AsyncNotifier<AuthSession?> {
  @override
  Future<AuthSession?> build() => ref.read(authRepositoryProvider).restore();

  void signedIn(AuthSession session) => state = AsyncData(session);

  Future<void> signOut() async {
    await ref.read(authRepositoryProvider).logout();
    state = const AsyncData(null);
  }

  /// Called by the network layer when the refresh token is rejected.
  void sessionExpired() => state = const AsyncData(null);
}

final authControllerProvider =
    AsyncNotifierProvider<AuthController, AuthSession?>(AuthController.new);
