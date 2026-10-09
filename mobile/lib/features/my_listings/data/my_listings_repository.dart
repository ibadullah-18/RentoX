import 'package:dio/dio.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/l10n/locale_controller.dart';
import '../../../core/network/api_client.dart';
import '../../../core/network/api_exception.dart';
import '../../catalog/domain/catalog_models.dart';
import '../../wallet/domain/wallet_models.dart';
import '../domain/owned_listing_models.dart';

class MyListingsRepository {
  MyListingsRepository(this._dio);

  final Dio _dio;

  Future<Paged<OwnedListingSummary>> mine({int page = 1, int pageSize = 20}) =>
      guardApi(() async {
        final res = await _dio.get<Map<String, dynamic>>(
          '/api/listings/mine',
          queryParameters: {'page': page, 'pageSize': pageSize},
        );
        return Paged.fromJson(res.data!, OwnedListingSummary.fromJson);
      });

  Future<OwnedListingDetails> details(String id, String language) =>
      guardApi(() async {
        final res = await _dio.get<Map<String, dynamic>>(
          '/api/listings/mine/$id',
          queryParameters: {'language': language},
        );
        return OwnedListingDetails.fromJson(res.data!);
      });

  /// Sends a draft (or rejected) listing to moderation.
  Future<ListingStatus> submit(String id) => guardApi(() async {
    final res = await _dio.post<Map<String, dynamic>>(
      '/api/listings/$id/submit',
    );
    return ListingStatus.fromId(res.data!['status'] as int);
  });

  /// Pays the activation fee from the wallet (listings in `paymentRequired`).
  Future<PaymentResult> payAndActivate(String id) => guardApi(() async {
    final res = await _dio.post<Map<String, dynamic>>(
      '/api/listings/$id/pay-and-activate',
    );
    return PaymentResult.fromJson(res.data!);
  });

  /// Buys a promotion. Retrying with the same [idempotencyKey] never charges
  /// twice.
  Future<PaymentResult> promote(
    String id, {
    required PromotionType type,
    required String idempotencyKey,
  }) => guardApi(() async {
    final res = await _dio.post<Map<String, dynamic>>(
      '/api/listings/$id/promotions',
      data: {'type': type.id, 'idempotencyKey': idempotencyKey},
    );
    return PaymentResult.fromJson(res.data!);
  });

  Future<void> deactivate(String id) =>
      guardApi(() => _dio.post<void>('/api/listings/$id/deactivate'));

  Future<void> reactivate(String id) =>
      guardApi(() => _dio.post<void>('/api/listings/$id/reactivate'));

  /// Renews an expired listing for another 30 days. Charges the wallet unless
  /// the monthly free quota applies; the reply says what was charged.
  Future<PaymentResult> renew(String id) => guardApi(() async {
    final res = await _dio.post<Map<String, dynamic>>(
      '/api/listings/$id/renew',
    );
    return PaymentResult.fromJson(res.data!);
  });

  /// Saves title, description, price and unit. Only drafts and rejected
  /// listings accept this; the listing becomes a draft again.
  Future<void> updateDetails(
    String id, {
    required String title,
    required String description,
    required double price,
    required RentalPeriodUnit unit,
    String currency = 'AZN',
  }) => guardApi(
    () => _dio.put<void>(
      '/api/listings/$id',
      data: {
        'title': title,
        'description': description,
        'price': price,
        'currency': currency,
        'rentalPeriodUnit': unit.id,
      },
    ),
  );

  /// Replaces ALL category field values with [fields].
  Future<void> updateFields(String id, List<Map<String, dynamic>> fields) =>
      guardApi(
        () => _dio.put<void>(
          '/api/listings/$id/fields',
          data: {'fields': fields},
        ),
      );

  Future<void> deleteImage(String listingId, String imageId) => guardApi(
    () => _dio.delete<void>('/api/listings/$listingId/images/$imageId'),
  );

  Future<void> setCover(String listingId, String imageId) => guardApi(
    () => _dio.patch<void>('/api/listings/$listingId/images/$imageId/cover'),
  );

  /// Removes a listing (used to discard an unfinished draft).
  Future<void> delete(String id) =>
      guardApi(() => _dio.delete<void>('/api/listings/$id'));
}

final myListingsRepositoryProvider = Provider<MyListingsRepository>(
  (ref) => MyListingsRepository(ref.watch(dioProvider)),
);

/// The owner's view of one listing, read straight from the server.
final myListingDetailsProvider = FutureProvider.autoDispose
    .family<OwnedListingDetails, String>((ref, id) {
      return ref
          .watch(myListingsRepositoryProvider)
          .details(id, ref.watch(localeProvider).languageCode);
    });
