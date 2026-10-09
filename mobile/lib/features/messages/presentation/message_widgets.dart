import 'package:cached_network_image/cached_network_image.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/config/app_config.dart';
import '../../../core/storage/token_storage.dart';
import '../../../core/theme/app_icons.dart';

/// Bearer header for private images (chat photos). Re-read whenever the
/// session changes.
final authHeadersProvider = FutureProvider.autoDispose<Map<String, String>>((
  ref,
) async {
  final tokens = await ref.watch(tokenStorageProvider).readTokens();
  return {if (tokens != null) 'Authorization': 'Bearer ${tokens.accessToken}'};
});

/// Round avatar of another user: their profile photo, or a soft placeholder.
class UserAvatar extends StatelessWidget {
  const UserAvatar({super.key, required this.userId, this.size = 48});

  final String userId;
  final double size;

  @override
  Widget build(BuildContext context) {
    final scheme = Theme.of(context).colorScheme;
    final placeholder = Container(
      width: size,
      height: size,
      color: scheme.primary.withValues(alpha: 0.12),
      child: Icon(AppIcons.person, size: size * 0.52, color: scheme.primary),
    );
    return ClipOval(
      child: SizedBox(
        width: size,
        height: size,
        child: CachedNetworkImage(
          imageUrl: '${AppConfig.apiBaseUrl}/api/users/$userId/profile-image',
          fit: BoxFit.cover,
          placeholder: (_, _) => placeholder,
          errorWidget: (_, _, _) => placeholder,
        ),
      ),
    );
  }
}
