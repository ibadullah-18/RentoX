import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../../app/router.dart';
import '../../../core/l10n/app_localizations.dart';
import '../../../core/theme/app_icons.dart';
import '../../../core/theme/app_spacing.dart';
import '../../../shared/widgets/listing_grid.dart';
import '../../catalog/data/catalog_repository.dart';

/// "Similar listings" at the bottom of a listing: the same brand first, then
/// the same category, then neighbouring categories (the backend mixes them),
/// and a button to see more of the category.
///
/// Shows nothing while loading, on error, or when there is nothing similar:
/// it is an extra, never a blocker for the page.
class SimilarListingsSliver extends ConsumerWidget {
  const SimilarListingsSliver({
    super.key,
    required this.listingId,
    this.categoryId,
  });

  final String listingId;
  final String? categoryId;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final items = ref.watch(similarListingsProvider(listingId)).value;
    if (items == null || items.isEmpty) {
      return const SliverToBoxAdapter(child: SizedBox.shrink());
    }

    final l10n = AppL10n.of(context);
    final scheme = Theme.of(context).colorScheme;

    return SliverMainAxisGroup(
      slivers: [
        SliverToBoxAdapter(
          child: Padding(
            padding: const EdgeInsets.fromLTRB(
              AppSpacing.lg,
              AppSpacing.xl,
              AppSpacing.lg,
              AppSpacing.md,
            ),
            child: Text(
              l10n.similarListings,
              style: Theme.of(context).textTheme.titleLarge
                  ?.copyWith(fontWeight: FontWeight.w800, letterSpacing: -0.3),
            ),
          ),
        ),
        SliverListingGrid(items: items),
        if (categoryId != null)
          SliverToBoxAdapter(
            child: Padding(
              padding: const EdgeInsets.fromLTRB(
                AppSpacing.lg,
                AppSpacing.lg,
                AppSpacing.lg,
                0,
              ),
              child: OutlinedButton(
                onPressed: () =>
                    context.push(Routes.searchWith(categoryId: categoryId)),
                style: OutlinedButton.styleFrom(
                  minimumSize: const Size.fromHeight(52),
                ),
                child: Row(
                  mainAxisAlignment: MainAxisAlignment.center,
                  children: [
                    Text(l10n.similarMore),
                    const SizedBox(width: 4),
                    Icon(AppIcons.forward, size: 18, color: scheme.primary),
                  ],
                ),
              ),
            ),
          ),
      ],
    );
  }
}
