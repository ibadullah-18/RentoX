import 'package:flutter/material.dart';

import '../../core/theme/app_colors.dart';

/// The RentoX wordmark: lowercase "rento" + brand-coloured "x".
/// Scales with [size]; at 42 it matches the brand spec exactly
/// (w900, letter-spacing -2, height 1).
class RentoXLogo extends StatelessWidget {
  const RentoXLogo({super.key, this.size = 28});

  final double size;

  @override
  Widget build(BuildContext context) {
    final dark = Theme.of(context).brightness == Brightness.dark;
    return Semantics(
      label: 'RentoX',
      child: ExcludeSemantics(
        child: Text.rich(
          TextSpan(
            children: [
              TextSpan(
                text: 'rento',
                style: TextStyle(
                  color: dark ? AppColors.darkInk : AppColors.ink,
                ),
              ),
              TextSpan(
                text: 'x',
                style: TextStyle(
                  color: dark ? AppColors.darkPrimary : AppColors.primary,
                ),
              ),
            ],
          ),
          style: TextStyle(
            fontSize: size,
            fontWeight: FontWeight.w900,
            letterSpacing: -size * (2 / 42),
            height: 1,
          ),
        ),
      ),
    );
  }
}

/// The price-tag brand mark (outline version, transparent background).
class RentoXMark extends StatelessWidget {
  const RentoXMark({super.key, this.size = 64});

  final double size;

  @override
  Widget build(BuildContext context) => Image.asset(
    'assets/brand/tag_mark.png',
    width: size,
    height: size,
    semanticLabel: 'RentoX',
  );
}
