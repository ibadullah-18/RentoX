import 'package:dio/dio.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/network/api_client.dart';
import '../../../core/network/api_exception.dart';
import '../../auth/domain/auth_models.dart';
import '../../listing_create/domain/photo.dart';
import '../domain/account_models.dart';

class AccountRepository {
  AccountRepository(this._dio);

  final Dio _dio;

  Future<Account> me() => guardApi(() async {
    final res = await _dio.get<Map<String, dynamic>>('/api/account/me');
    return Account.fromJson(res.data!);
  });

  Future<Account> update({
    required String fullName,
    required String bio,
    required PreferredLanguage language,
  }) => guardApi(() async {
    final res = await _dio.put<Map<String, dynamic>>(
      '/api/account/me',
      data: {
        'fullName': fullName.trim(),
        'bio': bio.trim().isEmpty ? null : bio.trim(),
        'preferredLanguage': language.id,
      },
    );
    return Account.fromJson(res.data!);
  });

  /// Returns the (relative) URL of the new photo.
  Future<String?> uploadPhoto(PickedPhoto photo) => guardApi(() async {
    final form = FormData.fromMap({
      'file': MultipartFile.fromBytes(
        photo.bytes,
        filename: photo.name,
        contentType: DioMediaType.parse(photo.mimeType),
      ),
    });
    final res = await _dio.post<Map<String, dynamic>>(
      '/api/account/me/profile-image',
      data: form,
    );
    return res.data?['url'] as String?;
  });

  Future<void> deletePhoto() =>
      guardApi(() => _dio.delete<void>('/api/account/me/profile-image'));
}

final accountRepositoryProvider = Provider<AccountRepository>(
  (ref) => AccountRepository(ref.watch(dioProvider)),
);
