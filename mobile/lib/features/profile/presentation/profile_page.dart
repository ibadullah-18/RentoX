import 'package:cached_network_image/cached_network_image.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../../app/router.dart';
import '../../../core/l10n/app_localizations.dart';
import '../../../shared/widgets/app_dialogs.dart';

import '../../../core/l10n/locale_controller.dart';
import '../../../core/theme/app_colors.dart';
import '../../../core/theme/app_icons.dart';
import '../../../core/theme/theme_controller.dart';
import '../../../core/theme/app_spacing.dart';
import '../../../shared/widgets/async_states.dart';
import '../../auth/presentation/auth_controller.dart';
import '../../notifications/presentation/notifications_controller.dart';
import '../../shell/tab_page.dart';
import '../../wallet/data/wallet_repository.dart';
import '../../wallet/presentation/payment_sheet.dart';
import '../../store/domain/store_models.dart';
import '../../store/presentation/my_store_page.dart';
import '../../store/presentation/store_controller.dart';
import '../domain/account_models.dart';
import 'account_controller.dart';
import 'delete_account_dialog.dart';
import 'language_sheet.dart';
import 'theme_sheet.dart';
import 'profile_avatar.dart';

/// Account hub: who you are, your listings and money, settings, sign out.
class ProfilePage extends ConsumerWidget {
  const ProfilePage({super.key});

  Future<void> _signOutEverywhere(BuildContext context, WidgetRef ref) async {
    final l10n = AppL10n.of(context);
    final messenger = ScaffoldMessenger.of(context);
    final ok = await showConfirmDialog(
      context,
      title: l10n.logoutAllTitle,
      message: l10n.logoutAllMessage,
      confirmLabel: l10n.logoutAllConfirm,
      destructive: true,
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
              if (myStore != null) ...[
                _StoreCard(store: myStore),
                const SizedBox(height: AppSpacing.sm),
                _AccountLine(
                  account: account,
                  phone: session?.phoneNumber ?? '',
                ),
              ] else
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
              const SizedBox(height: AppSpacing.md),
              _WalletCard(
                balance: wallet.value == null
                    ? null
                    : formatMoney(locale, wallet.value!.balance),
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
                    if (myStore == null) ...[
                      const Divider(indent: 56),
                      _MenuTile(
                        icon: AppIcons.store,
                        title: l10n.storeMine,
                        onTap: () => context.push(Routes.myStore),
                      ),
                    ],
                    const Divider(indent: 56),
                    _MenuTile(
                      icon: AppIcons.follow,
                      title: l10n.storeFollowing,
                      onTap: () => context.push(Routes.followedStores),
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
                      icon: AppIcons.appearance,
                      title: l10n.themeTitle,
                      trailingText: themeChoiceLabel(
                        l10n,
                        ref.watch(themePreferenceProvider).choice,
                      ),
                      onTap: () => showThemeSheet(context),
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
                      icon: AppIcons.info,
                      title: l10n.infoTitle,
                      onTap: () => context.push(Routes.info),
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

/// The user's store, shown in place of the personal card.
class _StoreCard extends StatelessWidget {
  const _StoreCard({required this.store});

  final MyStore store;

  @override
  Widget build(BuildContext context) {
    final l10n = AppL10n.of(context);
    final scheme = Theme.of(context).colorScheme;
    final statusColor = storeStatusColor(
      store.status,
      Theme.of(context).brightness,
    );
    final placeholder = ColoredBox(
      color: scheme.primary.withValues(alpha: 0.10),
      child: Icon(AppIcons.store, color: scheme.primary, size: 30),
    );
    Widget info(IconData icon, String text) => Padding(
      padding: const EdgeInsets.only(top: 6),
      child: Row(
        children: [
          Icon(icon, size: 16, color: scheme.onSurfaceVariant),
          const SizedBox(width: 6),
          Expanded(
            child: Text(
              text,
              maxLines: 1,
              overflow: TextOverflow.ellipsis,
              style: TextStyle(fontSize: 13, color: scheme.onSurfaceVariant),
            ),
          ),
        ],
      ),
    );
    return Card(
      clipBehavior: Clip.antiAlias,
      child: InkWell(
        onTap: () => context.push(Routes.myStore),
        child: Padding(
          padding: const EdgeInsets.all(AppSpacing.lg),
          child: Row(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              ClipRRect(
                borderRadius: BorderRadius.circular(AppRadius.lg),
                child: SizedBox(
                  width: 64,
                  height: 64,
                  child: store.logoUrl == null
                      ? placeholder
                      : CachedNetworkImage(
                          imageUrl: store.logoUrl!,
                          fit: BoxFit.cover,
                          placeholder: (_, _) => placeholder,
                          errorWidget: (_, _, _) => placeholder,
                        ),
                ),
              ),
              const SizedBox(width: AppSpacing.lg),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      store.name,
                      maxLines: 1,
                      overflow: TextOverflow.ellipsis,
                      style: const TextStyle(
                        fontWeight: FontWeight.w800,
                        fontSize: 18,
                      ),
                    ),
                    const SizedBox(height: 4),
                    Container(
                      padding: const EdgeInsets.symmetric(
                        horizontal: 10,
                        vertical: 3,
                      ),
                      decoration: BoxDecoration(
                        color: statusColor.withValues(alpha: 0.14),
                        borderRadius: BorderRadius.circular(AppRadius.pill),
                      ),
                      child: Text(
                        storeStatusLabel(l10n, store.status),
                        style: TextStyle(
                          fontSize: 12,
                          fontWeight: FontWeight.w700,
                          color: statusColor,
                        ),
                      ),
                    ),
                    if (store.description.isNotEmpty)
                      Padding(
                        padding: const EdgeInsets.only(top: 6),
                        child: Text(
                          store.description,
                          maxLines: 2,
                          overflow: TextOverflow.ellipsis,
                          style: TextStyle(
                            fontSize: 13.5,
                            color: scheme.onSurfaceVariant,
                          ),
                        ),
                      ),
                    if (store.phone.isNotEmpty)
                      info(AppIcons.call, store.phone),
                    if (store.address.isNotEmpty)
                      info(AppIcons.place, store.address),
                  ],
                ),
              ),
              Icon(AppIcons.forward, color: scheme.onSurfaceVariant),
            ],
          ),
        ),
      ),
    );
  }
}

/// Personal account in one slim line (shown under the store card).
class _AccountLine extends StatelessWidget {
  const _AccountLine({required this.account, required this.phone});

  final Account? account;
  final String phone;

  @override
  Widget build(BuildContext context) {
    final l10n = AppL10n.of(context);
    final scheme = Theme.of(context).colorScheme;
    final named = account?.hasName == true;
    return Card(
      clipBehavior: Clip.antiAlias,
      child: InkWell(
        onTap: () => context.push(Routes.profileEdit),
        child: Padding(
          padding: const EdgeInsets.symmetric(
            horizontal: AppSpacing.lg,
            vertical: AppSpacing.md,
          ),
          child: Row(
            children: [
              ProfileAvatar(account: account, size: 36),
              const SizedBox(width: AppSpacing.md),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      named ? account!.fullName : l10n.profileAddName,
                      maxLines: 1,
                      overflow: TextOverflow.ellipsis,
                      style: TextStyle(
                        fontWeight: FontWeight.w700,
                        color: named ? scheme.onSurface : scheme.primary,
                      ),
                    ),
                    Text(
                      phone,
                      style: TextStyle(
                        fontSize: 12.5,
                        color: scheme.onSurfaceVariant,
                      ),
                    ),
                  ],
                ),
              ),
              Icon(AppIcons.edit, size: 20, color: scheme.onSurfaceVariant),
            ],
          ),
        ),
      ),
    );
  }
}

/// Balance at a glance; opens the wallet page (top-up and history).
class _WalletCard extends StatelessWidget {
  const _WalletCard({required this.balance});

  final String? balance;

  @override
  Widget build(BuildContext context) {
    final l10n = AppL10n.of(context);
    final scheme = Theme.of(context).colorScheme;
    return Material(
      color: scheme.primary,
      borderRadius: BorderRadius.circular(AppRadius.xl),
      clipBehavior: Clip.antiAlias,
      child: InkWell(
        onTap: () => context.push(Routes.wallet),
        child: Padding(
          padding: const EdgeInsets.all(AppSpacing.lg),
          child: Row(
            children: [
              Container(
                width: 46,
                height: 46,
                decoration: BoxDecoration(
                  color: scheme.onPrimary.withValues(alpha: 0.18),
                  borderRadius: BorderRadius.circular(AppRadius.lg),
                ),
                child: Icon(AppIcons.wallet, color: scheme.onPrimary),
              ),
              const SizedBox(width: AppSpacing.md),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      l10n.walletTitle,
                      style: TextStyle(
                        color: scheme.onPrimary.withValues(alpha: 0.85),
                        fontWeight: FontWeight.w600,
                      ),
                    ),
                    Text(
                      balance ?? '—',
                      style: TextStyle(
                        color: scheme.onPrimary,
                        fontWeight: FontWeight.w800,
                        fontSize: 22,
                      ),
                    ),
                  ],
                ),
              ),
              Text(
                l10n.walletTopUpAndHistory,
                style: TextStyle(
                  color: scheme.onPrimary,
                  fontWeight: FontWeight.w700,
                  fontSize: 13,
                ),
              ),
              Icon(AppIcons.forward, color: scheme.onPrimary),
            ],
          ),
        ),
      ),
    );
  }
}
