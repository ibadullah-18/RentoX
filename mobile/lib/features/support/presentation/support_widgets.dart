import 'package:flutter/material.dart';

import '../../../core/l10n/app_localizations.dart';
import '../../../core/theme/app_colors.dart';
import '../../../core/theme/app_icons.dart';
import '../../../core/theme/app_spacing.dart';
import '../domain/support_models.dart';

String supportCategoryLabel(AppL10n l10n, SupportCategory c) => switch (c) {
  SupportCategory.general => l10n.supportCatGeneral,
  SupportCategory.account => l10n.supportCatAccount,
  SupportCategory.listing => l10n.supportCatListing,
  SupportCategory.payment => l10n.supportCatPayment,
  SupportCategory.store => l10n.supportCatStore,
  SupportCategory.technical => l10n.supportCatTechnical,
  SupportCategory.other => l10n.supportCatOther,
};

String supportStatusLabel(AppL10n l10n, SupportStatus s) => switch (s) {
  SupportStatus.open => l10n.supportStOpen,
  SupportStatus.inProgress => l10n.supportStInProgress,
  SupportStatus.resolved => l10n.supportStResolved,
  SupportStatus.closed => l10n.supportStClosed,
};

Color supportStatusColor(SupportStatus s) => switch (s) {
  SupportStatus.open => AppColors.primary,
  SupportStatus.inProgress => AppColors.warningInk,
  SupportStatus.resolved => AppColors.successInk,
  SupportStatus.closed => AppColors.inkSecondary,
};

class SupportStatusChip extends StatelessWidget {
  const SupportStatusChip({super.key, required this.status});

  final SupportStatus status;

  @override
  Widget build(BuildContext context) {
    final color = supportStatusColor(status);
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 4),
      decoration: BoxDecoration(
        color: color.withValues(alpha: 0.12),
        borderRadius: BorderRadius.circular(AppRadius.sm),
      ),
      child: Text(
        supportStatusLabel(AppL10n.of(context), status),
        style: TextStyle(
          fontSize: 12.5,
          fontWeight: FontWeight.w700,
          color: color,
        ),
      ),
    );
  }
}

/// A quiet row under a listing or store: "Something wrong? Let us know."
/// Opens the support form with the topic and a reference already filled in.
class ReportProblemRow extends StatelessWidget {
  const ReportProblemRow({super.key, required this.hint, required this.onTap});

  final String hint;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    final l10n = AppL10n.of(context);
    final scheme = Theme.of(context).colorScheme;
    return InkWell(
      borderRadius: BorderRadius.circular(AppRadius.lg),
      onTap: onTap,
      child: Padding(
        padding: const EdgeInsets.symmetric(vertical: 12, horizontal: 4),
        child: Row(
          children: [
            Icon(AppIcons.report, size: 20, color: scheme.onSurfaceVariant),
            const SizedBox(width: AppSpacing.md),
            Expanded(
              child: Text(
                hint,
                style: TextStyle(
                  color: scheme.onSurfaceVariant,
                  fontSize: 13.5,
                ),
              ),
            ),
            Text(
              l10n.reportProblem,
              style: TextStyle(
                color: scheme.primary,
                fontWeight: FontWeight.w700,
                fontSize: 13.5,
              ),
            ),
          ],
        ),
      ),
    );
  }
}

/// `maxLength` characters of [text], so a long title still fits the 160
/// character subject limit.
String clip(String text, int maxLength) =>
    text.length <= maxLength ? text : '${text.substring(0, maxLength - 1)}…';
