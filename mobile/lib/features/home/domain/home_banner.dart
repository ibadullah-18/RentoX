import '../../../core/config/app_config.dart';

/// A banner at the top of the home page, set by the staff.
class HomeBanner {
  const HomeBanner({
    required this.id,
    required this.title,
    required this.imageUrl,
    this.subtitle,
    this.linkUrl,
  });

  final String id;
  final String title;
  final String? subtitle;
  final String imageUrl;

  /// A place on the site ("/listing/…") or a full address; null when it is not a link.
  final String? linkUrl;

  factory HomeBanner.fromJson(Map<String, dynamic> json) => HomeBanner(
    id: json['id'] as String,
    title: json['title'] as String,
    subtitle: json['subtitle'] as String?,
    imageUrl: AppConfig.resolveUrl(json['imageUrl'] as String?) ?? '',
    linkUrl: json['linkUrl'] as String?,
  );
}
