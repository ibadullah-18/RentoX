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

    return Material(
      color: scheme.surface,
      borderRadius: BorderRadius.circular(AppRadius.lg),
      clipBehavior: Clip.antiAlias,
      child: InkWell(
        onTap: () {
          onOpen?.call();
          context.push(Routes.listing(listing.id));
        },
        child: Padding(
          padding: const EdgeInsets.all(8),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              AspectRatio(
                aspectRatio: 4 / 3,
                child: ClipRRect(
                  borderRadius: BorderRadius.circular(AppRadius.sm),
                  child: Stack(
                    fit: StackFit.expand,
                    children: [
                      _Cover(url: listing.coverImageUrl),
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
                padding: const EdgeInsets.fromLTRB(6, 10, 6, 4),
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
                    if (listing.categoryName != null) ...[
                      const SizedBox(height: 3),
                      Text(
                        listing.categoryName!,
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
    );
  }
}

class _Cover extends StatelessWidget {
  const _Cover({this.url});

  final String? url;

  @override
  Widget build(BuildContext context) {
    const placeholder = ColoredBox(
      color: AppColors.primarySoft,
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
