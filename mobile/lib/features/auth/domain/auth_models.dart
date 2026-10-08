class AuthSession {
  const AuthSession({required this.userId, required this.phoneNumber});

  final String userId;
  final String phoneNumber;
}

class OtpChallenge {
  const OtpChallenge({
    required this.challengeId,
    required this.expiresAt,
    required this.resendAvailableAt,
  });

  final String challengeId;
  final DateTime expiresAt;
  final DateTime resendAvailableAt;

  factory OtpChallenge.fromJson(Map<String, dynamic> json) => OtpChallenge(
    challengeId: json['challengeId'] as String,
    expiresAt: DateTime.parse(json['expiresAtUtc'] as String),
    resendAvailableAt: DateTime.parse(json['resendAvailableAtUtc'] as String),
  );
}

/// State handed from the phone screen to the code screen.
class OtpFlowArgs {
  const OtpFlowArgs({
    required this.phone,
    required this.challenge,
    required this.isRegistration,
  });

  /// Normalised `+994XXXXXXXXX` form.
  final String phone;
  final OtpChallenge challenge;
  final bool isRegistration;
}

/// Preferred-language ids as defined by the backend enum.
enum PreferredLanguage {
  azerbaijani(1, 'az'),
  russian(2, 'ru'),
  english(3, 'en');

  const PreferredLanguage(this.id, this.code);
  final int id;
  final String code;

  static PreferredLanguage fromCode(String code) =>
      values.firstWhere((l) => l.code == code, orElse: () => azerbaijani);
}
