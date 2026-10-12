import 'package:cached_network_image/cached_network_image.dart';
import 'package:flutter/material.dart';
import 'package:intl/intl.dart';

import 'package:go_router/go_router.dart';

import '../../app/router.dart';
import '../../core/l10n/app_localizations.dart';
import '../../core/theme/app_colors.dart';
import '../../core/theme/app_icons.dart';
import '../../core/theme/app_spacing.dart';
import '../../features/catalog/domain/catalog_models.dart';
import '../../features/favorites/presentation/favorite_button.dart';
import 'glass.dart';
import 'motion.dart';

String rentalUnitLabel(AppL10n l10n, RentalPeriodUnit unit) => switch (unit) {
  RentalPeriodUnit.hour => l10n.unitHour,
  RentalPeriodUnit.day => l10n.unitDay,
  RentalPeriodUnit.week => l10n.unitWeek,
  RentalPeriodUnit.month => l10n.unitMonth,
  RentalPeriodUnit.event => l10n.unitEvent,
  RentalPeriodUnit.negotiable => l10n.unitNegotiable,
};

/// "85 AZN / day" (or just "85 AZN" for negotiable listings).
String formatPriceValue(
  AppL10n l10n, {
  required double price,
  required String currency,
  required RentalPeriodUnit unit,
}) {
  final amount = NumberFormat.decimalPattern(l10n.localeName).format(price);
  if (unit == RentalPeriodUnit.negotiable) return '$amount $currency';
  return '$amount $currency / ${rentalUnitLabel(l10n, unit)}';
}

String formatPrice(AppL10n l10n, ListingSummary l) =>
    formatPriceValue(l10n, price: l.price, currency: l.currency, unit: l.unit);

class ListingCard extends StatelessWidget {
  const ListingCard({super.key, required this.listing, this.onOpen});

  final ListingSummary listing;

  /// Called just before the details page opens (e.g. to remember a search).
  final VoidCallback? onOpen;

  @override
  Widget build(BuildContext context) {
    final l10n = AppL10n.of(context);
    final theme = Theme.of(context);
    final scheme = theme.colorScheme;

    return PressScale(
      scale: 0.97,
      // One card: the photo runs to the edges (no frame at its top or
      // sides) and the text below it sits in the same box, whose outline
      // starts where the photo ends.
      child: Material(
        color: scheme.surface,
        borderRadius: BorderRadius.circular(AppRadius.lg),
        clipBehavior: Clip.antiAlias,
        child: InkWell(
          onTap: () {
            onOpen?.call();
            context.push(Routes.listing(listing.id));
          },
          child: CustomPaint(
            foregroundPainter: _FramePainter(
              color: scheme.outlineVariant.withValues(alpha: 0.7),
              radius: AppRadius.lg,
            ),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                AspectRatio(
                  aspectRatio: 4 / 3,
                  child: ClipRect(
                    child: Stack(
                      fit: StackFit.expand,
                      children: [
                        _Cover(url: listing.coverImageUrl),
                        const PhotoTopScrim(height: 52),
                        if (listing.isVip)
                          Positioned(
                            top: 8,
                            left: 8,
                            child: _VipBadge(label: l10n.vip),
                          ),
                        Positioned(
                          top: 2,
                          right: 2,
                          child: FavoriteButton(
                            listingId: listing.id,
                            isFavorite: listing.isFavorite,
                          ),
                        ),
                      ],
                    ),
                  ),
                ),
                Padding(
                  padding: const EdgeInsets.fromLTRB(10, 8, 10, 8),
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(
                        listing.title,
                        maxLines: 1,
                        overflow: TextOverflow.ellipsis,
                        style: theme.textTheme.titleSmall?.copyWith(
                          fontWeight: FontWeight.w600,
                        ),
                      ),
                      const SizedBox(height: 3),
                      Text(
                        formatPrice(l10n, listing),
                        maxLines: 1,
                        overflow: TextOverflow.ellipsis,
                        style: theme.textTheme.titleSmall?.copyWith(
                          color: scheme.primary,
                          fontWeight: FontWeight.w800,
                        ),
                      ),
                      if ((listing.location ?? listing.categoryName) !=
                          null) ...[
                        const SizedBox(height: 3),
                        Text(
                          (listing.location ?? listing.categoryName)!,
                          maxLines: 1,
                          overflow: TextOverflow.ellipsis,
                          style: theme.textTheme.bodySmall?.copyWith(
                            color: scheme.onSurfaceVariant,
                          ),
                        ),
                      ],
                    ],
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

class _Cover extends StatelessWidget {
  const _Cover({this.url});

  final String? url;

  @override
  Widget build(BuildContext context) {
    final placeholder = ColoredBox(
      color: context.primarySoft,
      child: Center(
        child: Icon(AppIcons.image, color: AppColors.primary, size: 34),
      ),
    );
    if (url == null) return placeholder;
    return CachedNetworkImage(
      imageUrl: url!,
      fit: BoxFit.cover,
      fadeInDuration: const Duration(milliseconds: 180),
      placeholder: (_, _) => placeholder,
      errorWidget: (_, _, _) => placeholder,
    );
  }
}

class _VipBadge extends StatelessWidget {
  const _VipBadge({required this.label});

  final String label;

  @override
  Widget build(BuildContext context) => GlassSurface(
    radius: AppRadius.sm,
    blur: 14,
    shadow: false,
    tintOpacity: 0.55,
    padding: const EdgeInsets.symmetric(horizontal: 9, vertical: 5),
    child: Row(
      mainAxisSize: MainAxisSize.min,
      children: [
        const Icon(AppIcons.vip, size: 14, fill: 1, color: AppColors.warning),
        const SizedBox(width: 4),
        Text(
          label,
          style: const TextStyle(
            fontSize: 11.5,
            fontWeight: FontWeight.w800,
            color: AppColors.ink,
          ),
        ),
      ],
    ),
  );
}

/// The card's outline, drawn only from the bottom of the 4:3 photo down:
/// sides and bottom of the text part, so the photo itself has no frame.
class _FramePainter extends CustomPainter {
  const _FramePainter({required this.color, required this.radius});

  final Color color;
  final double radius;

  @override
  void paint(Canvas canvas, Size size) {
    final photoBottom = size.width * 3 / 4;
    canvas.save();
    canvas.clipRect(
      Rect.fromLTWH(0, photoBottom, size.width, size.height - photoBottom),
    );
    canvas.drawRRect(
      RRect.fromRectAndRadius(
        (Offset.zero & size).deflate(0.5),
        Radius.circular(radius),
      ),
      Paint()
        ..style = PaintingStyle.stroke
        ..strokeWidth = 1
        ..color = color,
    );
    canvas.restore();
  }

  @override
  bool shouldRepaint(_FramePainter old) =>
      old.color != color || old.radius != radius;
}
