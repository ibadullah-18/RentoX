import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';

import '../../app/router.dart';
import '../../core/l10n/app_localizations.dart';
import '../../core/theme/app_icons.dart';
import '../../core/theme/app_spacing.dart';
import 'glass.dart';

/// Frame for full-screen pages opened from a tab: frosted header with a round
/// back button and a title, content scrolling underneath. Same look as the
/// notifications and chat pages.
class SubPage extends StatelessWidget {
  const SubPage({
    super.key,
    required this.title,
    required this.child,
    this.actions = const [],
    this.fallbackRoute = Routes.profile,
  });

  final String title;
  final Widget child;
  final List<Widget> actions;

  /// Where "back" goes when the page was opened directly (no history).
  final String fallbackRoute;

  static const headerHeight = 68.0;

  /// Space the content must leave free at the top for the header.
  static double topInset(BuildContext context) =>
      MediaQuery.paddingOf(context).top + headerHeight;

  @override
  Widget build(BuildContext context) {
    final l10n = AppL10n.of(context);
    return Scaffold(
      body: Stack(
        children: [
          Positioned.fill(child: child),
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
                    onPressed: () => context.canPop()
                        ? context.pop()
                        : context.go(fallbackRoute),
                  ),
                  const SizedBox(width: AppSpacing.md),
                  Expanded(
                    child: Text(
                      title,
                      maxLines: 1,
                      overflow: TextOverflow.ellipsis,
                      style: Theme.of(context).textTheme.titleLarge
                          ?.copyWith(fontWeight: FontWeight.w800),
                    ),
                  ),
                  ...actions,
                ],
              ),
            ),
          ),
        ],
      ),
    );
  }
}
