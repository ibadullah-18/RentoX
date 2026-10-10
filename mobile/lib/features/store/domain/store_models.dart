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

enum StoreIssue { required, tooShort, tooLong, invalidEmail, invalidUrl }

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
  final String? logoUrl;
  final String? coverUrl;
  final int activeListingCount;
  final int totalViewCount;
  final int totalFavoriteCount;

  /// Social/web links that are filled in, as (kind, url).
  List<(SocialKind, String)> get links => [
    if (website.isNotEmpty) (SocialKind.website, website),
    if (instagram.isNotEmpty) (SocialKind.instagram, instagram),
    if (tiktok.isNotEmpty) (SocialKind.tiktok, tiktok),
    if (facebook.isNotEmpty) (SocialKind.facebook, facebook),
  ];

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
    logoUrl: AppConfig.resolveUrl(j['logoImageUrl'] as String?),
    coverUrl: AppConfig.resolveUrl(j['coverImageUrl'] as String?),
    activeListingCount: (j['activeListingCount'] as num?)?.toInt() ?? 0,
    totalViewCount: (j['totalViewCount'] as num?)?.toInt() ?? 0,
    totalFavoriteCount: (j['totalFavoriteCount'] as num?)?.toInt() ?? 0,
  );
}

enum SocialKind { website, instagram, tiktok, facebook }

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
