import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../../app/router.dart';
import '../../../core/l10n/app_localizations.dart';
import '../../../core/theme/app_colors.dart';
import '../../../core/theme/app_icons.dart';
import '../../../core/theme/app_spacing.dart';
import '../../../shared/formatting/time_labels.dart';
import '../../../shared/widgets/async_states.dart';
import '../../../shared/widgets/glass.dart';
import '../../../shared/widgets/motion.dart';
import '../domain/notification_models.dart';
import 'notifications_controller.dart';

/// Full-screen list of notifications (opened from the bell).
class NotificationsPage extends ConsumerStatefulWidget {
  const NotificationsPage({super.key});

  @override
  ConsumerState<NotificationsPage> createState() => _NotificationsPageState();
}

class _NotificationsPageState extends ConsumerState<NotificationsPage> {
  final _scroll = ScrollController();

  @override
  void initState() {
    super.initState();
    _scroll.addListener(() {
      if (_scroll.hasClients && _scroll.position.extentAfter < 600) {
        ref.read(notificationsProvider.notifier).loadMore();
      }
    });
  }

  @override
  void dispose() {
    _scroll.dispose();
    super.dispose();
  }

  void _back() {
    if (context.canPop()) {
      context.pop();
    } else {
      context.go(Routes.home);
    }
  }

  void _open(AppNotification n) {
    ref.read(notificationsProvider.notifier).markRead(n.id);
    final listingId = n.listingId;
    final ticketId = n.ticketId;
    if (listingId != null) {
      context.push(Routes.myListing(listingId));
    } else if (ticketId != null) {
      context.push(Routes.ticket(ticketId));
    }
  }

  @override
  Widget build(BuildContext context) {
    final l10n = AppL10n.of(context);
    final state = ref.watch(notificationsProvider);
    final padding = MediaQuery.paddingOf(context);
    const headerHeight = 68.0;
    final top = padding.top + headerHeight;
    final hasUnread = (state.value?.unread ?? 0) > 0;

    return Scaffold(
      body: Stack(
        children: [
          Positioned.fill(
            child: state.when(
              skipLoadingOnReload: true,
              loading: () => const Center(child: CircularProgressIndicator()),
              error: (e, _) => ErrorView(
                error: e,
                onRetry: () => ref.invalidate(notificationsProvider),
              ),
              data: (data) => RefreshIndicator(
                edgeOffset: top,
                onRefresh: () =>
                    ref.read(notificationsProvider.notifier).refresh(),
                child: data.items.isEmpty
                    ? ListView(
                        physics: const AlwaysScrollableScrollPhysics(),
                        children: [
                          SizedBox(height: top + 80),
                          EmptyView(
                            icon: AppIcons.bellOff,
                            title: l10n.notificationsEmpty,
                            message: l10n.notificationsEmptyHint,
                          ),
                        ],
                      )
                    : _List(
                        controller: _scroll,
                        top: top,
                        bottom: padding.bottom + AppSpacing.lg,
                        state: data,
                        onOpen: _open,
                      ),
              ),
            ),
          ),
          Positioned(
            top: 0,
            left: 0,
            right: 0,
            child: FrostedBar(
              height: headerHeight,
              child: Row(
                children: [
                  GlassIconButton(
                    icon: AppIcons.back,
                    semanticLabel: l10n.backAction,
                    size: 42,
                    onPressed: _back,
                  ),
                  const SizedBox(width: AppSpacing.md),
                  Expanded(
                    child: Text(
                      l10n.notifications,
                      style: Theme.of(context).textTheme.titleLarge
                          ?.copyWith(fontWeight: FontWeight.w800),
                    ),
                  ),
                  if (hasUnread)
                    TextButton.icon(
                      onPressed: () => ref
                          .read(notificationsProvider.notifier)
                          .markAllRead(),
                      icon: const Icon(AppIcons.read, size: 18),
                      label: Text(l10n.notificationsMarkAll),
                    ),
                ],
              ),
            ),
          ),
        ],
      ),
    );
  }
}

class _List extends StatelessWidget {
  const _List({
    required this.controller,
    required this.top,
    required this.bottom,
    required this.state,
    required this.onOpen,
  });

  final ScrollController controller;
  final double top;
  final double bottom;
  final NotificationsState state;
  final ValueChanged<AppNotification> onOpen;

  @override
  Widget build(BuildContext context) {
    final items = state.items;
    return ListView.builder(
      controller: controller,
      physics: const AlwaysScrollableScrollPhysics(),
      padding: EdgeInsets.fromLTRB(
        AppSpacing.lg,
        top + AppSpacing.sm,
        AppSpacing.lg,
        bottom,
      ),
      itemCount: items.length + (state.loadingMore ? 1 : 0),
      itemBuilder: (context, i) {
        if (i >= items.length) {
          return const Padding(
            padding: EdgeInsets.all(AppSpacing.lg),
            child: Center(
              child: SizedBox(
                width: 22,
                height: 22,
                child: CircularProgressIndicator(strokeWidth: 2.2),
              ),
            ),
          );
        }
        final n = items[i];
        final startsDay =
            i == 0 || !isSameDay(items[i - 1].createdAt, n.createdAt);
        return Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            if (startsDay)
              Padding(
                padding: EdgeInsets.only(
                  top: i == 0 ? 0 : AppSpacing.lg,
                  bottom: AppSpacing.sm,
                  left: 2,
                ),
                child: Text(
                  daySeparatorLabel(context, n.createdAt),
                  style: TextStyle(
                    fontSize: 13,
                    fontWeight: FontWeight.w700,
                    color: Theme.of(context).colorScheme.onSurfaceVariant,
                  ),
                ),
              ),
            Padding(
              padding: const EdgeInsets.only(bottom: AppSpacing.sm),
              child: Reveal(
                id: 'notification-${n.id}',
                index: i,
                child: PressScale(
                  scale: 0.98,
                  child: _NotificationTile(
                    notification: n,
                    onTap: () => onOpen(n),
                  ),
                ),
              ),
            ),
          ],
        );
      },
    );
  }
}

class _Look {
  const _Look(this.icon, this.color);
  final IconData icon;
  final Color color;
}

_Look _lookOf(NotificationKind kind) => switch (kind) {
  NotificationKind.listingApproved => const _Look(
    AppIcons.check,
    AppColors.success,
  ),
  NotificationKind.listingRejected => const _Look(
    AppIcons.error,
    AppColors.danger,
  ),
  NotificationKind.listingPaymentRequired => const _Look(
    AppIcons.wallet,
    AppColors.warning,
  ),
  NotificationKind.storeApproved => const _Look(
    AppIcons.store,
    AppColors.success,
  ),
  NotificationKind.storeRejected => const _Look(
    AppIcons.store,
    AppColors.danger,
  ),
  NotificationKind.reportUpdated => const _Look(
    AppIcons.shield,
    AppColors.primary,
  ),
  NotificationKind.supportReply => const _Look(
    AppIcons.support,
    AppColors.primary,
  ),
  NotificationKind.supportStatusChanged => const _Look(
    AppIcons.info,
    AppColors.primary,
  ),
  NotificationKind.other => const _Look(AppIcons.bell, AppColors.primary),
};

class _NotificationTile extends StatelessWidget {
  const _NotificationTile({required this.notification, required this.onTap});

  final AppNotification notification;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    final scheme = Theme.of(context).colorScheme;
    final look = _lookOf(notification.kind);
    final unread = !notification.isRead;
    final tappable =
        notification.listingId != null || notification.ticketId != null;

    return Material(
      color: unread ? scheme.primary.withValues(alpha: 0.07) : scheme.surface,
      borderRadius: BorderRadius.circular(AppRadius.xl),
      child: InkWell(
        borderRadius: BorderRadius.circular(AppRadius.xl),
        onTap: onTap,
        child: Container(
          padding: const EdgeInsets.all(AppSpacing.md),
          decoration: BoxDecoration(
            borderRadius: BorderRadius.circular(AppRadius.xl),
            border: Border.all(
              color: unread
                  ? scheme.primary.withValues(alpha: 0.25)
                  : scheme.outlineVariant,
            ),
          ),
          child: Row(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Container(
                width: 44,
                height: 44,
                decoration: BoxDecoration(
                  color: look.color.withValues(alpha: 0.14),
                  borderRadius: BorderRadius.circular(AppRadius.lg),
                ),
                child: Icon(look.icon, color: look.color, size: 24),
              ),
              const SizedBox(width: AppSpacing.md),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Row(
                      children: [
                        Expanded(
                          child: Text(
                            notification.title,
                            maxLines: 2,
                            overflow: TextOverflow.ellipsis,
                            style: TextStyle(
                              fontSize: 15,
                              fontWeight: unread
                                  ? FontWeight.w800
                                  : FontWeight.w600,
                            ),
                          ),
                        ),
                        if (unread) ...[
                          const SizedBox(width: 8),
                          Container(
                            width: 9,
                            height: 9,
                            decoration: const BoxDecoration(
                              color: AppColors.accent,
                              shape: BoxShape.circle,
                            ),
                          ),
                        ],
                      ],
                    ),
                    const SizedBox(height: 4),
                    Text(
                      notification.body,
                      maxLines: 4,
                      overflow: TextOverflow.ellipsis,
                      style: TextStyle(
                        fontSize: 14,
                        height: 1.35,
                        color: scheme.onSurfaceVariant,
                      ),
                    ),
                    const SizedBox(height: 8),
                    Row(
                      children: [
                        Text(
                          conversationTime(context, notification.createdAt),
                          style: TextStyle(
                            fontSize: 12,
                            color: scheme.onSurfaceVariant,
                          ),
                        ),
                        const Spacer(),
                        if (tappable)
                          Icon(
                            AppIcons.forward,
                            size: 18,
                            color: scheme.onSurfaceVariant,
                          ),
                      ],
                    ),
                  ],
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }
}
