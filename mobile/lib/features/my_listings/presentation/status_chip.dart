import 'package:flutter/material.dart';

import '../../../core/l10n/app_localizations.dart';
import '../../../core/theme/app_colors.dart';
import '../../../core/theme/app_spacing.dart';
import '../domain/owned_listing_models.dart';

String statusLabel(AppL10n l10n, ListingStatus s) => switch (s) {
  ListingStatus.draft => l10n.statusDraft,
  ListingStatus.pendingReview => l10n.statusPending,
  ListingStatus.active => l10n.statusActive,
  ListingStatus.rejected => l10n.statusRejected,
  ListingStatus.expired => l10n.statusExpired,
  ListingStatus.deactivated => l10n.statusDeactivated,
  ListingStatus.deleted => l10n.statusDeleted,
  ListingStatus.paymentRequired => l10n.statusPaymentRequired,
};

Color statusColor(ListingStatus s) => switch (s) {
  ListingStatus.active => AppColors.successInk,
  ListingStatus.pendingReview => AppColors.warningInk,
  ListingStatus.paymentRequired => AppColors.primary,
  ListingStatus.rejected => AppColors.danger,
  ListingStatus.draft ||
  ListingStatus.expired ||
  ListingStatus.deactivated ||
  ListingStatus.deleted => AppColors.inkSecondary,
};

class ListingStatusChip extends StatelessWidget {
  const ListingStatusChip({super.key, required this.status});

  final ListingStatus status;

  @override
  Widget build(BuildContext context) {
    final color = statusColor(status);
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 4),
      decoration: BoxDecoration(
        color: color.withValues(alpha: 0.12),
        borderRadius: BorderRadius.circular(AppRadius.sm),
      ),
      child: Text(
        statusLabel(AppL10n.of(context), status),
        style: TextStyle(
          fontSize: 12.5,
          fontWeight: FontWeight.w700,
          color: color,
        ),
      ),
    );
  }
}
