import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../../app/router.dart';
import '../../../core/l10n/app_localizations.dart';
import '../../../core/theme/app_colors.dart';
import '../../../core/theme/app_icons.dart';
import '../../../core/theme/app_spacing.dart';
import '../../auth/presentation/auth_controller.dart';
import '../../shell/tab_page.dart';
import '../../wallet/data/wallet_repository.dart';
import '../../wallet/presentation/payment_sheet.dart';

/// Account hub. Wallet and "my listings" are live; settings follow.
class ProfilePage extends ConsumerWidget {
  const ProfilePage({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final l10n = AppL10n.of(context);
    final theme = Theme.of(context);
    final locale = Localizations.localeOf(context).toString();
    final session = ref.watch(authControllerProvider).value;
    final wallet = ref.watch(walletBalanceProvider);

    return TabPage(
      onRefresh: () async {
        ref.invalidate(walletBalanceProvider);
        await ref.read(walletBalanceProvider.future);
      },
      slivers: [
        SliverPadding(
          padding: AppSpacing.screenPadding,
          sliver: SliverList.list(
            children: [
              const SizedBox(height: AppSpacing.md),
              Text(
                l10n.navProfile,
                style: theme.textTheme.headlineMedium?.copyWith(
                  fontWeight: FontWeight.w800,
                ),
              ),
              const SizedBox(height: AppSpacing.lg),
              Card(
                child: Padding(
                  padding: const EdgeInsets.all(AppSpacing.lg),
                  child: Row(
                    children: [
                      Container(
                        width: 56,
                        height: 56,
                        decoration: BoxDecoration(
                          color: AppColors.primarySoft,
                          borderRadius: BorderRadius.circular(AppRadius.lg),
                        ),
                        child: const Icon(
                          AppIcons.profile,
                          fill: 1,
                          size: 30,
                          color: AppColors.primary,
                        ),
                      ),
                      const SizedBox(width: AppSpacing.lg),
                      Expanded(
                        child: Text(
                          session?.phoneNumber ?? '',
                          style: const TextStyle(
                            fontWeight: FontWeight.w700,
                            fontSize: 17,
                          ),
                        ),
                      ),
                    ],
                  ),
                ),
              ),
              const SizedBox(height: AppSpacing.lg),
              Card(
                child: Column(
                  children: [
                    _MenuTile(
                      icon: AppIcons.listings,
                      title: l10n.myListingsTitle,
                      onTap: () => context.push(Routes.myListings),
                    ),
                    const Divider(indent: 56),
                    _MenuTile(
                      icon: AppIcons.wallet,
                      title: l10n.walletTitle,
                      trailingText: wallet.value == null
                          ? null
                          : formatMoney(locale, wallet.value!.balance),
                      onTap: () => showWalletSheet(context),
                    ),
                  ],
                ),
              ),
              const SizedBox(height: AppSpacing.xl),
              OutlinedButton.icon(
                onPressed: () =>
                    ref.read(authControllerProvider.notifier).signOut(),
                icon: const Icon(AppIcons.signOut),
                label: Text(l10n.signOut),
              ),
            ],
          ),
        ),
      ],
    );
  }
}

class _MenuTile extends StatelessWidget {
  const _MenuTile({
    required this.icon,
    required this.title,
    required this.onTap,
    this.trailingText,
  });

  final IconData icon;
  final String title;
  final String? trailingText;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    final scheme = Theme.of(context).colorScheme;
    return ListTile(
      onTap: onTap,
      contentPadding: const EdgeInsets.symmetric(horizontal: AppSpacing.lg),
      leading: Icon(icon, color: scheme.primary),
      title: Text(title, style: const TextStyle(fontWeight: FontWeight.w600)),
      trailing: Row(
        mainAxisSize: MainAxisSize.min,
        children: [
          if (trailingText != null)
            Text(
              trailingText!,
              style: TextStyle(
                fontWeight: FontWeight.w700,
                color: scheme.primary,
              ),
            ),
          const SizedBox(width: 4),
          Icon(AppIcons.forward, color: scheme.onSurfaceVariant),
        ],
      ),
    );
  }
}
