import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../app/router.dart';
import '../../core/l10n/app_localizations.dart';
import '../../core/theme/app_icons.dart';
import '../../shared/widgets/glass.dart';
import '../../shared/widgets/rentox_logo.dart';
import '../notifications/data/notifications_repository.dart';

/// Frosted header shared by every main tab: wordmark on the left, the
/// notifications bell on the right. It floats above the scroll view, so the
/// content blurs beneath it.
class AppHeader extends ConsumerWidget {
  const AppHeader({super.key});

  static const height = 70.0;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final l10n = AppL10n.of(context);
    final unread = ref.watch(unreadNotificationsProvider).value ?? 0;
    final wide = MediaQuery.sizeOf(context).width >= 900;
    return FrostedBar(
      height: height,
      child: Row(
        children: [
          if (!wide) const RentoXLogo(size: 32),
          const Spacer(),
          // Signed-out users are sent to sign in by the router guard.
          GlassIconButton(
            icon: AppIcons.bell,
            semanticLabel: l10n.notifications,
            showDot: unread > 0,
            onPressed: () => context.push(Routes.notifications),
          ),
        ],
      ),
    );
  }
}
