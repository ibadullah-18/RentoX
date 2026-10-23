import 'package:cached_network_image/cached_network_image.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../core/branding/branding.dart';
import '../../core/theme/app_colors.dart';

/// The RentoX logo (the tag, "rentox" and the tagline). It is the picture that comes with the app until the owner
/// uploads their own in the admin panel; [size] is the old text size, kept so every place keeps its place
/// (the picture is a little taller than the text was). With "show the name as text" on, the name is written instead.
class RentoXLogo extends ConsumerWidget {
  const RentoXLogo({super.key, this.size = 28});

  final double size;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final dark = Theme.of(context).brightness == Brightness.dark;
    final branding = ref.watch(brandingProvider).value ?? const Branding();

    if (branding.useTextLogo) {
      return _TextLogo(size: size, name: branding.siteName, dark: dark);
    }

    // The owner's own height setting scales it (40 is the standard).
    final height =
        size * 1.3 * (branding.headerLogoHeight / 40).clamp(0.5, 2.4);
    final asset = Image.asset(
      dark ? 'assets/brand/logo_dark.png' : 'assets/brand/logo.png',
      height: height,
      fit: BoxFit.contain,
      semanticLabel: branding.siteName,
    );

    final url = branding.logoFor(dark: dark);
    if (url == null) return asset;
    return CachedNetworkImage(
      imageUrl: url,
      height: height,
      fit: BoxFit.contain,
      fadeInDuration: const Duration(milliseconds: 150),
      placeholder: (_, _) => SizedBox(height: height),
      errorWidget: (_, _, _) => asset,
    );
  }
}

class _TextLogo extends StatelessWidget {
  const _TextLogo({required this.size, required this.name, required this.dark});

  final double size;
  final String name;
  final bool dark;

  @override
  Widget build(BuildContext context) {
    final plain = name.toLowerCase() == 'rentox';
    final ink = dark ? AppColors.darkInk : AppColors.ink;
    return Semantics(
      label: name,
      child: ExcludeSemantics(
        child: Text.rich(
          plain
              ? TextSpan(
                  children: [
                    TextSpan(
                      text: 'rento',
                      style: TextStyle(color: ink),
                    ),
                    TextSpan(
                      text: 'x',
                      style: TextStyle(
                        color: dark ? AppColors.darkPrimary : AppColors.primary,
                      ),
                    ),
                  ],
                )
              : TextSpan(
                  text: name,
                  style: TextStyle(color: ink),
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
