import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:intl/intl.dart';

import '../../../core/l10n/app_localizations.dart';
import '../../../core/network/api_exception.dart';
import '../../../core/theme/app_colors.dart';
import '../../../core/theme/app_icons.dart';
import '../../../core/theme/app_spacing.dart';
import '../data/wallet_repository.dart';
import '../domain/wallet_models.dart';

String formatMoney(String locale, double amount) =>
    '${NumberFormat.decimalPattern(locale).format(amount)} ${Pricing.currency}';

/// What the user is paying for.
enum PaymentKind { activation, vip, bump, renewal }

/// Asks for confirmation and runs [pay] (which performs the actual purchase).
/// Returns `true` when the payment went through.
///
/// [pay] receives a key that stays the same for every retry inside this
/// sheet, so pressing "Pay" twice (or retrying after a timeout) can never
/// charge the user twice.
Future<bool?> showPaymentSheet(
  BuildContext context, {
  required PaymentKind kind,
  required Future<PaymentResult> Function(String idempotencyKey) pay,
}) {
  return showModalBottomSheet<bool>(
    context: context,
    isScrollControlled: true,
    useSafeArea: true,
    showDragHandle: true,
    shape: const RoundedRectangleBorder(
      borderRadius: BorderRadius.vertical(top: Radius.circular(AppRadius.xl)),
    ),
    builder: (_) => _PaymentSheet(kind: kind, pay: pay),
  );
}

class _PaymentSheet extends ConsumerStatefulWidget {
  const _PaymentSheet({required this.kind, required this.pay});

  final PaymentKind kind;
  final Future<PaymentResult> Function(String key) pay;

  @override
  ConsumerState<_PaymentSheet> createState() => _PaymentSheetState();
}

class _PaymentSheetState extends ConsumerState<_PaymentSheet> {
  final String _key = newIdempotencyKey();
  bool _paying = false;
  bool _failed = false;

  double get _amount => switch (widget.kind) {
    PaymentKind.vip => Pricing.vipPrice,
    PaymentKind.bump => Pricing.bumpPrice,
    PaymentKind.renewal => Pricing.renewalFee,
    PaymentKind.activation => Pricing.activationFee,
  };

  Future<void> _pay() async {
    if (_paying) return;
    setState(() {
      _paying = true;
      _failed = false;
    });
    try {
      await widget.pay(_key);
      ref.invalidate(walletBalanceProvider);
      if (mounted) Navigator.of(context).pop(true);
    } catch (_) {
      ref.invalidate(walletBalanceProvider);
      if (mounted) {
        setState(() {
          _paying = false;
          _failed = true;
        });
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    final l10n = AppL10n.of(context);
    final theme = Theme.of(context);
    final scheme = theme.colorScheme;
    final locale = Localizations.localeOf(context).toString();
    final wallet = ref.watch(walletBalanceProvider);
    final balance = wallet.value?.balance;
    final enough = balance != null && balance >= _amount;

    final title = switch (widget.kind) {
      PaymentKind.vip => l10n.payForVip(Pricing.vipDays),
      PaymentKind.bump => l10n.payForBump,
      PaymentKind.renewal => l10n.payForRenewal,
      PaymentKind.activation => l10n.payForActivation,
    };

    return SingleChildScrollView(
      padding: EdgeInsets.fromLTRB(
        AppSpacing.lg,
        0,
        AppSpacing.lg,
        MediaQuery.viewInsetsOf(context).bottom + AppSpacing.xl,
      ),
      child: Column(
        mainAxisSize: MainAxisSize.min,
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text(
            l10n.paymentTitle,
            style: theme.textTheme.titleLarge?.copyWith(
              fontWeight: FontWeight.w800,
            ),
          ),
          const SizedBox(height: AppSpacing.lg),
          Card(
            child: Padding(
              padding: const EdgeInsets.all(AppSpacing.lg),
              child: Column(
                children: [
                  _Row(label: title, value: ''),
                  const Divider(height: 24),
                  _Row(
                    label: l10n.paymentAmount,
                    value: formatMoney(locale, _amount),
                    bold: true,
                  ),
                  const SizedBox(height: AppSpacing.sm),
                  _Row(
                    label: l10n.walletBalance,
                    value: wallet.isLoading
                        ? '…'
                        : (balance == null
                              ? '—'
                              : formatMoney(locale, balance)),
                    valueColor: wallet.hasValue && !enough
                        ? AppColors.danger
                        : null,
                  ),
                ],
              ),
            ),
          ),
          if (wallet.hasValue && !enough) ...[
            const SizedBox(height: AppSpacing.lg),
            Text(
              l10n.insufficientBalance,
              style: const TextStyle(
                color: AppColors.danger,
                fontWeight: FontWeight.w700,
              ),
            ),
            const SizedBox(height: AppSpacing.sm),
            TopUpSection(missing: _amount - (balance ?? 0)),
          ],
          if (_failed) ...[
            const SizedBox(height: AppSpacing.md),
            Text(
              l10n.paymentFailed,
              style: const TextStyle(color: AppColors.danger, fontSize: 13),
            ),
          ],
          const SizedBox(height: AppSpacing.lg),
          FilledButton(
            onPressed: (_paying || !enough) ? null : _pay,
            child: _paying
                ? const SizedBox(
                    width: 22,
                    height: 22,
                    child: CircularProgressIndicator(
                      strokeWidth: 2.4,
                      color: Colors.white,
                    ),
                  )
                : Text(l10n.payNow(formatMoney(locale, _amount))),
          ),
          const SizedBox(height: AppSpacing.sm),
          Center(
            child: Text(
              l10n.demoNote,
              style: TextStyle(fontSize: 12.5, color: scheme.onSurfaceVariant),
            ),
          ),
        ],
      ),
    );
  }
}

class _Row extends StatelessWidget {
  const _Row({
    required this.label,
    required this.value,
    this.bold = false,
    this.valueColor,
  });

  final String label;
  final String value;
  final bool bold;
  final Color? valueColor;

  @override
  Widget build(BuildContext context) {
    final scheme = Theme.of(context).colorScheme;
    return Row(
      children: [
        Expanded(
          child: Text(
            label,
            style: TextStyle(
              fontWeight: value.isEmpty ? FontWeight.w700 : FontWeight.w500,
              color: value.isEmpty ? scheme.onSurface : scheme.onSurfaceVariant,
            ),
          ),
        ),
        Text(
          value,
          style: TextStyle(
            fontWeight: bold ? FontWeight.w800 : FontWeight.w600,
            fontSize: bold ? 17 : 15,
            color: valueColor ?? scheme.onSurface,
          ),
        ),
      ],
    );
  }
}

/// Demo-only top-up (the backend refuses it outside Development).
class TopUpSection extends ConsumerStatefulWidget {
  const TopUpSection({super.key, this.missing = 0});

  /// How much is still needed; the quick amounts start from this.
  final double missing;

  @override
  ConsumerState<TopUpSection> createState() => _TopUpSectionState();
}

class _TopUpSectionState extends ConsumerState<TopUpSection> {
  static const _amounts = [10.0, 20.0, 50.0];
  double _selected = 20;
  String? _key; // reused until a top-up succeeds
  bool _busy = false;
  bool _failed = false;

  @override
  void initState() {
    super.initState();
    _selected = _amounts.firstWhere(
      (a) => a >= widget.missing,
      orElse: () => _amounts.last,
    );
  }

  Future<void> _topUp() async {
    if (_busy) return;
    final l10n = AppL10n.of(context);
    final messenger = ScaffoldMessenger.of(context);
    setState(() {
      _busy = true;
      _failed = false;
    });
    try {
      await ref
          .read(walletRepositoryProvider)
          .topUp(_selected, idempotencyKey: _key ??= newIdempotencyKey());
      _key = null;
      ref.invalidate(walletBalanceProvider);
      messenger.showSnackBar(SnackBar(content: Text(l10n.topUpDone)));
    } on ApiException {
      _failed = true;
    } catch (_) {
      _failed = true;
    } finally {
      if (mounted) setState(() => _busy = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    final l10n = AppL10n.of(context);
    final locale = Localizations.localeOf(context).toString();
    final scheme = Theme.of(context).colorScheme;

    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Wrap(
          spacing: AppSpacing.sm,
          children: [
            for (final a in _amounts)
              ChoiceChip(
                label: Text(formatMoney(locale, a)),
                selected: _selected == a,
                showCheckmark: false,
                onSelected: _busy ? null : (_) => setState(() => _selected = a),
              ),
          ],
        ),
        const SizedBox(height: AppSpacing.md),
        OutlinedButton.icon(
          onPressed: _busy ? null : _topUp,
          icon: _busy
              ? SizedBox(
                  width: 18,
                  height: 18,
                  child: CircularProgressIndicator(
                    strokeWidth: 2.2,
                    color: scheme.primary,
                  ),
                )
              : const Icon(AppIcons.wallet),
          label: Text(l10n.topUpDemo),
        ),
        if (_failed) ...[
          const SizedBox(height: 6),
          Text(
            l10n.actionFailed,
            style: const TextStyle(color: AppColors.danger, fontSize: 13),
          ),
        ],
      ],
    );
  }
}

/// Wallet overview (balance + demo top-up), opened from the profile page.
Future<void> showWalletSheet(BuildContext context) {
  return showModalBottomSheet<void>(
    context: context,
    // Opened from a tab: must sit above the floating navigation bar.
    useRootNavigator: true,
    isScrollControlled: true,
    useSafeArea: true,
    showDragHandle: true,
    shape: const RoundedRectangleBorder(
      borderRadius: BorderRadius.vertical(top: Radius.circular(AppRadius.xl)),
    ),
    builder: (_) => const _WalletSheet(),
  );
}

class _WalletSheet extends ConsumerWidget {
  const _WalletSheet();

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final l10n = AppL10n.of(context);
    final theme = Theme.of(context);
    final locale = Localizations.localeOf(context).toString();
    final wallet = ref.watch(walletBalanceProvider);

    return SingleChildScrollView(
      padding: EdgeInsets.fromLTRB(
        AppSpacing.lg,
        0,
        AppSpacing.lg,
        MediaQuery.viewInsetsOf(context).bottom + AppSpacing.xl,
      ),
      child: Column(
        mainAxisSize: MainAxisSize.min,
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text(
            l10n.walletTitle,
            style: theme.textTheme.titleLarge?.copyWith(
              fontWeight: FontWeight.w800,
            ),
          ),
          const SizedBox(height: AppSpacing.lg),
          Text(
            l10n.walletBalance,
            style: TextStyle(color: theme.colorScheme.onSurfaceVariant),
          ),
          const SizedBox(height: 4),
          Text(
            wallet.when(
              loading: () => '…',
              error: (_, _) => '—',
              data: (w) => w == null ? '—' : formatMoney(locale, w.balance),
            ),
            style: theme.textTheme.headlineMedium?.copyWith(
              fontWeight: FontWeight.w900,
              color: theme.colorScheme.primary,
            ),
          ),
          const SizedBox(height: AppSpacing.xl),
          const TopUpSection(),
          const SizedBox(height: AppSpacing.md),
          Text(
            l10n.demoNote,
            style: TextStyle(
              fontSize: 12.5,
              color: theme.colorScheme.onSurfaceVariant,
            ),
          ),
        ],
      ),
    );
  }
}
