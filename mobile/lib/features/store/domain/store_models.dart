import '../../../core/config/app_config.dart';

/// Mirrors the backend `StoreStatus` enum.
enum StoreStatus {
  draft(1),
  pendingReview(2),
  active(3),
  rejected(4),
  suspended(5),
  deleted(6);

  const StoreStatus(this.id);
  final int id;

  static StoreStatus fromId(int id) =>
      values.firstWhere((s) => s.id == id, orElse: () => draft);

  /// The backend only lets drafts and rejected stores be edited or submitted.
  bool get canEdit => this == draft || this == rejected;
  bool get canSubmit => canEdit;
}

/// Limits enforced by the backend (`StoreProfile`, `StoreImageService`).
abstract final class StoreRules {
  static const nameMin = 2;
  static const nameMax = 100;
  static const descriptionMin = 10;
  static const descriptionMax = 2000;
  static const phoneMin = 7;
  static const phoneMax = 30;
  static const emailMax = 254;
  static const addressMax = 500;
  static const urlMax = 300;
  static const logoMaxBytes = 5 * 1024 * 1024;
  static const coverMaxBytes = 10 * 1024 * 1024;
}

/// One open day of a store: [day] 1 Monday to 7 Sunday, the hours as minutes after midnight.
class StoreHours {
  const StoreHours({
    required this.day,
    required this.openMinute,
    required this.closeMinute,
  });

  final int day;
  final int openMinute;
  final int closeMinute;

  factory StoreHours.fromJson(Map<String, dynamic> j) => StoreHours(
    day: (j['day'] as num).toInt(),
    openMinute: (j['openMinute'] as num).toInt(),
    closeMinute: (j['closeMinute'] as num).toInt(),
  );

  Map<String, dynamic> toJson() => {
    'day': day,
    'openMinute': openMinute,
    'closeMinute': closeMinute,
  };

  /// "09:30".
  static String clock(int minute) =>
      '${(minute ~/ 60 % 24).toString().padLeft(2, '0')}:'
      '${(minute % 60).toString().padLeft(2, '0')}';

  /// Is a store with these hours open at [now] (Baku time, UTC+4)? `null` when no hours were given.
  static bool? openAt(List<StoreHours> hours, DateTime now) {
    if (hours.isEmpty) return null;
    final baku = now.toUtc().add(const Duration(hours: 4));
    final minute = baku.hour * 60 + baku.minute;
    return hours.any(
      (h) =>
          h.day == baku.weekday &&
          minute >= h.openMinute &&
          minute < h.closeMinute,
    );
  }
}

List<StoreHours> _hoursFrom(Object? raw) => [
  for (final e in (raw as List?) ?? const [])
    StoreHours.fromJson((e as Map).cast<String, dynamic>()),
]..sort((a, b) => a.day.compareTo(b.day));

/// The contact side of a store: it can be changed at any time, a live store stays live.
class StoreContactForm {
  const StoreContactForm({
    this.phone = '',
    this.email = '',
    this.address = '',
    this.whatsApp = '',
    this.whatsAppMessage = '',
    this.instagram = '',
    this.tiktok = '',
    this.youtube = '',
    this.facebook = '',
    this.website = '',
    this.hours = const [],
  });

  final String phone;
  final String email;
  final String address;
  final String whatsApp;
  final String whatsAppMessage;
  final String instagram;
  final String tiktok;
  final String youtube;
  final String facebook;
  final String website;
  final List<StoreHours> hours;

  static final _email = RegExp(r'^[^@\s]+@[^@\s]+\.[^@\s]+$');

  /// Every problem, keyed by field name. Empty when it can be sent.
  Map<String, StoreIssue> validate() {
    final out = <String, StoreIssue>{};
    final p = phone.trim();
    if (p.isEmpty) {
      out['phone'] = StoreIssue.required;
    } else if (p.length < StoreRules.phoneMin) {
      out['phone'] = StoreIssue.tooShort;
    } else if (p.length > StoreRules.phoneMax) {
      out['phone'] = StoreIssue.tooLong;
    }
    final m = email.trim();
    if (m.isNotEmpty && !_email.hasMatch(m)) {
      out['email'] = StoreIssue.invalidEmail;
    }
    if (address.trim().length > StoreRules.addressMax) {
      out['address'] = StoreIssue.tooLong;
    }
    final digits = whatsApp.replaceAll(RegExp(r'\D'), '');
    if (whatsApp.trim().isNotEmpty &&
        (digits.length < 9 || digits.length > 15)) {
      out['whatsApp'] = StoreIssue.invalidNumber;
    }
    for (final e in {
      'instagram': instagram,
      'tiktok': tiktok,
      'youtube': youtube,
      'facebook': facebook,
      'website': website,
    }.entries) {
      final v = e.value.trim();
      if (v.isEmpty) continue;
      final uri = Uri.tryParse(v);
      final ok =
          uri != null &&
          uri.hasAuthority &&
          uri.host.isNotEmpty &&
          (uri.scheme == 'http' || uri.scheme == 'https');
      if (!ok || v.length > StoreRules.urlMax) {
        out[e.key] = StoreIssue.invalidUrl;
      }
    }
    for (final h in hours) {
      if (h.openMinute < 0 ||
          h.closeMinute > 1440 ||
          h.closeMinute <= h.openMinute) {
        out['hours'] = StoreIssue.invalidHours;
      }
    }
    return out;
  }

  String? _opt(String v) => v.trim().isEmpty ? null : v.trim();

  Map<String, dynamic> toJson() => {
    'phoneNumber': phone.trim(),
    'email': _opt(email),
    'address': _opt(address),
    'instagramUrl': _opt(instagram),
    'tiktokUrl': _opt(tiktok),
    'facebookUrl': _opt(facebook),
    'youtubeUrl': _opt(youtube),
    'websiteUrl': _opt(website),
    'whatsAppNumber': _opt(whatsApp),
    'whatsAppMessage': _opt(whatsAppMessage),
    'workingHours': [for (final h in hours) h.toJson()],
  };
}

/// What the owner sees: followers, views and how often the store was contacted.
class StoreStats {
  const StoreStats({
    this.followers = 0,
    this.activeListings = 0,
    this.listingViews = 0,
    this.listingFavorites = 0,
    this.profileViews = 0,
    this.calls = 0,
    this.whatsApp = 0,
    this.messages = 0,
    this.viewsLast7Days = 0,
    this.viewsLast30Days = 0,
    this.contactsLast30Days = 0,
    this.last30Views = const [],
  });

  final int followers;
  final int activeListings;
  final int listingViews;
  final int listingFavorites;
  final int profileViews;
  final int calls;
  final int whatsApp;
  final int messages;
  final int viewsLast7Days;
  final int viewsLast30Days;
  final int contactsLast30Days;

  /// Page views per day, the oldest first (30 values).
  final List<int> last30Views;

  factory StoreStats.fromJson(Map<String, dynamic> j) {
    int n(String key) => (j[key] as num?)?.toInt() ?? 0;
    return StoreStats(
      followers: n('followers'),
      activeListings: n('activeListings'),
      listingViews: n('listingViews'),
      listingFavorites: n('listingFavorites'),
      profileViews: n('profileViews'),
      calls: n('calls'),
      whatsApp: n('whatsApp'),
      messages: n('messages'),
      viewsLast7Days: n('profileViewsLast7Days'),
      viewsLast30Days: n('profileViewsLast30Days'),
      contactsLast30Days: n('contactsLast30Days'),
      last30Views: [
        for (final d in (j['last30Days'] as List?) ?? const [])
          ((d as Map)['views'] as num?)?.toInt() ?? 0,
      ],
    );
  }
}

enum StoreField {
  name,
  description,
  phone,
  email,
  address,
  instagram,
  tiktok,
  facebook,
  website,
}

enum StoreIssue {
  required,
  tooShort,
  tooLong,
  invalidEmail,
  invalidUrl,
  invalidNumber,
  invalidHours,
}

/// What the user typed into the store form. Validation mirrors the backend so
/// mistakes are caught before a request is sent.
class StoreForm {
  const StoreForm({
    this.name = '',
    this.description = '',
    this.phone = '',
    this.email = '',
    this.address = '',
    this.instagram = '',
    this.tiktok = '',
    this.facebook = '',
    this.website = '',
  });

  final String name;
  final String description;
  final String phone;
  final String email;
  final String address;
  final String instagram;
  final String tiktok;
  final String facebook;
  final String website;

  static final _email = RegExp(r'^[^@\s]+@[^@\s]+\.[^@\s]+$');

  static StoreIssue? _required(String raw, int min, int max) {
    final v = raw.trim();
    if (v.isEmpty) return StoreIssue.required;
    if (v.length < min) return StoreIssue.tooShort;
    if (v.length > max) return StoreIssue.tooLong;
    return null;
  }

  static StoreIssue? _optionalUrl(String raw) {
    final v = raw.trim();
    if (v.isEmpty) return null;
    if (v.length > StoreRules.urlMax) return StoreIssue.tooLong;
    final uri = Uri.tryParse(v);
    final ok =
        uri != null &&
        uri.hasAuthority &&
        uri.host.isNotEmpty &&
        (uri.scheme == 'http' || uri.scheme == 'https');
    return ok ? null : StoreIssue.invalidUrl;
  }

  /// Every problem, keyed by field. Empty when the form can be sent.
  Map<StoreField, StoreIssue> validate() {
    final out = <StoreField, StoreIssue?>{
      StoreField.name: _required(name, StoreRules.nameMin, StoreRules.nameMax),
      StoreField.description: _required(
        description,
        StoreRules.descriptionMin,
        StoreRules.descriptionMax,
      ),
      StoreField.phone: _required(
        phone,
        StoreRules.phoneMin,
        StoreRules.phoneMax,
      ),
      StoreField.email: email.trim().isEmpty
          ? null
          : email.trim().length > StoreRules.emailMax
          ? StoreIssue.tooLong
          : (_email.hasMatch(email.trim()) ? null : StoreIssue.invalidEmail),
      StoreField.address: address.trim().length > StoreRules.addressMax
          ? StoreIssue.tooLong
          : null,
      StoreField.instagram: _optionalUrl(instagram),
      StoreField.tiktok: _optionalUrl(tiktok),
      StoreField.facebook: _optionalUrl(facebook),
      StoreField.website: _optionalUrl(website),
    };
    return {
      for (final e in out.entries)
        if (e.value != null) e.key: e.value!,
    };
  }

  String? _opt(String v) => v.trim().isEmpty ? null : v.trim();

  Map<String, dynamic> toJson() => {
    'name': name.trim(),
    'description': description.trim(),
    'phoneNumber': phone.trim(),
    'email': _opt(email),
    'address': _opt(address),
    'instagramUrl': _opt(instagram),
    'tiktokUrl': _opt(tiktok),
    'facebookUrl': _opt(facebook),
    'websiteUrl': _opt(website),
  };
}

/// The owner's own store (any status).
class MyStore {
  const MyStore({
    required this.id,
    required this.name,
    required this.slug,
    required this.description,
    required this.phone,
    required this.status,
    this.email = '',
    this.address = '',
    this.instagram = '',
    this.tiktok = '',
    this.facebook = '',
    this.website = '',
    this.youtube = '',
    this.whatsApp = '',
    this.whatsAppMessage = '',
    this.hours = const [],
    this.logoPath,
    this.coverPath,
    this.rejectionReason,
    this.imageVersion = 0,
  });

  final String id;
  final String name;
  final String slug;
  final String description;
  final String phone;
  final String email;
  final String address;
  final String instagram;
  final String tiktok;
  final String facebook;
  final String website;
  final String youtube;
  final String whatsApp;
  final String whatsAppMessage;
  final List<StoreHours> hours;
  final String? logoPath;
  final String? coverPath;
  final StoreStatus status;
  final String? rejectionReason;

  /// Bumped on every logo/cover change: the image URLs never change, so this
  /// is appended to defeat image caches.
  final int imageVersion;

  bool get hasLogo => (logoPath ?? '').isNotEmpty;
  bool get hasCover => (coverPath ?? '').isNotEmpty;

  String? _url(String? path) {
    if (path == null || path.isEmpty) return null;
    final base = AppConfig.resolveUrl(path)!;
    return imageVersion == 0 ? base : '$base?v=$imageVersion';
  }

  String? get logoUrl => _url(logoPath);
  String? get coverUrl => _url(coverPath);

  StoreContactForm toContactForm() => StoreContactForm(
    phone: phone,
    email: email,
    address: address,
    whatsApp: whatsApp,
    whatsAppMessage: whatsAppMessage,
    instagram: instagram,
    tiktok: tiktok,
    youtube: youtube,
    facebook: facebook,
    website: website,
    hours: hours,
  );

  StoreForm toForm() => StoreForm(
    name: name,
    description: description,
    phone: phone,
    email: email,
    address: address,
    instagram: instagram,
    tiktok: tiktok,
    facebook: facebook,
    website: website,
  );

  MyStore withImageVersion(int v) => MyStore(
    id: id,
    name: name,
    slug: slug,
    description: description,
    phone: phone,
    status: status,
    email: email,
    address: address,
    instagram: instagram,
    tiktok: tiktok,
    facebook: facebook,
    website: website,
    youtube: youtube,
    whatsApp: whatsApp,
    whatsAppMessage: whatsAppMessage,
    hours: hours,
    logoPath: logoPath,
    coverPath: coverPath,
    rejectionReason: rejectionReason,
    imageVersion: v,
  );

  factory MyStore.fromJson(Map<String, dynamic> j) => MyStore(
    id: j['id'] as String,
    name: (j['name'] as String?) ?? '',
    slug: (j['slug'] as String?) ?? '',
    description: (j['description'] as String?) ?? '',
    phone: (j['phoneNumber'] as String?) ?? '',
    email: (j['email'] as String?) ?? '',
    address: (j['address'] as String?) ?? '',
    instagram: (j['instagramUrl'] as String?) ?? '',
    tiktok: (j['tiktokUrl'] as String?) ?? '',
    facebook: (j['facebookUrl'] as String?) ?? '',
    website: (j['websiteUrl'] as String?) ?? '',
    youtube: (j['youtubeUrl'] as String?) ?? '',
    whatsApp: (j['whatsAppNumber'] as String?) ?? '',
    whatsAppMessage: (j['whatsAppMessage'] as String?) ?? '',
    hours: _hoursFrom(j['workingHours']),
    logoPath: j['logoImageUrl'] as String?,
    coverPath: j['coverImageUrl'] as String?,
    status: StoreStatus.fromId((j['status'] as num?)?.toInt() ?? 1),
    rejectionReason: j['rejectionReason'] as String?,
  );
}

/// A live store as everyone sees it.
class PublicStore {
  const PublicStore({
    required this.id,
    required this.name,
    required this.slug,
    required this.description,
    required this.phone,
    this.email = '',
    this.address = '',
    this.instagram = '',
    this.tiktok = '',
    this.facebook = '',
    this.website = '',
    this.youtube = '',
    this.whatsApp = '',
    this.whatsAppMessage = '',
    this.hours = const [],
    this.followerCount = 0,
    this.logoUrl,
    this.coverUrl,
    this.activeListingCount = 0,
    this.totalViewCount = 0,
    this.totalFavoriteCount = 0,
  });

  final String id;
  final String name;
  final String slug;
  final String description;
  final String phone;
  final String email;
  final String address;
  final String instagram;
  final String tiktok;
  final String facebook;
  final String website;
  final String youtube;

  /// International digits (994501234567); empty when the store has no WhatsApp.
  final String whatsApp;

  /// The first message the WhatsApp of a visitor opens with.
  final String whatsAppMessage;
  final List<StoreHours> hours;
  final int followerCount;
  final String? logoUrl;
  final String? coverUrl;
  final int activeListingCount;
  final int totalViewCount;
  final int totalFavoriteCount;

  /// The networks the store has, as (kind, url); a network without a link is not here.
  List<(SocialKind, String)> get links => [
    if (instagram.isNotEmpty) (SocialKind.instagram, instagram),
    if (tiktok.isNotEmpty) (SocialKind.tiktok, tiktok),
    if (youtube.isNotEmpty) (SocialKind.youtube, youtube),
    if (facebook.isNotEmpty) (SocialKind.facebook, facebook),
    if (website.isNotEmpty) (SocialKind.website, website),
  ];

  /// The wa.me link, with the first message of the owner ready to send.
  Uri? get whatsAppUri {
    final digits = whatsApp.replaceAll(RegExp(r'\D'), '');
    if (digits.isEmpty) return null;
    final text = whatsAppMessage.trim();
    return Uri.https('wa.me', '/$digits', {if (text.isNotEmpty) 'text': text});
  }

  factory PublicStore.fromJson(Map<String, dynamic> j) => PublicStore(
    id: j['id'] as String,
    name: (j['name'] as String?) ?? '',
    slug: (j['slug'] as String?) ?? '',
    description: (j['description'] as String?) ?? '',
    phone: (j['phoneNumber'] as String?) ?? '',
    email: (j['email'] as String?) ?? '',
    address: (j['address'] as String?) ?? '',
    instagram: (j['instagramUrl'] as String?) ?? '',
    tiktok: (j['tiktokUrl'] as String?) ?? '',
    facebook: (j['facebookUrl'] as String?) ?? '',
    website: (j['websiteUrl'] as String?) ?? '',
    youtube: (j['youtubeUrl'] as String?) ?? '',
    whatsApp: (j['whatsAppNumber'] as String?) ?? '',
    whatsAppMessage: (j['whatsAppMessage'] as String?) ?? '',
    hours: _hoursFrom(j['workingHours']),
    followerCount: (j['followerCount'] as num?)?.toInt() ?? 0,
    logoUrl: AppConfig.resolveUrl(j['logoImageUrl'] as String?),
    coverUrl: AppConfig.resolveUrl(j['coverImageUrl'] as String?),
    activeListingCount: (j['activeListingCount'] as num?)?.toInt() ?? 0,
    totalViewCount: (j['totalViewCount'] as num?)?.toInt() ?? 0,
    totalFavoriteCount: (j['totalFavoriteCount'] as num?)?.toInt() ?? 0,
  );
}

enum SocialKind { website, instagram, tiktok, youtube, facebook }

class FollowStatus {
  const FollowStatus({required this.isFollowing, required this.followerCount});

  final bool isFollowing;
  final int followerCount;

  FollowStatus toggled() => FollowStatus(
    isFollowing: !isFollowing,
    followerCount: (followerCount + (isFollowing ? -1 : 1)).clamp(0, 1 << 30),
  );

  factory FollowStatus.fromJson(Map<String, dynamic> j) => FollowStatus(
    isFollowing: (j['isFollowing'] as bool?) ?? false,
    followerCount: (j['followerCount'] as num?)?.toInt() ?? 0,
  );
}

class FollowedStore {
  const FollowedStore({
    required this.id,
    required this.name,
    required this.slug,
    this.description = '',
    this.logoUrl,
    this.activeListingCount = 0,
    this.followerCount = 0,
  });

  final String id;
  final String name;
  final String slug;

  /// Only filled in by the store search (not by the followed list).
  final String description;
  final String? logoUrl;
  final int activeListingCount;
  final int followerCount;

  factory FollowedStore.fromJson(Map<String, dynamic> j) => FollowedStore(
    id: j['storeId'] as String,
    name: (j['name'] as String?) ?? '',
    slug: (j['slug'] as String?) ?? '',
    description: (j['description'] as String?) ?? '',
    logoUrl: AppConfig.resolveUrl(j['logoImageUrl'] as String?),
    activeListingCount: (j['activeListingCount'] as num?)?.toInt() ?? 0,
    followerCount: (j['followerCount'] as num?)?.toInt() ?? 0,
  );
}

/// The offer on the owner store page: the home page place for a price and a time the admin set.
class StoreVipOffer {
  const StoreVipOffer({
    required this.name,
    required this.price,
    required this.durationDays,
    this.offerMessage = '',
    this.isOnSale = false,
    this.isVip = false,
    this.vipUntil,
    this.currency = 'AZN',
  });

  final String name;
  final double price;
  final int durationDays;
  final String offerMessage;

  /// False when the admin switched the product off: nothing is bought or shown.
  final bool isOnSale;
  final bool isVip;
  final DateTime? vipUntil;
  final String currency;

  factory StoreVipOffer.fromJson(Map<String, dynamic> j) => StoreVipOffer(
    name: (j['name'] as String?) ?? '',
    price: (j['price'] as num?)?.toDouble() ?? 0,
    durationDays: (j['durationDays'] as num?)?.toInt() ?? 0,
    offerMessage: (j['offerMessage'] as String?) ?? '',
    isOnSale: (j['isOnSale'] as bool?) ?? false,
    isVip: (j['isVip'] as bool?) ?? false,
    vipUntil: j['vipUntilUtc'] == null
        ? null
        : DateTime.tryParse(j['vipUntilUtc'] as String)?.toLocal(),
    currency: (j['currency'] as String?) ?? 'AZN',
  );
}
