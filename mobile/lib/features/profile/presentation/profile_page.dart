import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../../app/router.dart';
import '../../../core/l10n/app_localizations.dart';
import '../../../core/l10n/locale_controller.dart';
import '../../../core/theme/app_colors.dart';
import '../../../core/theme/app_icons.dart';
import '../../../core/theme/app_spacing.dart';
import '../../../shared/widgets/async_states.dart';
import '../../auth/presentation/auth_controller.dart';
import '../../notifications/presentation/notifications_controller.dart';
import '../../shell/tab_page.dart';
import '../../wallet/data/wallet_repository.dart';
import '../../wallet/presentation/payment_sheet.dart';
import '../../store/presentation/my_store_page.dart';
import '../../store/presentation/store_controller.dart';
import 'account_controller.dart';
import 'delete_account_dialog.dart';
import 'language_sheet.dart';
import 'profile_avatar.dart';

/// Account hub: who you are, your listings and money, settings, sign out.
class ProfilePage extends ConsumerWidget {
  const ProfilePage({super.key});

  Future<void> _signOutEverywhere(BuildContext context, WidgetRef ref) async {
    final l10n = AppL10n.of(context);
    final messenger = ScaffoldMessenger.of(context);
    final ok = await showDialog<bool>(
      context: context,
      builder: (context) => AlertDialog(
        title: Text(l10n.logoutAllTitle),
        content: Text(l10n.logoutAllMessage),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(context, false),
            child: Text(l10n.cancelAction),
          ),
          TextButton(
            onPressed: () => Navigator.pop(context, true),
            child: Text(
              l10n.logoutAllConfirm,
              style: const TextStyle(color: AppColors.danger),
            ),
          ),
        ],
      ),
    );
    if (ok != true) return;
    try {
      await ref.read(authControllerProvider.notifier).signOutEverywhere();
    } catch (e) {
      if (!context.mounted) return;
      messenger.showSnackBar(SnackBar(content: Text(errorMessage(context, e))));
    }
  }

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final l10n = AppL10n.of(context);
    final theme = Theme.of(context);
    final scheme = theme.colorScheme;
    final locale = Localizations.localeOf(context).toString();
    final session = ref.watch(authControllerProvider).value;
    final account = ref.watch(accountProvider).value;
    final wallet = ref.watch(walletBalanceProvider);
    final myStore = ref.watch(myStoreProvider).value;
    final unread = ref.watch(unreadNotificationsProvider).value ?? 0;
    final language = ref.watch(localeProvider).languageCode;

    return TabPage(
      onRefresh: () async {
        ref.invalidate(walletBalanceProvider);
        ref.invalidate(accountProvider);
        ref.invalidate(myStoreProvider);
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
                clipBehavior: Clip.antiAlias,
                child: InkWell(
                  onTap: () => context.push(Routes.profileEdit),
                  child: Padding(
                    padding: const EdgeInsets.all(AppSpacing.lg),
                    child: Row(
                      children: [
                        ProfileAvatar(account: account, size: 64),
                        const SizedBox(width: AppSpacing.lg),
                        Expanded(
                          child: Column(
                            crossAxisAlignment: CrossAxisAlignment.start,
                            children: [
                              Text(
                                account?.hasName == true
                                    ? account!.fullName
                                    : l10n.profileAddName,
                                maxLines: 1,
                                overflow: TextOverflow.ellipsis,
                                style: TextStyle(
                                  fontWeight: FontWeight.w800,
                                  fontSize: 18,
                                  color: account?.hasName == true
                                      ? scheme.onSurface
                                      : scheme.primary,
                                ),
                              ),
                              const SizedBox(height: 2),
                              Text(
                                session?.phoneNumber ?? '',
                                style: TextStyle(
                                  color: scheme.onSurfaceVariant,
                                ),
                              ),
                            ],
                          ),
                        ),
                        Icon(AppIcons.edit, color: scheme.onSurfaceVariant),
                      ],
                    ),
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
                      icon: AppIcons.store,
                      title: l10n.storeMine,
                      trailingText: myStore == null
                          ? null
                          : storeStatusLabel(l10n, myStore.status),
                      onTap: () => context.push(Routes.myStore),
                    ),
                    const Divider(indent: 56),
                    _MenuTile(
                      icon: AppIcons.follow,
                      title: l10n.storeFollowing,
                      onTap: () => context.push(Routes.followedStores),
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
              const SizedBox(height: AppSpacing.lg),
              Card(
                child: Column(
                  children: [
                    _MenuTile(
                      icon: AppIcons.bell,
                      title: l10n.notifications,
                      trailingText: unread > 0 ? '$unread' : null,
                      onTap: () => context.push(Routes.notifications),
                    ),
                    const Divider(indent: 56),
                    _MenuTile(
                      icon: AppIcons.language,
                      title: l10n.languageTitle,
                      trailingText: languageName(language),
                      onTap: () => showLanguageSheet(context),
                    ),
                    const Divider(indent: 56),
                    _MenuTile(
                      icon: AppIcons.support,
                      title: l10n.supportTitle,
                      onTap: () => context.push(Routes.support),
                    ),
                    const Divider(indent: 56),
                    _MenuTile(
                      icon: AppIcons.devices,
                      title: l10n.logoutAllTitle,
                      onTap: () => _signOutEverywhere(context, ref),
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
              const SizedBox(height: AppSpacing.sm),
              TextButton(
                onPressed: () => confirmAndDeleteAccount(context),
                child: Text(
                  l10n.deleteAccount,
                  style: const TextStyle(color: AppColors.danger),
                ),
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
