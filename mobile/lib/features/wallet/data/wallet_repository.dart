import 'package:dio/dio.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/network/api_client.dart';
import '../../../core/network/api_exception.dart';
import '../../catalog/domain/catalog_models.dart' show Paged;
import '../../auth/presentation/auth_controller.dart';
import '../domain/wallet_models.dart';

class WalletRepository {
  WalletRepository(this._dio);

  final Dio _dio;

  Future<WalletBalance> balance() => guardApi(() async {
    final res = await _dio.get<Map<String, dynamic>>('/api/wallet');
    return WalletBalance.fromJson(res.data!);
  });

  Future<Paged<WalletTransaction>> transactions({
    int page = 1,
    int pageSize = 20,
  }) => guardApi(() async {
    final res = await _dio.get<Map<String, dynamic>>(
      '/api/wallet/transactions',
      queryParameters: {'page': page, 'pageSize': pageSize},
    );
    final d = res.data!;
    return Paged(
      items: [
        for (final e in d['items'] as List)
          WalletTransaction.fromJson((e as Map).cast<String, dynamic>()),
      ],
      page: d['page'] as int,
      totalPages: d['totalPages'] as int,
      totalCount: d['totalCount'] as int,
    );
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

/// The wallet history, newest first, loaded page by page.
class WalletHistory {
  const WalletHistory({
    this.items = const [],
    this.page = 0,
    this.totalPages = 1,
  });

  final List<WalletTransaction> items;
  final int page;
  final int totalPages;

  bool get hasMore => page < totalPages;
}

class WalletHistoryController extends AsyncNotifier<WalletHistory> {
  bool _loadingMore = false;

  @override
  Future<WalletHistory> build() async {
    if (ref.watch(authControllerProvider).value == null) {
      return const WalletHistory();
    }
    final first = await ref.watch(walletRepositoryProvider).transactions();
    return WalletHistory(
      items: first.items,
      page: first.page,
      totalPages: first.totalPages,
    );
  }

  Future<void> loadMore() async {
    final current = state.value;
    if (current == null || !current.hasMore || _loadingMore) return;
    _loadingMore = true;
    try {
      final next = await ref
          .read(walletRepositoryProvider)
          .transactions(page: current.page + 1);
      final known = {for (final t in current.items) t.id};
      state = AsyncData(
        WalletHistory(
          items: [
            ...current.items,
            ...next.items.where((t) => !known.contains(t.id)),
          ],
          page: next.page,
          totalPages: next.totalPages,
        ),
      );
    } catch (_) {
      // Keep what is shown; the button stays for another try.
    } finally {
      _loadingMore = false;
    }
  }
}

final walletHistoryProvider =
    AsyncNotifierProvider.autoDispose<WalletHistoryController, WalletHistory>(
      WalletHistoryController.new,
    );
