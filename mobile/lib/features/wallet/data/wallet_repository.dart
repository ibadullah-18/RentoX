import 'package:dio/dio.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/network/api_client.dart';
import '../../../core/network/api_exception.dart';
import '../../auth/presentation/auth_controller.dart';
import '../domain/wallet_models.dart';

class WalletRepository {
  WalletRepository(this._dio);

  final Dio _dio;

  Future<WalletBalance> balance() => guardApi(() async {
    final res = await _dio.get<Map<String, dynamic>>('/api/wallet');
    return WalletBalance.fromJson(res.data!);
  });

  /// Adds demo money. The backend only allows this in Development.
  Future<WalletBalance> topUp(
    double amount, {
    required String idempotencyKey,
  }) => guardApi(() async {
    final res = await _dio.post<Map<String, dynamic>>(
      '/api/wallet/demo-top-up',
      data: {'amount': amount, 'idempotencyKey': idempotencyKey},
    );
    return WalletBalance.fromJson(
      (res.data!['wallet'] as Map).cast<String, dynamic>(),
    );
  });
}

final walletRepositoryProvider = Provider<WalletRepository>(
  (ref) => WalletRepository(ref.watch(dioProvider)),
);

/// Current balance of the signed-in user (reloads after payments/top-ups via
/// `ref.invalidate`).
final walletBalanceProvider = FutureProvider.autoDispose<WalletBalance?>((
  ref,
) async {
  if (ref.watch(authControllerProvider).value == null) return null;
  return ref.watch(walletRepositoryProvider).balance();
});
