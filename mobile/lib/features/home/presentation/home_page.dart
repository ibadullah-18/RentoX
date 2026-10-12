import 'package:cached_network_image/cached_network_image.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../../app/router.dart';
import '../../../core/l10n/app_localizations.dart';
import '../../../core/theme/app_colors.dart';
import '../../../core/theme/app_icons.dart';
import '../../../core/theme/app_spacing.dart';
import '../../../shared/widgets/async_states.dart';
import '../../../shared/widgets/glass.dart';
import '../../../shared/widgets/listing_card.dart';
import '../../../shared/widgets/listing_grid.dart';
import '../../catalog/data/catalog_repository.dart';
import '../../catalog/domain/catalog_models.dart';
import '../../catalog/domain/paged_listings.dart';
import '../../search/presentation/filters_sheet.dart';
import 'home_state.dart';
import '../../store/domain/store_models.dart';
import '../../notifications/presentation/notifications_controller.dart';
import '../../store/presentation/store_controller.dart';
import '../../shell/tab_page.dart';

class HomePage extends ConsumerWidget {
  const HomePage({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final l10n = AppL10n.of(context);
    final listings = ref.watch(homeListingsProvider);

    return TabPage(
      onRefresh: () async {
        ref.invalidate(categoriesProvider);
        ref.invalidate(homeListingsProvider);
        ref.invalidate(homeStoresProvider);
        ref.invalidate(unreadNotificationsProvider);
        await ref.read(homeListingsProvider.future);
      },
      slivers: [
        const SliverToBoxAdapter(child: _SearchRow()),
        SliverToBoxAdapter(
          child: _SectionHeader(title: _categoriesTitle(ref, l10n)),
        ),
        const SliverToBoxAdapter(child: _CategoryStrip()),
        const SliverToBoxAdapter(child: _StoreStrip()),
        ...listings.when(
          loading: () => const [
            SliverFillRemaining(
              hasScrollBody: false,
              child: Center(child: CircularProgressIndicator()),
            ),
          ],
          error: (e, _) => [
            SliverFillRemaining(
              hasScrollBody: false,
              child: ErrorView(
                error: e,
                onRetry: () => ref.invalidate(homeListingsProvider),
              ),
            ),
          ],
          data: (page) {
            final tree = ref.watch(categoriesProvider).value;
            final path = ref.watch(homeCategoryPathProvider);
            final picked = tree == null || path.isEmpty
                ? null
                : findCategory(tree, path.last);
            return _listingSlivers(
              context,
              ref,
              l10n,
              page,
              pickedName: picked?.name,
            );
          },
        ),
      ],
    );
  }

  /// "Categories", or the picked chain: "Transport › Cars".
  String _categoriesTitle(WidgetRef ref, AppL10n l10n) {
    final tree = ref.watch(categoriesProvider).value;
    final path = ref.watch(homeCategoryPathProvider);
    if (tree == null || path.isEmpty) return l10n.categories;
    final names = [
      for (final id in path)
        if (findCategory(tree, id) case final c?) c.name,
    ];
    return names.isEmpty ? l10n.categories : names.join(' › ');
  }

  List<Widget> _listingSlivers(
    BuildContext context,
    WidgetRef ref,
    AppL10n l10n,
    PagedListings page, {
    String? pickedName,
  }) {
    final items = page.items;
    if (items.isEmpty) {
      return [
        SliverToBoxAdapter(
          child: Padding(
            padding: const EdgeInsets.symmetric(vertical: 48),
            child: EmptyView(
              icon: AppIcons.empty,
              title: l10n.emptyListings,
              message: l10n.emptyListingsHint,
            ),
          ),
        ),
      ];
    }

    final vip = items.where((e) => e.isVip).toList(growable: false);
    return [
      if (vip.isNotEmpty) ...[
        SliverToBoxAdapter(
          child: _SectionHeader(
            title: l10n.vipListings,
            leading: const Icon(
              AppIcons.vip,
              size: 20,
              fill: 1,
              color: AppColors.warning,
            ),
          ),
        ),
        SliverToBoxAdapter(
          child: SizedBox(
            height: 250,
            child: ListView.separated(
              scrollDirection: Axis.horizontal,
              padding: AppSpacing.screenPadding,
              itemCount: vip.length,
              separatorBuilder: (_, _) =>
                  const SizedBox(width: SliverListingGrid.gap),
              itemBuilder: (_, i) =>
                  SizedBox(width: 214, child: ListingCard(listing: vip[i])),
            ),
          ),
        ),
      ],
      SliverToBoxAdapter(
        child: _SectionHeader(title: pickedName ?? l10n.forYou),
      ),
      SliverListingGrid(items: items),
      if (page.loadingMore)
        const SliverToBoxAdapter(
          child: Padding(
            padding: EdgeInsets.all(AppSpacing.xl),
            child: Center(child: CircularProgressIndicator(strokeWidth: 2.4)),
          ),
        )
      else if (page.hasMore)
        SliverToBoxAdapter(
          child: Padding(
            padding: AppSpacing.screenPadding,
            child: OutlinedButton(
              onPressed: () =>
                  ref.read(homeListingsProvider.notifier).loadMore(),
              child: Text(l10n.loadMore),
            ),
          ),
        ),
    ];
  }
}

class _SearchRow extends ConsumerWidget {
  const _SearchRow();

  /// The filters apply to the home feed itself: no trip to the search page.
  Future<void> _openFilters(BuildContext context, WidgetRef ref) async {
    final path = ref.read(homeCategoryPathProvider);
    final current = ref
        .read(homeFiltersProvider)
        .copyWith(
          categoryId: path.isEmpty ? null : path.last,
          clearCategory: path.isEmpty,
        );
    final result = await showFiltersSheet(context, current: current);
    if (result == null || !context.mounted) return;
    ref.read(homeFiltersProvider.notifier).apply(result);
  }

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final l10n = AppL10n.of(context);
    final scheme = Theme.of(context).colorScheme;
    final query = ref.watch(homeQueryProvider);

    return Padding(
      padding: const EdgeInsets.fromLTRB(
        AppSpacing.lg,
        AppSpacing.sm,
        AppSpacing.lg,
        0,
      ),
      child: Row(
        children: [
          Expanded(
            child: GestureDetector(
              onTap: () =>
                  context.push(Routes.searchWith(query: query, focus: true)),
              behavior: HitTestBehavior.opaque,
              child: Semantics(
                button: true,
                label: l10n.searchHint,
                child: GlassSurface(
                  radius: AppRadius.pill,
                  padding: const EdgeInsets.symmetric(horizontal: 18),
                  child: SizedBox(
                    height: 52,
                    child: Row(
                      children: [
                        Icon(
                          AppIcons.search,
                          size: 22,
                          color: scheme.onSurfaceVariant,
                        ),
                        const SizedBox(width: 12),
                        Expanded(
                          child: Text(
                            query.isEmpty ? l10n.searchHint : query,
                            maxLines: 1,
                            overflow: TextOverflow.ellipsis,
                            style: TextStyle(
                              fontSize: 15,
                              fontWeight: query.isEmpty
                                  ? FontWeight.w400
                                  : FontWeight.w700,
                              color: query.isEmpty
                                  ? scheme.onSurfaceVariant
                                  : scheme.onSurface,
                            ),
                          ),
                        ),
                        if (query.isNotEmpty)
                          GestureDetector(
                            key: const ValueKey('home-search-clear'),
                            onTap: () =>
                                ref.read(homeQueryProvider.notifier).set(''),
                            behavior: HitTestBehavior.opaque,
                            child: Padding(
                              padding: const EdgeInsets.all(6),
                              child: Icon(
                                AppIcons.close,
                                size: 20,
                                semanticLabel: l10n.clearSearch,
                                color: scheme.onSurfaceVariant,
                              ),
                            ),
                          ),
                      ],
                    ),
                  ),
                ),
              ),
            ),
          ),
          const SizedBox(width: 10),
          GlassIconButton(
            icon: AppIcons.filter,
            size: 52,
            semanticLabel: l10n.filters,
            showDot: ref.watch(homeFiltersProvider).hasSheetFilters,
            onPressed: () => _openFilters(context, ref),
          ),
        ],
      ),
    );
  }
}

class _SectionHeader extends StatelessWidget {
  const _SectionHeader({required this.title, this.leading, this.onSeeAll});

  final String title;
  final Widget? leading;
  final VoidCallback? onSeeAll;

  @override
  Widget build(BuildContext context) {
    final l10n = AppL10n.of(context);
    final scheme = Theme.of(context).colorScheme;

    return Padding(
      padding: const EdgeInsets.fromLTRB(
        AppSpacing.lg,
        AppSpacing.xl,
        AppSpacing.lg,
        AppSpacing.md,
      ),
      child: Row(
        children: [
          if (leading != null) ...[leading!, const SizedBox(width: 6)],
          Expanded(
            child: Text(
              title,
              style: Theme.of(context).textTheme.titleLarge
                  ?.copyWith(fontWeight: FontWeight.w800, letterSpacing: -0.3),
            ),
          ),
          if (onSeeAll != null)
            GestureDetector(
              onTap: onSeeAll,
              behavior: HitTestBehavior.opaque,
              child: Row(
                children: [
                  Text(
                    l10n.seeAll,
                    style: TextStyle(
                      color: scheme.primary,
                      fontWeight: FontWeight.w600,
                      fontSize: 14,
                    ),
                  ),
                  const SizedBox(width: 2),
                  Icon(AppIcons.forward, size: 16, color: scheme.primary),
                ],
              ),
            ),
        ],
      ),
    );
  }
}

/// The categories as cards. Picking one stays on the home page and the
/// strip then shows what is *inside* it (its sub-categories); a leading
/// "back" card goes one level up. A category without children keeps showing
/// its siblings with itself marked.
class _CategoryStrip extends ConsumerWidget {
  const _CategoryStrip();

  static const _tints = [
    AppColors.primarySoft,
    AppColors.successSoft,
    AppColors.accentSoft,
    AppColors.warningSoft,
  ];

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final l10n = AppL10n.of(context);
    final categories = ref.watch(categoriesProvider);
    final path = ref.watch(homeCategoryPathProvider);
    final notifier = ref.read(homeCategoryPathProvider.notifier);
    final dark = Theme.of(context).brightness == Brightness.dark;
    final scheme = Theme.of(context).colorScheme;

    return SizedBox(
      height: 64,
      child: categories.when(
        loading: () => const SizedBox.shrink(),
        error: (_, _) => const SizedBox.shrink(),
        data: (tree) {
          // The picked chain, as far as it still exists in the tree.
          final chain = <Category>[];
          for (final id in path) {
            final c = findCategory(tree, id);
            if (c == null) break;
            chain.add(c);
          }
          final last = chain.isEmpty ? null : chain.last;
          final leafMode = last != null && last.children.isEmpty;

          final List<Category> shown;
          if (last == null) {
            shown = tree;
          } else if (!leafMode) {
            shown = last.children;
          } else {
            shown = chain.length > 1 ? chain[chain.length - 2].children : tree;
          }
          final selectedId = leafMode ? last.id : null;

          void pick(Category c) {
            if (!leafMode) {
              notifier.select(chain.length, c.id);
            } else if (c.id == selectedId) {
              notifier.clearFrom(chain.length - 1);
            } else {
              notifier.select(chain.length - 1, c.id);
            }
          }

          final hasBack = chain.isNotEmpty;
          return ListView.separated(
            scrollDirection: Axis.horizontal,
            padding: AppSpacing.screenPadding,
            itemCount: shown.length + (hasBack ? 1 : 0),
            separatorBuilder: (_, _) => const SizedBox(width: 10),
            itemBuilder: (context, i) {
              if (hasBack && i == 0) {
                return _BackTile(
                  label: l10n.backAction,
                  onTap: () => notifier.clearFrom(chain.length - 1),
                );
              }
              final category = shown[i - (hasBack ? 1 : 0)];
              final tint = _tints[i % _tints.length];
              return _CategoryTile(
                label: category.name,
                icon: AppIcons.categoryIcon(category.slug),
                color: dark ? tint.withValues(alpha: 0.16) : tint,
                iconColor: dark ? scheme.onSurface : AppColors.ink,
                selected: category.id == selectedId,
                onTap: () => pick(category),
              );
            },
          );
        },
      ),
    );
  }
}

/// "Back" in the category strip: just the arrow, as narrow as a button.
class _BackTile extends StatelessWidget {
  const _BackTile({required this.label, required this.onTap});

  final String label;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    final scheme = Theme.of(context).colorScheme;
    return Semantics(
      button: true,
      label: label,
      child: GestureDetector(
        key: const ValueKey('category-back'),
        onTap: onTap,
        behavior: HitTestBehavior.opaque,
        child: Container(
          width: 48,
          decoration: BoxDecoration(
            color: scheme.primary.withValues(alpha: 0.10),
            borderRadius: BorderRadius.circular(AppRadius.lg),
          ),
          child: Icon(AppIcons.back, size: 26, color: scheme.primary),
        ),
      ),
    );
  }
}

/// One category: a wide, low card with its icon and name side by side.
class _CategoryTile extends StatelessWidget {
  const _CategoryTile({
    required this.label,
    required this.icon,
    required this.color,
    required this.iconColor,
    required this.onTap,
    this.selected = false,
  });

  final String label;
  final IconData icon;
  final Color color;
  final Color iconColor;
  final bool selected;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    final scheme = Theme.of(context).colorScheme;
    return Semantics(
      button: true,
      selected: selected,
      label: label,
      child: GestureDetector(
        onTap: onTap,
        behavior: HitTestBehavior.opaque,
        child: AnimatedContainer(
          duration: const Duration(milliseconds: 160),
          constraints: const BoxConstraints(minWidth: 120, maxWidth: 190),
          padding: const EdgeInsets.fromLTRB(8, 8, 16, 8),
          decoration: BoxDecoration(
            color: color,
            borderRadius: BorderRadius.circular(AppRadius.lg),
            border: Border.all(
              color: selected ? scheme.primary : Colors.transparent,
              width: 2,
            ),
          ),
          child: Row(
            mainAxisSize: MainAxisSize.min,
            children: [
              Container(
                width: 40,
                height: 40,
                decoration: BoxDecoration(
                  color: scheme.surface.withValues(alpha: 0.65),
                  borderRadius: BorderRadius.circular(AppRadius.md),
                ),
                child: Icon(icon, size: 24, color: iconColor),
              ),
              const SizedBox(width: 10),
              Flexible(
                child: Text(
                  label,
                  maxLines: 1,
                  overflow: TextOverflow.ellipsis,
                  style: TextStyle(
                    fontSize: 14,
                    fontWeight: selected ? FontWeight.w800 : FontWeight.w700,
                    color: selected ? scheme.primary : null,
                  ),
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }
}

/// Stores to browse. Nothing is shown while loading, on error, or when there
/// are no stores yet: it is an extra, never a blocker for the page.
class _StoreStrip extends ConsumerWidget {
  const _StoreStrip();

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final stores = ref.watch(homeStoresProvider).value;
    if (stores == null || stores.isEmpty) return const SizedBox.shrink();

    final l10n = AppL10n.of(context);
    final scheme = Theme.of(context).colorScheme;

    Widget logo(FollowedStore store) {
      final placeholder = ColoredBox(
        color: scheme.primary.withValues(alpha: 0.12),
        child: Icon(AppIcons.store, color: scheme.primary),
      );
      return ClipRRect(
        borderRadius: BorderRadius.circular(AppRadius.lg),
        child: SizedBox(
          width: 62,
          height: 62,
          child: store.logoUrl == null
              ? placeholder
              : CachedNetworkImage(
                  imageUrl: store.logoUrl!,
                  fit: BoxFit.cover,
                  placeholder: (_, _) => placeholder,
                  errorWidget: (_, _, _) => placeholder,
                ),
        ),
      );
    }

    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        _SectionHeader(
          title: l10n.storesTitle,
          onSeeAll: () => context.push(Routes.searchWith(stores: true)),
        ),
        SizedBox(
          height: 100,
          child: ListView.separated(
            scrollDirection: Axis.horizontal,
            padding: AppSpacing.screenPadding,
            itemCount: stores.length,
            separatorBuilder: (_, _) => const SizedBox(width: 14),
            itemBuilder: (context, i) {
              final store = stores[i];
              return Semantics(
                button: true,
                label: store.name,
                child: GestureDetector(
                  onTap: () => context.push(Routes.store(store.slug)),
                  behavior: HitTestBehavior.opaque,
                  child: SizedBox(
                    width: 76,
                    child: Column(
                      children: [
                        logo(store),
                        const SizedBox(height: 8),
                        Text(
                          store.name,
                          maxLines: 1,
                          overflow: TextOverflow.ellipsis,
                          textAlign: TextAlign.center,
                          style: const TextStyle(
                            fontSize: 12.5,
                            fontWeight: FontWeight.w600,
                          ),
                        ),
                      ],
                    ),
                  ),
                ),
              );
            },
          ),
        ),
      ],
    );
  }
}
