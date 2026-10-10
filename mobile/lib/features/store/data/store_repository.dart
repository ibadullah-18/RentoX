import 'package:dio/dio.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/network/api_client.dart';
import '../../../core/network/api_exception.dart';
import '../../catalog/domain/catalog_models.dart';
import '../../listing_create/domain/photo.dart';
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
  }) => guardApi(() async {
    final res = await _dio.get<Map<String, dynamic>>(
      '/api/stores/$slug/listings',
      queryParameters: {
        'language': language,
        'page': page,
        'pageSize': pageSize,
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
