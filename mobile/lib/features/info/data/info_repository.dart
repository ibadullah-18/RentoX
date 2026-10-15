import 'package:dio/dio.dart';
import 'package:flutter/widgets.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/l10n/locale_controller.dart';
import '../../../core/network/api_client.dart';
import '../../../core/network/api_exception.dart';
import '../domain/info_models.dart';

class InfoRepository {
  InfoRepository(this._dio);

  final Dio _dio;

  Future<List<InfoPageSummary>> pages(String language) => guardApi(() async {
    final res = await _dio.get<List<dynamic>>(
      '/api/pages',
      queryParameters: {'language': language},
    );
    return res.data!
        .cast<Map<String, dynamic>>()
        .map(InfoPageSummary.fromJson)
        .toList(growable: false);
  });

  Future<InfoDocument> page(String slug, String language) => guardApi(() async {
    final res = await _dio.get<Map<String, dynamic>>(
      '/api/pages/$slug',
      queryParameters: {'language': language},
    );
    return InfoDocument.fromJson(res.data!);
  });

  Future<SiteInfo> siteInfo() => guardApi(() async {
    final res = await _dio.get<Map<String, dynamic>>('/api/site-info');
    return SiteInfo.fromJson(res.data!);
  });
}

final infoRepositoryProvider = Provider<InfoRepository>(
  (ref) => InfoRepository(ref.watch(dioProvider)),
);

final infoPagesProvider = FutureProvider.autoDispose<List<InfoPageSummary>>((
  ref,
) {
  final Locale locale = ref.watch(localeProvider);
  return ref.watch(infoRepositoryProvider).pages(locale.languageCode);
});

final infoPageProvider = FutureProvider.autoDispose
    .family<InfoDocument, String>((ref, slug) {
      final Locale locale = ref.watch(localeProvider);
      return ref.watch(infoRepositoryProvider).page(slug, locale.languageCode);
    });

/// The contact details. A failure just means none: the app never breaks because of them.
final siteInfoProvider = FutureProvider.autoDispose<SiteInfo>((ref) async {
  try {
    return await ref.watch(infoRepositoryProvider).siteInfo();
  } catch (_) {
    return const SiteInfo();
  }
});
