import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:intl/intl.dart';

import '../../../app/router.dart';
import '../../../core/l10n/app_localizations.dart';
import '../../../core/theme/app_colors.dart';
import '../../../core/theme/app_icons.dart';
import '../../../core/theme/app_spacing.dart';
import '../../../shared/widgets/async_states.dart';
import '../../../shared/widgets/sub_page.dart';
import '../data/wallet_repository.dart';
import '../domain/wallet_models.dart';
import 'payment_sheet.dart';

/// Balance, top-up and the history of every payment.
class WalletPage extends ConsumerWidget {
  const WalletPage({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final l10n = AppL10n.of(context);
    final theme = Theme.of(context);
    final scheme = theme.colorScheme;
    final locale = Localizations.localeOf(context).toString();
    final balance = ref.watch(walletBalanceProvider);
    final history = ref.watch(walletHistoryProvider);
    final top = SubPage.topInset(context);

    Future<void> refresh() async {
      ref.invalidate(walletBalanceProvider);
      ref.invalidate(walletHistoryProvider);
      await ref.read(walletHistoryProvider.future);
    }

    final items = history.value?.items ?? const <WalletTransaction>[];
    final rows = _withDayHeaders(context, items);

    return SubPage(
      title: l10n.walletTitle,
      fallbackRoute: Routes.profile,
      child: RefreshIndicator(
        edgeOffset: top,
        onRefresh: refresh,
        child: ListView(
          physics: const AlwaysScrollableScrollPhysics(),
          padding: EdgeInsets.fromLTRB(
            AppSpacing.lg,
            top + AppSpacing.md,
            AppSpacing.lg,
            MediaQuery.paddingOf(context).bottom + AppSpacing.xl,
          ),
          children: [
            Container(
              padding: const EdgeInsets.all(AppSpacing.xl),
              decoration: BoxDecoration(
                color: scheme.primary,
                borderRadius: BorderRadius.circular(AppRadius.xl),
              ),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(
                    l10n.walletBalance,
                    style: TextStyle(
                      color: scheme.onPrimary.withValues(alpha: 0.8),
                      fontWeight: FontWeight.w600,
                    ),
                  ),
                  const SizedBox(height: 6),
                  Text(
                    balance.value == null
                        ? '—'
                        : formatMoney(locale, balance.value!.balance),
                    style: TextStyle(
                      color: scheme.onPrimary,
                      fontSize: 34,
                      fontWeight: FontWeight.w800,
                    ),
                  ),
                ],
              ),
            ),
            const SizedBox(height: AppSpacing.xl),
            Text(
              l10n.walletTopUpTitle,
              style: theme.textTheme.titleMedium?.copyWith(
                fontWeight: FontWeight.w800,
              ),
            ),
            const SizedBox(height: AppSpacing.md),
            const TopUpSection(),
            const SizedBox(height: 4),
            Text(
              l10n.demoNote,
              style: TextStyle(fontSize: 12.5, color: scheme.onSurfaceVariant),
            ),
            const SizedBox(height: AppSpacing.xl),
            Text(
              l10n.walletHistoryTitle,
              style: theme.textTheme.titleMedium?.copyWith(
                fontWeight: FontWeight.w800,
              ),
            ),
            const SizedBox(height: AppSpacing.md),
            if (history.isLoading && !history.hasValue)
              const Padding(
                padding: EdgeInsets.all(AppSpacing.xl),
                child: Center(child: CircularProgressIndicator()),
              )
            else if (history.hasError && !history.hasValue)
              ErrorView(
                error: history.error!,
                onRetry: () => ref.invalidate(walletHistoryProvider),
              )
            else if (items.isEmpty)
              EmptyView(
                icon: AppIcons.history,
                title: l10n.walletHistoryEmpty,
                message: l10n.walletHistoryEmptyHint,
              )
            else ...[
              for (final r in rows)
                switch (r) {
                  final String day => Padding(
                    padding: const EdgeInsets.only(top: 12, bottom: 6),
                    child: Text(
                      day,
                      style: TextStyle(
                        fontWeight: FontWeight.w700,
                        color: scheme.onSurfaceVariant,
                      ),
                    ),
                  ),
                  final WalletTransaction t => _TransactionTile(tx: t),
                  _ => const SizedBox.shrink(),
                },
              if (history.value!.hasMore)
                Padding(
                  padding: const EdgeInsets.only(top: AppSpacing.md),
                  child: OutlinedButton(
                    onPressed: () =>
                        ref.read(walletHistoryProvider.notifier).loadMore(),
                    child: Text(l10n.loadMore),
                  ),
                ),
            ],
          ],
        ),
      ),
    );
  }

  /// The transactions with a day heading in front of each new day.
  List<Object> _withDayHeaders(
    BuildContext context,
    List<WalletTransaction> items,
  ) {
    final l10n = AppL10n.of(context);
    final locale = Localizations.localeOf(context).toString();
    final now = DateTime.now();
    final today = DateTime(now.year, now.month, now.day);
    String label(DateTime d) {
      final day = DateTime(d.year, d.month, d.day);
      final diff = today.difference(day).inDays;
      if (diff == 0) return l10n.chatToday;
      if (diff == 1) return l10n.chatYesterday;
      return DateFormat.yMMMMd(locale).format(d);
    }

    final out = <Object>[];
    String? last;
    for (final t in items) {
      final l = label(t.occurredAt);
      if (l != last) {
        out.add(l);
        last = l;
      }
      out.add(t);
    }
    return out;
  }
}

String walletTxLabel(AppL10n l10n, WalletTxType t) => switch (t) {
  WalletTxType.topUp => l10n.txTopUp,
  WalletTxType.listingFee => l10n.txListingFee,
  WalletTxType.promotionFee => l10n.txPromotionFee,
  WalletTxType.refund => l10n.txRefund,
  WalletTxType.bonus => l10n.txBonus,
  WalletTxType.adjustment => l10n.txAdjustment,
};

class _TransactionTile extends StatelessWidget {
  const _TransactionTile({required this.tx});

  final WalletTransaction tx;

  @override
  Widget build(BuildContext context) {
    final l10n = AppL10n.of(context);
    final scheme = Theme.of(context).colorScheme;
    final locale = Localizations.localeOf(context).toString();
    final color = tx.isCredit ? AppColors.success : AppColors.danger;
    final icon = switch (tx.type) {
      WalletTxType.topUp => AppIcons.add,
      WalletTxType.promotionFee => AppIcons.vip,
      WalletTxType.listingFee => AppIcons.listings,
      _ => AppIcons.wallet,
    };
    final sign = tx.isCredit ? '+' : '−';
    return Padding(
      padding: const EdgeInsets.only(bottom: AppSpacing.sm),
      child: Card(
        margin: EdgeInsets.zero,
        child: Padding(
          padding: const EdgeInsets.symmetric(
            horizontal: AppSpacing.md,
            vertical: 12,
          ),
          child: Row(
            children: [
              Container(
                width: 42,
                height: 42,
                decoration: BoxDecoration(
                  color: color.withValues(alpha: 0.12),
                  borderRadius: BorderRadius.circular(AppRadius.md),
                ),
                child: Icon(icon, color: color, size: 22),
              ),
              const SizedBox(width: AppSpacing.md),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      walletTxLabel(l10n, tx.type),
                      style: const TextStyle(fontWeight: FontWeight.w700),
                    ),
                    const SizedBox(height: 2),
                    Text(
                      DateFormat.Hm(locale).format(tx.occurredAt),
                      style: TextStyle(
                        fontSize: 12.5,
                        color: scheme.onSurfaceVariant,
                      ),
                    ),
                  ],
                ),
              ),
              Column(
                crossAxisAlignment: CrossAxisAlignment.end,
                children: [
                  Text(
                    '$sign${formatMoney(locale, tx.amount)}',
                    style: TextStyle(fontWeight: FontWeight.w800, color: color),
                  ),
                  const SizedBox(height: 2),
                  Text(
                    l10n.walletAfter(formatMoney(locale, tx.balanceAfter)),
                    style: TextStyle(
                      fontSize: 12,
                      color: scheme.onSurfaceVariant,
                    ),
                  ),
                ],
              ),
            ],
          ),
        ),
      ),
    );
  }
}
