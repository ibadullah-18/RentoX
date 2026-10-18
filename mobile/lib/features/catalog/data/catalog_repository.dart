import 'dart:convert';

import 'package:dio/dio.dart';
import 'package:flutter/widgets.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/l10n/locale_controller.dart';
import '../../../core/network/api_client.dart';
import '../../../core/network/api_exception.dart';
import '../../home/domain/home_banner.dart';
import '../../search/domain/search_options.dart';
import '../domain/catalog_models.dart';

class CatalogRepository {
  CatalogRepository(this._dio);

  final Dio _dio;

  /// The banners on show on the home page, in order.
  Future<List<HomeBanner>> banners(String language) => guardApi(() async {
    final res = await _dio.get<List<dynamic>>(
      '/api/banners',
      queryParameters: {'language': language, 'platform': 'app'},
    );
    return res.data!
        .cast<Map<String, dynamic>>()
        .map(HomeBanner.fromJson)
        .toList(growable: false);
  });

  /// The page of a campaign (a banner with its own page).
  Future<CampaignPageData> campaignPage(String slug, String language) =>
      guardApi(() async {
        final res = await _dio.get<Map<String, dynamic>>(
          '/api/banners/pages/${Uri.encodeComponent(slug)}',
          queryParameters: {'language': language},
        );
        return CampaignPageData.fromJson(res.data!);
      });

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
    List<Map<String, dynamic>>? fieldFilters,
    SellerType seller = SellerType.all,
    SearchSort sort = SearchSort.date,
    String? cityId,
    String? districtId,
    RentalPeriodUnit? rentalPeriod,
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
        if (fieldFilters != null && fieldFilters.isNotEmpty)
          'filters': jsonEncode(fieldFilters),
        if (seller != SellerType.all) 'sellerType': seller.apiValue,
        if (sort != SearchSort.date) 'sort': sort.apiValue,
        'cityId': ?cityId,
        'districtId': ?districtId,
        if (rentalPeriod != null) 'rentalPeriod': rentalPeriod.id,
      },
    );
    return Paged.fromJson(res.data!, ListingSummary.fromJson);
  });

  Future<List<SearchSuggestion>> suggestions(String text, String language) =>
      guardApi(() async {
        final res = await _dio.get<List<dynamic>>(
          '/api/listings/suggestions',
          queryParameters: {'q': text, 'language': language},
        );
        return res.data!
            .cast<Map<String, dynamic>>()
            .map(SearchSuggestion.fromJson)
            .toList(growable: false);
      });

  /// Listings like [id]: same brand first, then same category, then nearby.
  Future<List<ListingSummary>> similar(String id, String language) =>
      guardApi(() async {
        final res = await _dio.get<List<dynamic>>(
          '/api/listings/$id/similar',
          queryParameters: {'language': language, 'limit': 12},
        );
        return res.data!
            .cast<Map<String, dynamic>>()
            .map(ListingSummary.fromJson)
            .toList(growable: false);
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

/// The home banners. A failure just means none: the home page never breaks because of them.
final bannersProvider = FutureProvider<List<HomeBanner>>((ref) async {
  final Locale locale = ref.watch(localeProvider);
  try {
    return await ref
        .watch(catalogRepositoryProvider)
        .banners(locale.languageCode);
  } catch (_) {
    return const [];
  }
});

final campaignPageProvider = FutureProvider.autoDispose
    .family<CampaignPageData, String>((ref, slug) {
      final Locale locale = ref.watch(localeProvider);
      return ref
          .watch(catalogRepositoryProvider)
          .campaignPage(slug, locale.languageCode);
    });

final categoriesProvider = FutureProvider<List<Category>>((ref) {
  final Locale locale = ref.watch(localeProvider);
  return ref.watch(catalogRepositoryProvider).categories(locale.languageCode);
});

final similarListingsProvider = FutureProvider.autoDispose
    .family<List<ListingSummary>, String>((ref, id) {
      final Locale locale = ref.watch(localeProvider);
      return ref
          .watch(catalogRepositoryProvider)
          .similar(id, locale.languageCode);
    });

/// Drop-down suggestions for what is being typed in the search box.
final searchSuggestionsProvider = FutureProvider.autoDispose
    .family<List<SearchSuggestion>, String>((ref, text) {
      final Locale locale = ref.watch(localeProvider);
      return ref
          .watch(catalogRepositoryProvider)
          .suggestions(text, locale.languageCode);
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
