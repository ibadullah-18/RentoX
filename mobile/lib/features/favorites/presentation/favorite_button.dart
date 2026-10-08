import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../../app/router.dart';
import '../../../core/l10n/app_localizations.dart';
import '../../../core/theme/app_colors.dart';
import '../../../core/theme/app_icons.dart';
import '../../../core/theme/app_spacing.dart';
import '../../../shared/widgets/glass.dart';
import '../../auth/presentation/auth_controller.dart';
import 'favorites_controller.dart';

/// Glass heart used on every listing card. Tapping toggles the favourite
/// (optimistically); signed-out users are taken to sign in first.
class FavoriteButton extends ConsumerWidget {
  const FavoriteButton({
    super.key,
    required this.listingId,
    required this.isFavorite,
    this.diameter = 32,
  });

  final String listingId;

  /// The value the server sent with the listing.
  final bool isFavorite;

  /// Size of the glass circle (the touch target is always at least 44).
  final double diameter;

  Future<void> _onTap(BuildContext context, WidgetRef ref, bool active) async {
    final messenger = ScaffoldMessenger.of(context);
    final l10n = AppL10n.of(context);

    if (ref.read(authControllerProvider).value == null) {
      final from = GoRouterState.of(context).uri.toString();
      context.push(
        Uri(path: Routes.phone, queryParameters: {'from': from}).toString(),
      );
      return;
    }

    try {
      await ref
          .read(favoriteOverridesProvider.notifier)
          .toggle(listingId, current: active);
    } catch (_) {
      messenger
        ..hideCurrentSnackBar()
        ..showSnackBar(SnackBar(content: Text(l10n.favoriteFailed)));
    }
  }

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final l10n = AppL10n.of(context);
    final active = ref.watch(isFavoriteProvider((listingId, isFavorite)));

    return Semantics(
      button: true,
      toggled: active,
      label: active ? l10n.favoriteRemove : l10n.favoriteAdd,
      child: GestureDetector(
        onTap: () => _onTap(context, ref, active),
        behavior: HitTestBehavior.opaque,
        // 44px touch target around a 32px glass circle.
        child: SizedBox(
          width: diameter < 44 ? 44 : diameter,
          height: diameter < 44 ? 44 : diameter,
          child: Center(
            child: GlassSurface(
              radius: AppRadius.md,
              blur: 14,
              shadow: false,
              tintOpacity: 0.55,
              child: SizedBox(
                width: diameter,
                height: diameter,
                child: AnimatedSwitcher(
                  duration: const Duration(milliseconds: 220),
                  switchInCurve: Curves.easeOutBack,
                  transitionBuilder: (child, animation) =>
                      ScaleTransition(scale: animation, child: child),
                  child: Icon(
                    AppIcons.heart,
                    key: ValueKey(active),
                    size: diameter * 0.6,
                    fill: AppIcons.fillOf(active),
                    color: active ? AppColors.accent : AppColors.ink,
                  ),
                ),
              ),
            ),
          ),
        ),
      ),
    );
  }
}
