import 'dart:math';

/// Prices charged by the backend (`ListingActivationPricingService`,
/// `ListingPromotionPricingService`). Shown to the user *before* paying;
/// the server remains the source of truth for the actual charge.
abstract final class Pricing {
  static const activationFee = 1.0;
  static const vipPrice = 10.0;
  static const vipDays = 7;
  static const currency = 'AZN';
}

/// Mirrors the backend `ListingPromotionType` enum.
enum PromotionType {
  bump(1),
  vip(2);

  const PromotionType(this.id);
  final int id;
}

class WalletBalance {
  const WalletBalance({required this.balance, required this.currency});

  final double balance;
  final String currency;

  factory WalletBalance.fromJson(Map<String, dynamic> json) => WalletBalance(
    balance: (json['balance'] as num).toDouble(),
    currency: (json['currency'] as String?) ?? Pricing.currency,
  );
}

/// Result of a paid action (activation fee, promotion).
class PaymentResult {
  const PaymentResult({
    required this.chargedAmount,
    required this.remainingBalance,
    required this.wasAlreadyProcessed,
  });

  final double chargedAmount;
  final double remainingBalance;

  /// `true` when the same idempotency key was already used, i.e. the user was
  /// NOT charged a second time.
  final bool wasAlreadyProcessed;

  factory PaymentResult.fromJson(Map<String, dynamic> json) => PaymentResult(
    chargedAmount: (json['chargedAmount'] as num?)?.toDouble() ?? 0,
    remainingBalance: (json['remainingBalance'] as num?)?.toDouble() ?? 0,
    wasAlreadyProcessed: (json['wasAlreadyProcessed'] as bool?) ?? false,
  );
}

/// Random key for one logical payment attempt. Create it once per attempt and
/// reuse it for retries, so a repeated request can never charge twice.
String newIdempotencyKey([Random? random]) {
  final r = random ?? Random.secure();
  final bytes = List<int>.generate(16, (_) => r.nextInt(256));
  return 'rx-${bytes.map((b) => b.toRadixString(16).padLeft(2, '0')).join()}';
}
