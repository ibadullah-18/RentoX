import 'package:dio/dio.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/network/api_client.dart';
import '../../../core/network/api_exception.dart';
import '../../catalog/domain/catalog_models.dart';
import '../domain/field_models.dart';
import '../domain/photo.dart';

class ListingCreateRepository {
  ListingCreateRepository(this._dio);

  final Dio _dio;

  Future<List<FieldDefinition>> categoryFields(
    String categoryId,
    String language,
  ) => guardApi(() async {
    final res = await _dio.get<List<dynamic>>(
      '/api/categories/$categoryId/fields',
      queryParameters: {'language': language},
    );
    return (res.data!.cast<Map<String, dynamic>>())
        .map(FieldDefinition.fromJson)
        .toList()
      ..sort((a, b) => a.displayOrder.compareTo(b.displayOrder));
  });

  /// Creates the listing as a draft and returns its id.
  Future<String> create({
    required String categoryId,
    required String title,
    required String description,
    required double price,
    required RentalPeriodUnit unit,
    required List<Map<String, dynamic>> fields,
    String currency = 'AZN',
  }) => guardApi(() async {
    final res = await _dio.post<Map<String, dynamic>>(
      '/api/listings',
      data: {
        'categoryId': categoryId,
        'title': title,
        'description': description,
        'price': price,
        'currency': currency,
        'rentalPeriodUnit': unit.id,
        'fields': fields,
      },
    );
    return res.data!['id'] as String;
  });

  /// Uploads one photo. Photos are stored in upload order; the first one
  /// becomes the cover.
  Future<void> uploadPhoto(String listingId, PickedPhoto photo) =>
      guardApi(() async {
        final form = FormData.fromMap({
          'file': MultipartFile.fromBytes(
            photo.bytes,
            filename: photo.name,
            contentType: DioMediaType.parse(photo.mimeType),
          ),
        });
        await _dio.post<void>(
          '/api/listings/$listingId/images',
          data: form,
          options: Options(sendTimeout: const Duration(minutes: 2)),
        );
      });
}

final listingCreateRepositoryProvider = Provider<ListingCreateRepository>(
  (ref) => ListingCreateRepository(ref.watch(dioProvider)),
);
