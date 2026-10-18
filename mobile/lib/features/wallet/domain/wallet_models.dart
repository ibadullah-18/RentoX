import 'dart:math';

/// Prices charged by the backend (`ListingActivationPricingService`,
/// `ListingPromotionPricingService`). Shown to the user *before* paying;
/// the server remains the source of truth for the actual charge.
///
/// An admin can change the prices, so these are the last ones the server
/// told us ([apply], from `GET /api/tariffs`); until then, the starting ones.
abstract final class Pricing {
  static double activationFee = 1.0;
  static double vipPrice = 10.0;
  static double bumpPrice = 5.0;
  // Renewing costs the same as activating (free while the monthly free quota lasts).
  static double get renewalFee => activationFee;
  static int vipDays = 7;
  static const currency = 'AZN';

  static void apply(Map<String, dynamic> json) {
    activationFee =
        (json['activationFee'] as num?)?.toDouble() ?? activationFee;
    vipPrice = (json['vipPrice'] as num?)?.toDouble() ?? vipPrice;
    bumpPrice = (json['bumpPrice'] as num?)?.toDouble() ?? bumpPrice;
    vipDays = (json['vipDurationDays'] as num?)?.toInt() ?? vipDays;
  }
}

/// Mirrors the backend `ListingPromotionType` enum.
enum PromotionType {
  bump(1),
  vip(2);

  const PromotionType(this.id);
  final int id;
}

class WalletBalance {
  const WalletBalance({
    required this.balance,
    required this.currency,
    this.freeListingFee = false,
    this.freeVip = false,
    this.freeBump = false,
    this.freeStoreVip = false,
  });

  final double balance;
  final String currency;

  /// What RentoX pays for this person (the owner picks them one by one).
  final bool freeListingFee;
  final bool freeVip;
  final bool freeBump;
  final bool freeStoreVip;

  factory WalletBalance.fromJson(Map<String, dynamic> json) => WalletBalance(
    balance: (json['balance'] as num).toDouble(),
    currency: (json['currency'] as String?) ?? Pricing.currency,
    freeListingFee: (json['freeListingFee'] as bool?) ?? false,
    freeVip: (json['freeVip'] as bool?) ?? false,
    freeBump: (json['freeBump'] as bool?) ?? false,
    freeStoreVip: (json['freeStoreVip'] as bool?) ?? false,
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

/// Mirrors the backend `WalletTransactionType`.
enum WalletTxType { topUp, listingFee, promotionFee, refund, bonus, adjustment }

/// One line of the wallet history (`GET /api/wallet/transactions`).
class WalletTransaction {
  const WalletTransaction({
    required this.id,
    required this.isCredit,
    required this.type,
    required this.amount,
    required this.balanceAfter,
    required this.occurredAt,
  });

  final String id;

  /// Money came in (`true`) or went out.
  final bool isCredit;
  final WalletTxType type;
  final double amount;
  final double balanceAfter;
  final DateTime occurredAt;

  factory WalletTransaction.fromJson(Map<String, dynamic> json) =>
      WalletTransaction(
        id: json['id'].toString(),
        isCredit: json['direction'] == 1,
        type: switch (json['type']) {
          1 => WalletTxType.topUp,
          2 => WalletTxType.listingFee,
          3 => WalletTxType.promotionFee,
          4 => WalletTxType.refund,
          5 => WalletTxType.bonus,
          _ => WalletTxType.adjustment,
        },
        amount: (json['amount'] as num).toDouble(),
        balanceAfter: (json['balanceAfter'] as num).toDouble(),
        occurredAt: DateTime.parse(json['occurredAtUtc'] as String).toLocal(),
      );
}
