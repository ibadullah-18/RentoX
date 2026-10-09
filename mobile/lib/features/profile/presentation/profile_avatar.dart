import 'package:cached_network_image/cached_network_image.dart';
import 'package:flutter/material.dart';

import '../../../core/theme/app_icons.dart';
import '../domain/account_models.dart';

/// The signed-in user's own avatar (round): their photo, or their initials,
/// or a soft person icon.
class ProfileAvatar extends StatelessWidget {
  const ProfileAvatar({super.key, required this.account, this.size = 64});

  final Account? account;
  final double size;

  String get _initials {
    final parts = (account?.fullName ?? '')
        .trim()
        .split(RegExp(r'\s+'))
        .where((p) => p.isNotEmpty)
        .toList();
    if (parts.isEmpty) return '';
    final first = parts.first.characters.first;
    final second = parts.length > 1 ? parts.last.characters.first : '';
    return (first + second).toUpperCase();
  }

  @override
  Widget build(BuildContext context) {
    final scheme = Theme.of(context).colorScheme;
    final fallback = Container(
      width: size,
      height: size,
      color: scheme.primary.withValues(alpha: 0.12),
      alignment: Alignment.center,
      child: _initials.isEmpty
          ? Icon(AppIcons.person, size: size * 0.5, color: scheme.primary)
          : Text(
              _initials,
              style: TextStyle(
                fontSize: size * 0.36,
                fontWeight: FontWeight.w800,
                color: scheme.primary,
              ),
            ),
    );

    final url = account?.photoUrl;
    return ClipOval(
      child: SizedBox(
        width: size,
        height: size,
        child: url == null
            ? fallback
            : CachedNetworkImage(
                key: ValueKey(url),
                imageUrl: url,
                fit: BoxFit.cover,
                placeholder: (_, _) => fallback,
                errorWidget: (_, _, _) => fallback,
              ),
      ),
    );
  }
}
