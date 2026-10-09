import 'package:flutter/cupertino.dart';
import 'package:flutter/material.dart';

/// Soft page change used on every platform except iOS: the new page fades in
/// while rising a few pixels and settling from a hair's breadth smaller; the
/// page underneath fades back gently. No hard cut, no flash.
class SoftPageTransitionsBuilder extends PageTransitionsBuilder {
  const SoftPageTransitionsBuilder();

  @override
  Widget buildTransitions<T>(
    PageRoute<T> route,
    BuildContext context,
    Animation<double> animation,
    Animation<double> secondaryAnimation,
    Widget child,
  ) {
    if (MediaQuery.disableAnimationsOf(context)) return child;

    final enter = CurvedAnimation(
      parent: animation,
      curve: Curves.easeOutCubic,
      reverseCurve: Curves.easeInCubic,
    );
    final cover = CurvedAnimation(
      parent: secondaryAnimation,
      curve: Curves.easeOutCubic,
      reverseCurve: Curves.easeInCubic,
    );

    return FadeTransition(
      opacity: Tween<double>(begin: 1, end: 0.86).animate(cover),
      child: FadeTransition(
        opacity: enter,
        child: SlideTransition(
          position: Tween<Offset>(
            begin: const Offset(0, 0.035),
            end: Offset.zero,
          ).animate(enter),
          child: ScaleTransition(
            scale: Tween<double>(begin: 0.985, end: 1).animate(enter),
            child: child,
          ),
        ),
      ),
    );
  }
}

/// iOS keeps its native slide with the swipe-back gesture.
const appPageTransitions = PageTransitionsTheme(
  builders: {
    TargetPlatform.android: SoftPageTransitionsBuilder(),
    TargetPlatform.fuchsia: SoftPageTransitionsBuilder(),
    TargetPlatform.linux: SoftPageTransitionsBuilder(),
    TargetPlatform.macOS: CupertinoPageTransitionsBuilder(),
    TargetPlatform.windows: SoftPageTransitionsBuilder(),
    TargetPlatform.iOS: CupertinoPageTransitionsBuilder(),
  },
);
