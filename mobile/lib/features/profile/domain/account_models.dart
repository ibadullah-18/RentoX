import '../../../core/config/app_config.dart';
import '../../auth/domain/auth_models.dart';

/// Limits enforced by the backend (`UserProfile`, `ProfileImageService`).
abstract final class AccountRules {
  static const nameMin = 2;
  static const nameMax = 100;
  static const bioMax = 500;
  static const photoMaxBytes = 5 * 1024 * 1024;
}

class Account {
  const Account({
    required this.userId,
    required this.phoneNumber,
    required this.fullName,
    required this.bio,
    required this.language,
    this.profileImagePath,
    this.photoVersion = 0,
  });

  final String userId;
  final String phoneNumber;
  final String fullName;
  final String bio;
  final PreferredLanguage language;

  /// Relative API path of the photo, or null when there is none.
  final String? profileImagePath;

  /// Bumped on every upload/removal. The photo URL never changes, so it is
  /// appended to the URL to defeat image caches.
  final int photoVersion;

  String? get photoUrl {
    final path = profileImagePath;
    if (path == null || path.isEmpty) return null;
    final base = AppConfig.resolveUrl(path)!;
    return photoVersion == 0 ? base : '$base?v=$photoVersion';
  }

  bool get hasName => fullName.trim().isNotEmpty;

  Account copyWith({
    String? fullName,
    String? bio,
    PreferredLanguage? language,
    String? profileImagePath,
    bool clearPhoto = false,
    int? photoVersion,
  }) => Account(
    userId: userId,
    phoneNumber: phoneNumber,
    fullName: fullName ?? this.fullName,
    bio: bio ?? this.bio,
    language: language ?? this.language,
    profileImagePath: clearPhoto
        ? null
        : (profileImagePath ?? this.profileImagePath),
    photoVersion: photoVersion ?? this.photoVersion,
  );

  factory Account.fromJson(Map<String, dynamic> json) {
    final langId = (json['preferredLanguage'] as num?)?.toInt() ?? 1;
    return Account(
      userId: json['userId'] as String,
      phoneNumber: (json['phoneNumber'] as String?) ?? '',
      fullName: (json['fullName'] as String?) ?? '',
      bio: (json['bio'] as String?) ?? '',
      language: PreferredLanguage.values.firstWhere(
        (l) => l.id == langId,
        orElse: () => PreferredLanguage.azerbaijani,
      ),
      profileImagePath: json['profileImageUrl'] as String?,
    );
  }
}

enum NameIssue { empty, tooShort, tooLong }

/// Client-side check mirroring the backend rule (2-100 characters).
NameIssue? validateName(String raw) {
  final name = raw.trim();
  if (name.isEmpty) return NameIssue.empty;
  if (name.length < AccountRules.nameMin) return NameIssue.tooShort;
  if (name.length > AccountRules.nameMax) return NameIssue.tooLong;
  return null;
}
