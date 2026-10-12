import 'package:dio/dio.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/l10n/locale_controller.dart';
import '../../../core/network/api_client.dart';
import '../../../core/network/api_exception.dart';
import '../domain/location_models.dart';

class LocationsRepository {
  LocationsRepository(this._dio);

  final Dio _dio;

  Future<List<City>> cities(String language) => guardApi(() async {
    final res = await _dio.get<List<dynamic>>(
      '/api/locations/cities',
      queryParameters: {'language': language},
    );
    return res.data!
        .cast<Map<String, dynamic>>()
        .map(City.fromJson)
        .toList(growable: false);
  });

  Future<List<District>> districts(String cityId, String language) =>
      guardApi(() async {
        final res = await _dio.get<List<dynamic>>(
          '/api/locations/cities/$cityId/districts',
          queryParameters: {'language': language},
        );
        return res.data!
            .cast<Map<String, dynamic>>()
            .map(District.fromJson)
            .toList(growable: false);
      });
}

final locationsRepositoryProvider = Provider<LocationsRepository>(
  (ref) => LocationsRepository(ref.watch(dioProvider)),
);

final citiesProvider = FutureProvider.autoDispose<List<City>>((ref) {
  final language = ref.watch(localeProvider).languageCode;
  return ref.watch(locationsRepositoryProvider).cities(language);
});

final districtsProvider = FutureProvider.autoDispose
    .family<List<District>, String>((ref, cityId) {
      final language = ref.watch(localeProvider).languageCode;
      return ref.watch(locationsRepositoryProvider).districts(cityId, language);
    });
