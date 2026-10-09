import 'dart:ui';

import 'package:flutter/material.dart';

import '../../core/theme/app_colors.dart';
import '../../core/theme/app_spacing.dart';
import 'motion.dart';

/// iOS-style frosted glass: blurs whatever scrolls beneath it, with a
/// translucent tint and a hairline highlight. Use for bars, pills and chips.
class GlassSurface extends StatelessWidget {
  const GlassSurface({
    super.key,
    required this.child,
    this.radius = AppRadius.xl,
    this.padding,
    this.blur = 22,
    this.tintOpacity,
    this.shadow = true,
  });

  final Widget child;
  final double radius;
  final EdgeInsetsGeometry? padding;
  final double blur;
  final double? tintOpacity;
  final bool shadow;

  @override
  Widget build(BuildContext context) {
    final dark = Theme.of(context).brightness == Brightness.dark;
    final tint = dark ? AppColors.darkSurface : Colors.white;
    final borderRadius = BorderRadius.circular(radius);

    return DecoratedBox(
      decoration: BoxDecoration(
        borderRadius: borderRadius,
        boxShadow: shadow
            ? [
                BoxShadow(
                  color: (dark ? Colors.black : AppColors.primary).withValues(
                    alpha: dark ? 0.35 : 0.10,
                  ),
                  blurRadius: 28,
                  offset: const Offset(0, 10),
                ),
              ]
            : null,
      ),
      child: ClipRRect(
        borderRadius: borderRadius,
        child: BackdropFilter(
          filter: ImageFilter.blur(sigmaX: blur, sigmaY: blur),
          child: DecoratedBox(
            decoration: BoxDecoration(
              color: tint.withValues(
                alpha: tintOpacity ?? (dark ? 0.60 : 0.70),
              ),
              borderRadius: borderRadius,
              border: Border.all(
                color: Colors.white.withValues(alpha: dark ? 0.08 : 0.75),
              ),
            ),
            child: padding == null
                ? child
                : Padding(padding: padding!, child: child),
          ),
        ),
      ),
    );
  }
}

/// Round glass button (back, bell, filter...). Very transparent, iOS style.
///
/// Set [onPhoto] when the button sits on top of a picture: it then has no
/// background at all, just a crisp white icon with a soft shadow, so it
/// reads on any photo. An optional unread dot is available.
class GlassIconButton extends StatelessWidget {
  const GlassIconButton({
    super.key,
    required this.icon,
    required this.onPressed,
    required this.semanticLabel,
    this.size = 46,
    this.showDot = false,
    this.onPhoto = false,
    this.child,
  });

  final IconData icon;
  final VoidCallback onPressed;
  final String semanticLabel;
  final double size;
  final bool showDot;
  final bool onPhoto;

  /// Replaces the icon (e.g. an avatar image).
  final Widget? child;

  @override
  Widget build(BuildContext context) {
    final scheme = Theme.of(context).colorScheme;
    final dark = Theme.of(context).brightness == Brightness.dark;

    final Widget face = onPhoto
        ? Center(child: PhotoIcon(icon, size: size * 0.55))
        : GlassSurface(
            radius: size / 2,
            shadow: false,
            blur: 18,
            tintOpacity: dark ? 0.32 : 0.42,
            child: Center(
              child:
                  child ??
                  Icon(icon, size: size * 0.5, color: scheme.onSurface),
            ),
          );

    return Semantics(
      button: true,
      label: semanticLabel,
      child: PressScale(
        scale: 0.9,
        child: GestureDetector(
          onTap: onPressed,
          behavior: HitTestBehavior.opaque,
          child: SizedBox(
            width: size,
            height: size,
            child: Stack(
              clipBehavior: Clip.none,
              children: [
                Positioned.fill(child: face),
                if (showDot)
                  Positioned(
                    top: 9,
                    right: 10,
                    child: Container(
                      width: 9,
                      height: 9,
                      decoration: BoxDecoration(
                        color: AppColors.accent,
                        shape: BoxShape.circle,
                        border: Border.all(color: scheme.surface, width: 1.5),
                      ),
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

/// A white icon with a soft shadow, for use directly on top of photos.
class PhotoIcon extends StatelessWidget {
  const PhotoIcon(
    this.icon, {
    super.key,
    this.size = 26,
    this.color = Colors.white,
    this.fill = 0,
  });

  final IconData icon;
  final double size;
  final Color color;
  final double fill;

  @override
  Widget build(BuildContext context) {
    return Icon(
      icon,
      size: size,
      color: color,
      fill: fill,
      weight: 400,
      shadows: const [
        Shadow(color: Color(0x80000000), blurRadius: 8),
        Shadow(color: Color(0x40000000), blurRadius: 2, offset: Offset(0, 1)),
      ],
    );
  }
}

/// Full-width frosted bar (blurs what scrolls under it). Pin it above a
/// scroll view with a `Stack`; its total height is
/// `MediaQuery.paddingOf(context).top + height`.
class FrostedBar extends StatelessWidget {
  const FrostedBar({super.key, required this.child, this.height = 70});

  final Widget child;
  final double height;

  @override
  Widget build(BuildContext context) {
    final background = Theme.of(context).scaffoldBackgroundColor;
    return ClipRect(
      child: BackdropFilter(
        filter: ImageFilter.blur(sigmaX: 22, sigmaY: 22),
        child: ColoredBox(
          color: background.withValues(alpha: 0.72),
          child: SafeArea(
            bottom: false,
            child: SizedBox(
              height: height,
              child: Padding(
                padding: const EdgeInsets.symmetric(horizontal: 16),
                child: child,
              ),
            ),
          ),
        ),
      ),
    );
  }
}

/// A faint dark fade along the top of a photo, so white icons placed there
/// stay readable on light pictures. Use inside a `Stack`.
class PhotoTopScrim extends StatelessWidget {
  const PhotoTopScrim({super.key, this.height = 64});

  final double height;

  @override
  Widget build(BuildContext context) {
    return Positioned(
      top: 0,
      left: 0,
      right: 0,
      height: height,
      child: const IgnorePointer(
        child: DecoratedBox(
          decoration: BoxDecoration(
            gradient: LinearGradient(
              begin: Alignment.topCenter,
              end: Alignment.bottomCenter,
              colors: [Color(0x59000000), Color(0x00000000)],
            ),
          ),
        ),
      ),
    );
  }
}
