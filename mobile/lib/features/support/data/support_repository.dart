import 'package:dio/dio.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/network/api_client.dart';
import '../../../core/network/api_exception.dart';
import '../../catalog/domain/catalog_models.dart';
import '../../listing_create/domain/photo.dart';
import '../domain/support_models.dart';

class SupportRepository {
  SupportRepository(this._dio);

  final Dio _dio;

  Future<Paged<SupportTicket>> list({int page = 1, int pageSize = 20}) =>
      guardApi(() async {
        final res = await _dio.get<Map<String, dynamic>>(
          '/api/support/tickets',
          queryParameters: {'page': page, 'pageSize': pageSize},
        );
        return Paged.fromJson(res.data!, SupportTicket.fromJson);
      });

  Future<SupportTicket> details(String id) => guardApi(() async {
    final res = await _dio.get<Map<String, dynamic>>(
      '/api/support/tickets/$id',
    );
    return SupportTicket.fromJson(res.data!);
  });

  /// Opens a ticket and returns its id.
  Future<String> create({
    required SupportCategory category,
    required String subject,
    required String message,
  }) => guardApi(() async {
    final res = await _dio.post<Map<String, dynamic>>(
      '/api/support/tickets',
      data: {
        'category': category.id,
        'subject': subject.trim(),
        'initialMessage': message,
      },
    );
    return res.data!['id'] as String;
  });

  /// A reply with photos (and, if wanted, words): up to 5.
  Future<void> replyWithPhotos(
    String id,
    String body,
    List<PickedPhoto> photos,
  ) => guardApi(() async {
    final form = FormData.fromMap({
      if (body.trim().isNotEmpty) 'body': body.trim(),
      'files': [
        for (final p in photos)
          MultipartFile.fromBytes(
            p.bytes,
            filename: p.name,
            contentType: DioMediaType.parse(p.mimeType),
          ),
      ],
    });
    await _dio.post<void>(
      '/api/support/tickets/$id/messages/images',
      data: form,
      options: Options(sendTimeout: const Duration(minutes: 2)),
    );
  });

  Future<void> reply(String id, String body) => guardApi(
    () => _dio.post<void>(
      '/api/support/tickets/$id/messages',
      data: {'body': body.trim()},
    ),
  );
}

final supportRepositoryProvider = Provider<SupportRepository>(
  (ref) => SupportRepository(ref.watch(dioProvider)),
);
