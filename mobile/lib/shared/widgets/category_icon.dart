import 'package:flutter/material.dart';

import '../../core/theme/app_icons.dart';

/// The icon of a category: the picture an admin linked to it (a web address),
/// else the icon they chose from the list, else one guessed from the slug.
/// A picture that cannot be loaded falls back to the guessed icon.
class CategoryIcon extends StatelessWidget {
  const CategoryIcon({
    super.key,
    required this.slug,
    this.iconUrl,
    this.size = 24,
    this.color,
  });

  final String slug;
  final String? iconUrl;
  final double size;
  final Color? color;

  static bool isLink(String? value) {
    final v = value?.trim().toLowerCase() ?? '';
    return v.startsWith('https://') || v.startsWith('http://');
  }

  @override
  Widget build(BuildContext context) {
    final fallback = Icon(
      AppIcons.categoryIcon(slug, iconKey: isLink(iconUrl) ? null : iconUrl),
      size: size,
      color: color,
    );
    if (!isLink(iconUrl)) return fallback;

    return SizedBox(
      width: size,
      height: size,
      child: Image.network(
        iconUrl!.trim(),
        fit: BoxFit.contain,
        errorBuilder: (_, _, _) => fallback,
      ),
    );
  }
}
