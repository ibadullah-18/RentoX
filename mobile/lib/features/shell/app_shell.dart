import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../app/router.dart';
import '../../core/l10n/app_localizations.dart';
import '../../core/theme/app_colors.dart';
import '../../core/theme/app_icons.dart';
import '../../core/theme/app_spacing.dart';
import '../../shared/widgets/glass.dart';
import '../../shared/widgets/motion.dart';
import '../../shared/widgets/rentox_logo.dart';
import '../messages/presentation/inbox_controller.dart';

class _NavItem {
  const _NavItem(this.icon, this.label, {this.badge = 0});

  final IconData icon;
  final String label;

  /// Unread counter shown on the icon (0 hides it).
  final int badge;
}

/// Adaptive navigation: floating glass bar on phones, side rail on wide
/// (web/tablet) screens.
class AppShell extends ConsumerWidget {
  const AppShell({super.key, required this.navigationShell});

  final StatefulNavigationShell navigationShell;

  static const _wideBreakpoint = 900.0;
  static const _createIndex = 2;

  /// The bar has 5 buttons but only 4 tab branches: the centre "+" opens the
  /// full-screen create flow instead of switching tabs.
  int get _selectedIndex {
    final branch = navigationShell.currentIndex;
    return branch >= _createIndex ? branch + 1 : branch;
  }

  void _go(BuildContext context, int index) {
    if (index == _createIndex) {
      context.push(Routes.create);
      return;
    }
    final branch = index > _createIndex ? index - 1 : index;
    navigationShell.goBranch(
      branch,
      initialLocation: branch == navigationShell.currentIndex,
    );
  }

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final l10n = AppL10n.of(context);
    final unreadMessages = ref.watch(unreadMessagesProvider).value ?? 0;
    final width = MediaQuery.sizeOf(context).width;

    final items = [
      _NavItem(AppIcons.home, l10n.navHome),
      _NavItem(AppIcons.heart, l10n.navFavorites),
      _NavItem(AppIcons.add, l10n.navCreate),
      _NavItem(AppIcons.messages, l10n.navMessages, badge: unreadMessages),
      _NavItem(AppIcons.profile, l10n.navProfile),
    ];

    if (width >= _wideBreakpoint) {
      return Scaffold(
        body: Row(
          children: [
            NavigationRail(
              extended: width >= 1200,
              selectedIndex: _selectedIndex,
              onDestinationSelected: (i) => _go(context, i),
              backgroundColor: Colors.transparent,
              leading: const Padding(
                padding: EdgeInsets.symmetric(vertical: 28, horizontal: 12),
                child: RentoXLogo(size: 26),
              ),
              destinations: [
                for (final item in items)
                  NavigationRailDestination(
                    icon: _Badged(count: item.badge, child: Icon(item.icon)),
                    selectedIcon: _Badged(
                      count: item.badge,
                      child: Icon(item.icon, fill: 1),
                    ),
                    label: Text(item.label),
                  ),
              ],
            ),
            const VerticalDivider(width: 1),
            Expanded(
              child: Center(
                child: ConstrainedBox(
                  constraints: const BoxConstraints(maxWidth: 1200),
                  child: navigationShell,
                ),
              ),
            ),
          ],
        ),
      );
    }

    return Scaffold(
      extendBody: true,
      body: navigationShell,
      bottomNavigationBar: SafeArea(
        top: false,
        child: Padding(
          padding: const EdgeInsets.fromLTRB(16, 0, 16, 10),
          child: _GlassNavBar(
            items: items,
            currentIndex: _selectedIndex,
            onSelected: (i) => _go(context, i),
          ),
        ),
      ),
    );
  }
}

class _GlassNavBar extends StatelessWidget {
  const _GlassNavBar({
    required this.items,
    required this.currentIndex,
    required this.onSelected,
  });

  final List<_NavItem> items;
  final int currentIndex;
  final ValueChanged<int> onSelected;

  @override
  Widget build(BuildContext context) {
    return GlassSurface(
      radius: AppRadius.xl,
      padding: const EdgeInsets.symmetric(horizontal: 6, vertical: 8),
      child: Row(
        children: [
          for (var i = 0; i < items.length; i++)
            Expanded(
              child: i == AppShell._createIndex
                  ? _CreateButton(
                      label: items[i].label,
                      icon: items[i].icon,
                      onTap: () => onSelected(i),
                    )
                  : _NavButton(
                      item: items[i],
                      selected: i == currentIndex,
                      onTap: () => onSelected(i),
                    ),
            ),
        ],
      ),
    );
  }
}

class _NavButton extends StatelessWidget {
  const _NavButton({
    required this.item,
    required this.selected,
    required this.onTap,
  });

  final _NavItem item;
  final bool selected;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    final scheme = Theme.of(context).colorScheme;
    final color = selected ? scheme.primary : scheme.onSurfaceVariant;

    return Semantics(
      button: true,
      selected: selected,
      label: item.label,
      child: PressScale(
        scale: 0.92,
        child: GestureDetector(
          onTap: onTap,
          behavior: HitTestBehavior.opaque,
          child: Padding(
            padding: const EdgeInsets.symmetric(vertical: 2),
            child: Column(
              mainAxisSize: MainAxisSize.min,
              children: [
                AnimatedContainer(
                  duration: const Duration(milliseconds: 220),
                  curve: Curves.easeOutCubic,
                  padding: const EdgeInsets.symmetric(
                    horizontal: 16,
                    vertical: 5,
                  ),
                  decoration: BoxDecoration(
                    color: selected
                        ? scheme.primary.withValues(alpha: 0.12)
                        : Colors.transparent,
                    borderRadius: BorderRadius.circular(AppRadius.md),
                  ),
                  child: AnimatedScale(
                    scale: selected ? 1.1 : 1,
                    duration: const Duration(milliseconds: 280),
                    curve: Curves.easeOutBack,
                    child: _Badged(
                      count: item.badge,
                      child: Icon(
                        item.icon,
                        size: 25,
                        fill: AppIcons.fillOf(selected),
                        color: color,
                      ),
                    ),
                  ),
                ),
                const SizedBox(height: 2),
                Text(
                  item.label,
                  maxLines: 1,
                  overflow: TextOverflow.ellipsis,
                  style: TextStyle(
                    fontSize: 10.5,
                    fontWeight: selected ? FontWeight.w700 : FontWeight.w500,
                    color: color,
                  ),
                ),
              ],
            ),
          ),
        ),
      ),
    );
  }
}

/// Small counter pinned to the top-right of a nav icon.
class _Badged extends StatelessWidget {
  const _Badged({required this.count, required this.child});

  final int count;
  final Widget child;

  @override
  Widget build(BuildContext context) {
    if (count <= 0) return child;
    return Stack(
      clipBehavior: Clip.none,
      children: [
        child,
        Positioned(
          top: -5,
          right: -9,
          child: Container(
            constraints: const BoxConstraints(minWidth: 17, minHeight: 17),
            padding: const EdgeInsets.symmetric(horizontal: 4),
            alignment: Alignment.center,
            decoration: BoxDecoration(
              color: AppColors.accent,
              borderRadius: BorderRadius.circular(AppRadius.sm + 1),
              border: Border.all(
                color: Theme.of(context).colorScheme.surface,
                width: 1.5,
              ),
            ),
            child: Text(
              count > 99 ? '99+' : '$count',
              style: const TextStyle(
                color: Colors.white,
                fontSize: 10,
                fontWeight: FontWeight.w800,
                height: 1.1,
              ),
            ),
          ),
        ),
      ],
    );
  }
}

class _CreateButton extends StatelessWidget {
  const _CreateButton({
    required this.label,
    required this.icon,
    required this.onTap,
  });

  final String label;
  final IconData icon;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    return Semantics(
      button: true,
      label: label,
      child: GestureDetector(
        onTap: onTap,
        behavior: HitTestBehavior.opaque,
        // heightFactor keeps the bar compact (a bare Center would fill the screen).
        child: Center(
          heightFactor: 1,
          child: Container(
            width: 52,
            height: 52,
            decoration: BoxDecoration(
              borderRadius: BorderRadius.circular(AppRadius.lg),
              gradient: const LinearGradient(
                begin: Alignment.topLeft,
                end: Alignment.bottomRight,
                colors: [Color(0xFF7A6DFF), AppColors.primary],
              ),
              boxShadow: [
                BoxShadow(
                  color: AppColors.primary.withValues(alpha: 0.38),
                  blurRadius: 16,
                  offset: const Offset(0, 6),
                ),
              ],
            ),
            child: Icon(icon, color: Colors.white, size: 26, weight: 500),
          ),
        ),
      ),
    );
  }
}
