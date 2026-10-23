import 'package:dio/dio.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../config/app_config.dart';
import '../network/api_client.dart';

/// How the brand looks, as the owner set it in the admin panel: the same name, logos and sizes as on the site.
/// A picture address is null while the owner has not uploaded their own: then the picture inside the app is used.
class Branding {
  const Branding({
    this.siteName = 'RentoX',
    this.footerText,
    this.useTextLogo = false,
    this.headerLogoHeight = 40,
    this.headerLogoUrl,
    this.headerLogoDarkUrl,
    this.iconUrl,
  });

  final String siteName;
  final String? footerText;

  /// Write the name instead of showing the logo picture.
  final bool useTextLogo;

  /// The height the owner chose for the logo in the header (the site's default is 40).
  final int headerLogoHeight;
  final String? headerLogoUrl;
  final String? headerLogoDarkUrl;
  final String? iconUrl;

  /// The picture for a light or a dark page; a dark page falls back to the light picture the owner uploaded.
  String? logoFor({required bool dark}) =>
      dark ? (headerLogoDarkUrl ?? headerLogoUrl) : headerLogoUrl;

  factory Branding.fromJson(Map<String, dynamic> json) => Branding(
    siteName: (json['siteName'] as String?)?.trim().isNotEmpty == true
        ? json['siteName'] as String
        : 'RentoX',
    footerText: json['footerText'] as String?,
    useTextLogo: (json['useTextLogo'] as bool?) ?? false,
    headerLogoHeight: (json['headerLogoHeight'] as num?)?.toInt() ?? 40,
    headerLogoUrl: AppConfig.resolveUrl(json['headerLogoUrl'] as String?),
    headerLogoDarkUrl: AppConfig.resolveUrl(
      json['headerLogoDarkUrl'] as String?,
    ),
    iconUrl: AppConfig.resolveUrl(json['iconUrl'] as String?),
  );
}

/// The brand. A failure just means the pictures that come with the app: the app never breaks because of it.
final brandingProvider = FutureProvider<Branding>((ref) async {
  try {
    final res = await ref
        .watch(dioProvider)
        .get<Map<String, dynamic>>(
          '/api/branding',
          options: Options(receiveTimeout: const Duration(seconds: 8)),
        );
    return Branding.fromJson(res.data!);
  } catch (_) {
    return const Branding();
  }
});
