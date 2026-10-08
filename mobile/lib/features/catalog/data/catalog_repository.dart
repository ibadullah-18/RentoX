import 'package:dio/dio.dart';
import 'package:flutter/widgets.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/l10n/locale_controller.dart';
import '../../../core/network/api_client.dart';
import '../../../core/network/api_exception.dart';
import '../domain/catalog_models.dart';

class CatalogRepository {
  CatalogRepository(this._dio);

  final Dio _dio;

  Future<List<Category>> categories(String language) => guardApi(() async {
    final res = await _dio.get<List<dynamic>>(
      '/api/categories',
      queryParameters: {'language': language},
    );
    return res.data!
        .cast<Map<String, dynamic>>()
        .map(Category.fromJson)
        .toList(growable: false);
  });

  Future<Paged<ListingSummary>> listings({
    required String language,
    String? categoryId,
    String? search,
    double? minPrice,
    double? maxPrice,
    int page = 1,
    int pageSize = 20,
  }) => guardApi(() async {
    final res = await _dio.get<Map<String, dynamic>>(
      '/api/listings',
      queryParameters: {
        'language': language,
        'page': page,
        'pageSize': pageSize,
        'categoryId': ?categoryId,
        if (search != null && search.isNotEmpty) 'search': search,
        'minPrice': ?minPrice,
        'maxPrice': ?maxPrice,
      },
    );
    return Paged.fromJson(res.data!, ListingSummary.fromJson);
  });

  Future<ListingDetails> details(String id, String language) =>
      guardApi(() async {
        final res = await _dio.get<Map<String, dynamic>>(
          '/api/listings/$id',
          queryParameters: {'language': language},
        );
        return ListingDetails.fromJson(res.data!);
      });
}

final catalogRepositoryProvider = Provider<CatalogRepository>(
  (ref) => CatalogRepository(ref.watch(dioProvider)),
);

final categoriesProvider = FutureProvider<List<Category>>((ref) {
  final Locale locale = ref.watch(localeProvider);
  return ref.watch(catalogRepositoryProvider).categories(locale.languageCode);
});

final homeListingsProvider = FutureProvider<Paged<ListingSummary>>((ref) {
  final Locale locale = ref.watch(localeProvider);
  return ref
      .watch(catalogRepositoryProvider)
      .listings(language: locale.languageCode, pageSize: 24);
});

/// Details of one listing. Disposed when the page closes, so reopening it
/// always fetches fresh data (view counter, favourite state...).
final listingDetailsProvider = FutureProvider.autoDispose
    .family<ListingDetails, String>((ref, id) {
      final Locale locale = ref.watch(localeProvider);
      return ref
          .watch(catalogRepositoryProvider)
          .details(id, locale.languageCode);
    });
