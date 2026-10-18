import '../../../core/config/app_config.dart';

/// A banner at the top of the home page, set by the staff.
class HomeBanner {
  const HomeBanner({
    required this.id,
    required this.title,
    required this.imageUrl,
    this.subtitle,
    this.linkUrl,
    this.pageSlug,
    this.showText = true,
    this.textAlign = 0,
    this.darkOverlay = true,
    this.textTone = 0,
  });

  final String id;
  final String title;
  final String? subtitle;
  final String imageUrl;

  /// A place on the site ("/listing/…") or a full address; null when it is not a link.
  final String? linkUrl;

  /// The name of the campaign page (rentox.az/name), when the banner has one.
  final String? pageSlug;

  /// Title and subtitle over the picture.
  final bool showText;

  /// 0 left, 1 centre, 2 right.
  final int textAlign;

  /// A dark fade under the text.
  final bool darkOverlay;

  /// 0 light text, 1 dark text.
  final int textTone;

  factory HomeBanner.fromJson(Map<String, dynamic> json) => HomeBanner(
    id: json['id'] as String,
    title: json['title'] as String,
    subtitle: json['subtitle'] as String?,
    imageUrl: AppConfig.resolveUrl(json['imageUrl'] as String?) ?? '',
    linkUrl: json['linkUrl'] as String?,
    pageSlug: json['pageSlug'] as String?,
    showText: (json['showText'] as bool?) ?? true,
    textAlign: (json['textAlign'] as num?)?.toInt() ?? 0,
    darkOverlay: (json['darkOverlay'] as bool?) ?? true,
    textTone: (json['textTone'] as num?)?.toInt() ?? 0,
  );
}

/// The page of a campaign (rentox.az/name).
class CampaignPageData {
  const CampaignPageData({
    required this.title,
    required this.imageUrl,
    required this.isOver,
    this.subtitle,
    this.body,
    this.buttonLabel,
    this.buttonUrl,
  });

  final String title;
  final String? subtitle;
  final String? body;
  final String imageUrl;
  final String? buttonLabel;
  final String? buttonUrl;

  /// The campaign has ended.
  final bool isOver;

  factory CampaignPageData.fromJson(Map<String, dynamic> json) =>
      CampaignPageData(
        title: json['title'] as String,
        subtitle: json['subtitle'] as String?,
        body: json['body'] as String?,
        imageUrl: AppConfig.resolveUrl(json['imageUrl'] as String?) ?? '',
        buttonLabel: json['buttonLabel'] as String?,
        buttonUrl: json['buttonUrl'] as String?,
        isOver: (json['isOver'] as bool?) ?? false,
      );
}
