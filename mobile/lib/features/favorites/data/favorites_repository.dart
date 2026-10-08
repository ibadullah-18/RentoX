import 'package:dio/dio.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/network/api_client.dart';
import '../../../core/network/api_exception.dart';
import '../../catalog/domain/catalog_models.dart';

class FavoritesRepository {
  FavoritesRepository(this._dio);

  final Dio _dio;

  Future<Paged<ListingSummary>> list({
    required String language,
    int page = 1,
    int pageSize = 20,
  }) => guardApi(() async {
    final res = await _dio.get<Map<String, dynamic>>(
      '/api/favorites',
      queryParameters: {
        'language': language,
        'page': page,
        'pageSize': pageSize,
      },
    );
    return Paged.fromJson(res.data!, ListingSummary.fromJson);
  });

  Future<void> add(String listingId) =>
      guardApi(() => _dio.post<void>('/api/favorites/$listingId'));

  Future<void> remove(String listingId) =>
      guardApi(() => _dio.delete<void>('/api/favorites/$listingId'));
}

final favoritesRepositoryProvider = Provider<FavoritesRepository>(
  (ref) => FavoritesRepository(ref.watch(dioProvider)),
);
