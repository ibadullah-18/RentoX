import 'package:dio/dio.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/network/api_client.dart';
import '../../../core/network/api_exception.dart';
import '../../catalog/domain/catalog_models.dart';
import '../../listing_create/domain/photo.dart';
import '../../wallet/domain/wallet_models.dart';
import '../domain/store_models.dart';

enum StoreImageKind { logo, cover }

class StoreRepository {
  StoreRepository(this._dio);

  final Dio _dio;

  // ---- the owner's store ------------------------------------------------

  /// The signed-in user's store, or `null` when they have none yet.
  Future<MyStore?> mine() async {
    try {
      return await guardApi(() async {
        final res = await _dio.get<Map<String, dynamic>>('/api/stores/mine');
        return MyStore.fromJson(res.data!);
      });
    } on ApiException catch (e) {
      if (e.statusCode == 404) return null;
      rethrow;
    }
  }

  Future<void> create(StoreForm form) =>
      guardApi(() => _dio.post<void>('/api/stores', data: form.toJson()));

  Future<void> update(StoreForm form) =>
      guardApi(() => _dio.put<void>('/api/stores/mine', data: form.toJson()));

  /// The contact side (phone, networks, WhatsApp, hours): changeable at any time.
  Future<void> updateContact(StoreContactForm form) => guardApi(
    () => _dio.put<void>('/api/stores/mine/contact', data: form.toJson()),
  );

  /// The name and description of a live store, any time.
  Future<void> updateInfo(String name, String description) => guardApi(
    () => _dio.put<void>(
      '/api/stores/mine/info',
      data: {'name': name.trim(), 'description': description.trim()},
    ),
  );

  /// The VIP place offer for the owner store (null when there is no live store).
  Future<StoreVipOffer?> vipOffer() async {
    try {
      return await guardApi(() async {
        final res = await _dio.get<Map<String, dynamic>>(
          '/api/stores/mine/vip',
        );
        return StoreVipOffer.fromJson(res.data!);
      });
    } on ApiException catch (e) {
      if (e.statusCode == 404) return null;
      rethrow;
    }
  }

  /// Pays for the VIP place from the wallet. The same [idempotencyKey] never
  /// charges twice.
  Future<PaymentResult> buyVip(String idempotencyKey) => guardApi(() async {
    final res = await _dio.post<Map<String, dynamic>>(
      '/api/stores/mine/vip',
      data: {'idempotencyKey': idempotencyKey},
    );
    return PaymentResult.fromJson(res.data!);
  });

  /// The stores that paid for a place on the home page.
  Future<List<FollowedStore>> vipStores({int take = 10}) => guardApi(() async {
    final res = await _dio.get<List<dynamic>>(
      '/api/stores/vip',
      queryParameters: {'take': take},
    );
    return [
      for (final e in res.data!)
        FollowedStore.fromJson(e as Map<String, dynamic>),
    ];
  });

  Future<StoreStats> stats() => guardApi(() async {
    final res = await _dio.get<Map<String, dynamic>>('/api/stores/mine/stats');
    return StoreStats.fromJson(res.data!);
  });

  /// Tells the site that the page was opened or a contact button used
  /// (view, call, whatsapp, message). Fire and forget: it never fails the visitor.
  Future<void> reportEvent(String slug, String kind) async {
    try {
      await _dio.post<void>('/api/stores/$slug/events', data: {'kind': kind});
    } catch (_) {
      // Counting must never get in the way.
    }
  }

  /// Sends the store for review (needs a logo).
  Future<void> submit() =>
      guardApi(() => _dio.post<void>('/api/stores/mine/submit'));

  Future<void> uploadImage(StoreImageKind kind, PickedPhoto photo) =>
      guardApi(() async {
        final form = FormData.fromMap({
          'file': MultipartFile.fromBytes(
            photo.bytes,
            filename: photo.name,
            contentType: DioMediaType.parse(photo.mimeType),
          ),
        });
        await _dio.post<void>(
          '/api/stores/mine/${kind.name}',
          data: form,
          options: Options(sendTimeout: const Duration(minutes: 2)),
        );
      });

  Future<void> deleteImage(StoreImageKind kind) =>
      guardApi(() => _dio.delete<void>('/api/stores/mine/${kind.name}'));

  // ---- public ---------------------------------------------------------

  Future<PublicStore> bySlug(String slug) => guardApi(() async {
    final res = await _dio.get<Map<String, dynamic>>('/api/stores/$slug');
    return PublicStore.fromJson(res.data!);
  });

  Future<Paged<ListingSummary>> listings(
    String slug, {
    required String language,
    int page = 1,
    int pageSize = 20,
    String search = '',
    double? minPrice,
    double? maxPrice,
  }) => guardApi(() async {
    final res = await _dio.get<Map<String, dynamic>>(
      '/api/stores/$slug/listings',
      queryParameters: {
        'language': language,
        'page': page,
        'pageSize': pageSize,
        if (search.trim().isNotEmpty) 'search': search.trim(),
        'minPrice': ?minPrice,
        'maxPrice': ?maxPrice,
      },
    );
    return Paged.fromJson(res.data!, ListingSummary.fromJson);
  });

  /// Public stores; the most active first, or best match when [search] is set.
  Future<Paged<FollowedStore>> search({
    String search = '',
    int page = 1,
    int pageSize = 20,
  }) => guardApi(() async {
    final res = await _dio.get<Map<String, dynamic>>(
      '/api/stores',
      queryParameters: {
        if (search.trim().isNotEmpty) 'search': search.trim(),
        'page': page,
        'pageSize': pageSize,
      },
    );
    return Paged.fromJson(res.data!, FollowedStore.fromJson);
  });

  // ---- following ------------------------------------------------------

  Future<FollowStatus> followStatus(String storeId) => guardApi(() async {
    final res = await _dio.get<Map<String, dynamic>>(
      '/api/stores/$storeId/follow-status',
    );
    return FollowStatus.fromJson(res.data!);
  });

  Future<void> follow(String storeId) =>
      guardApi(() => _dio.post<void>('/api/stores/$storeId/follow'));

  Future<void> unfollow(String storeId) =>
      guardApi(() => _dio.delete<void>('/api/stores/$storeId/follow'));

  Future<Paged<FollowedStore>> following({int page = 1, int pageSize = 20}) =>
      guardApi(() async {
        final res = await _dio.get<Map<String, dynamic>>(
          '/api/stores/following',
          queryParameters: {'page': page, 'pageSize': pageSize},
        );
        return Paged.fromJson(res.data!, FollowedStore.fromJson);
      });
}

final storeRepositoryProvider = Provider<StoreRepository>(
  (ref) => StoreRepository(ref.watch(dioProvider)),
);
