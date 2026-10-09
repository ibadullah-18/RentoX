import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../../app/router.dart';
import '../../../core/l10n/app_localizations.dart';
import '../../../core/theme/app_colors.dart';
import '../../../core/theme/app_icons.dart';
import '../../../shared/widgets/glass.dart';
import '../../auth/presentation/auth_controller.dart';
import 'favorites_controller.dart';

/// Heart shown on top of listing photos (no background). Tapping toggles the favourite
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
        // 44px touch target; the heart floats on the photo with no
        // background, like every icon placed over a picture.
        child: SizedBox(
          width: diameter < 44 ? 44 : diameter,
          height: diameter < 44 ? 44 : diameter,
          child: Center(
            child: AnimatedSwitcher(
              duration: const Duration(milliseconds: 260),
              switchInCurve: Curves.easeOutBack,
              transitionBuilder: (child, animation) => AnimatedBuilder(
                animation: animation,
                child: child,
                builder: (context, child) => Transform(
                  alignment: Alignment.center,
                  transform: Matrix4.identity()
                    ..setEntry(3, 2, 0.002)
                    ..rotateY((1 - animation.value) * 1.2)
                    ..scaleByDouble(
                      0.6 + 0.4 * animation.value,
                      0.6 + 0.4 * animation.value,
                      1,
                      1,
                    ),
                  child: Opacity(
                    opacity: animation.value.clamp(0.0, 1.0),
                    child: child,
                  ),
                ),
              ),
              child: PhotoIcon(
                AppIcons.heart,
                key: ValueKey(active),
                size: diameter * 0.62,
                fill: AppIcons.fillOf(active),
                color: active ? AppColors.accent : Colors.white,
              ),
            ),
          ),
        ),
      ),
    );
  }
}
