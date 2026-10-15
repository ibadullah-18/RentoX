/// A fixed page (terms, privacy, about …) in the app's language.
class InfoPageSummary {
  const InfoPageSummary({required this.slug, required this.title});

  final String slug;
  final String title;

  factory InfoPageSummary.fromJson(Map<String, dynamic> json) =>
      InfoPageSummary(
        slug: json['slug'] as String,
        title: json['title'] as String,
      );
}

class InfoDocument {
  const InfoDocument({
    required this.slug,
    required this.title,
    required this.body,
    required this.updatedAt,
  });

  final String slug;
  final String title;

  /// The text: ## headings, paragraphs, lists, **bold**, [links](address).
  final String body;
  final DateTime updatedAt;

  factory InfoDocument.fromJson(Map<String, dynamic> json) => InfoDocument(
    slug: json['slug'] as String,
    title: json['title'] as String,
    body: json['body'] as String,
    updatedAt: DateTime.parse(json['updatedAtUtc'] as String).toLocal(),
  );
}

/// Who runs the app and how to reach them. Whatever is empty is not shown anywhere.
class SiteInfo {
  const SiteInfo({
    this.operatorName,
    this.voen,
    this.address,
    this.email,
    this.phone,
    this.hotline,
    this.whatsApp,
    this.instagram,
    this.tikTok,
    this.facebook,
  });

  final String? operatorName;
  final String? voen;
  final String? address;
  final String? email;
  final String? phone;
  final String? hotline;
  final String? whatsApp;
  final String? instagram;
  final String? tikTok;
  final String? facebook;

  static String? _text(dynamic value) {
    final text = (value as String?)?.trim();
    return text == null || text.isEmpty ? null : text;
  }

  factory SiteInfo.fromJson(Map<String, dynamic> json) => SiteInfo(
    operatorName: _text(json['operatorName']),
    voen: _text(json['voen']),
    address: _text(json['address']),
    email: _text(json['email']),
    phone: _text(json['phone']),
    hotline: _text(json['hotline']),
    whatsApp: _text(json['whatsApp']),
    instagram: _text(json['instagram']),
    tikTok: _text(json['tikTok']),
    facebook: _text(json['facebook']),
  );

  /// Nothing to show: no contact channel is filled in.
  bool get hasContacts =>
      email != null ||
      phone != null ||
      hotline != null ||
      whatsApp != null ||
      instagram != null ||
      tikTok != null ||
      facebook != null;
}
