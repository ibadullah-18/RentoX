import 'package:flutter/material.dart';

import '../../core/theme/app_spacing.dart';
import '../../features/catalog/domain/catalog_models.dart';
import 'listing_card.dart';

/// Responsive grid of [ListingCard]s used by every listing page, so cards
/// look and size identically everywhere (2 columns on phones, more on wide
/// screens).
class SliverListingGrid extends StatelessWidget {
  const SliverListingGrid({super.key, required this.items, this.onOpen});

  final List<ListingSummary> items;

  /// Called with the listing about to be opened.
  final ValueChanged<ListingSummary>? onOpen;

  static const gap = 12.0;

  @override
  Widget build(BuildContext context) {
    return SliverPadding(
      padding: AppSpacing.screenPadding,
      sliver: SliverLayoutBuilder(
        builder: (context, constraints) {
          final cross = constraints.crossAxisExtent;
          final columns = (cross / 210).floor().clamp(2, 6);
          final cardWidth = (cross - gap * (columns - 1)) / columns;
          final textScale = MediaQuery.textScalerOf(context).scale(1);
          // image (4:3) + paddings + three text lines.
          final extent = (cardWidth - 16) * 3 / 4 + 32 + 66 * textScale;
          return SliverGrid.builder(
            gridDelegate: SliverGridDelegateWithFixedCrossAxisCount(
              crossAxisCount: columns,
              mainAxisSpacing: gap,
              crossAxisSpacing: gap,
              mainAxisExtent: extent,
            ),
            itemCount: items.length,
            itemBuilder: (_, i) => ListingCard(
              listing: items[i],
              onOpen: onOpen == null ? null : () => onOpen!(items[i]),
            ),
          );
        },
      ),
    );
  }
}
