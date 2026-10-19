import '../../../shared/widgets/category_icon.dart';

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
import 'home_banners.dart';

class HomePage extends ConsumerWidget {
  const HomePage({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final l10n = AppL10n.of(context);
    final listings = ref.watch(homeListingsProvider);
    // A search, a picked category or a filter: only the matching listings are shown, without banners and stores.
    final browsing =
        ref.watch(homeCategoryPathProvider).isNotEmpty ||
        ref.watch(homeQueryProvider).isNotEmpty ||
        ref.watch(homeFiltersProvider).hasSheetFilters;

    return TabPage(
      onRefresh: () async {
        ref.invalidate(bannersProvider);
        ref.invalidate(categoriesProvider);
        ref.invalidate(homeListingsProvider);
        ref.invalidate(homeStoresProvider);
        ref.invalidate(unreadNotificationsProvider);
        await ref.read(homeListingsProvider.future);
      },
      slivers: [
        const SliverToBoxAdapter(child: _SearchRow()),
        if (!browsing) ...const [
          SliverToBoxAdapter(child: HomeBanners()),
          SliverToBoxAdapter(child: _StoreStrip()),
        ],
        SliverToBoxAdapter(
          child: _SectionHeader(title: _categoriesTitle(ref, l10n)),
        ),
        const SliverToBoxAdapter(child: _CategoryStrip()),
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

    // One feed, in the order the API makes it: 4 VIP, 4 moved up, 4 ordinary, and again.
    return [
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
      height: 46,
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
                slug: category.slug,
                iconUrl: category.iconUrl,
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
        child: SizedBox(
          width: 28,
          child: Icon(
            Icons.chevron_left_rounded,
            size: 30,
            color: scheme.onSurfaceVariant,
          ),
        ),
      ),
    );
  }
}

/// One category: a wide, low card with its icon and name side by side.
class _CategoryTile extends StatelessWidget {
  const _CategoryTile({
    required this.label,
    required this.slug,
    this.iconUrl,
    required this.color,
    required this.iconColor,
    required this.onTap,
    this.selected = false,
  });

  final String label;
  final String slug;
  final String? iconUrl;
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
          constraints: const BoxConstraints(minWidth: 0, maxWidth: 160),
          padding: const EdgeInsets.fromLTRB(5, 5, 12, 5),
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
                width: 28,
                height: 28,
                decoration: BoxDecoration(
                  color: scheme.surface.withValues(alpha: 0.65),
                  borderRadius: BorderRadius.circular(AppRadius.sm),
                ),
                child: CategoryIcon(
                  slug: slug,
                  iconUrl: iconUrl,
                  color: iconColor,
                  size: 18,
                ),
              ),
              const SizedBox(width: 8),
              Flexible(
                child: Text(
                  label,
                  maxLines: 1,
                  overflow: TextOverflow.ellipsis,
                  style: TextStyle(
                    fontSize: 13,
                    fontWeight: selected ? FontWeight.w800 : FontWeight.w600,
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
        borderRadius: BorderRadius.circular(AppRadius.md),
        child: SizedBox(
          width: 48,
          height: 48,
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
          leading: const Icon(
            AppIcons.vip,
            size: 20,
            fill: 1,
            color: AppColors.warning,
          ),
          onSeeAll: () => context.push(Routes.searchWith(stores: true)),
        ),
        SizedBox(
          height: 112,
          child: ListView.separated(
            scrollDirection: Axis.horizontal,
            padding: AppSpacing.screenPadding,
            itemCount: stores.length,
            separatorBuilder: (_, _) => const SizedBox(width: 10),
            itemBuilder: (context, i) {
              final store = stores[i];
              // More than one store: each card is a little narrower than the screen, so the next one peeks in.
              final cardWidth = stores.length > 1
                  ? MediaQuery.sizeOf(context).width * 0.78
                  : MediaQuery.sizeOf(context).width - AppSpacing.lg * 2;
              return Semantics(
                button: true,
                label: store.name,
                child: GestureDetector(
                  onTap: () => context.push(Routes.store(store.slug)),
                  behavior: HitTestBehavior.opaque,
                  child: Container(
                    width: cardWidth.clamp(220.0, 420.0),
                    padding: const EdgeInsets.symmetric(
                      horizontal: 10,
                      vertical: 8,
                    ),
                    decoration: BoxDecoration(
                      color: scheme.surface,
                      borderRadius: BorderRadius.circular(AppRadius.lg),
                      border: Border.all(
                        color: AppColors.warning.withValues(alpha: 0.55),
                      ),
                    ),
                    child: Row(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        logo(store),
                        const SizedBox(width: 10),
                        Expanded(
                          child: Column(
                            mainAxisAlignment: MainAxisAlignment.start,
                            crossAxisAlignment: CrossAxisAlignment.start,
                            children: [
                              Row(
                                mainAxisSize: MainAxisSize.min,
                                children: [
                                  const Icon(
                                    AppIcons.vip,
                                    size: 12,
                                    fill: 1,
                                    color: AppColors.warning,
                                  ),
                                  const SizedBox(width: 3),
                                  Text(
                                    l10n.storeVipBadge,
                                    style: const TextStyle(
                                      fontSize: 10.5,
                                      fontWeight: FontWeight.w800,
                                      color: AppColors.warning,
                                    ),
                                  ),
                                ],
                              ),
                              Text(
                                store.name,
                                maxLines: 1,
                                overflow: TextOverflow.ellipsis,
                                style: const TextStyle(
                                  fontSize: 14,
                                  fontWeight: FontWeight.w700,
                                ),
                              ),
                              if (store.description.isNotEmpty)
                                Text(
                                  store.description,
                                  maxLines: 3,
                                  overflow: TextOverflow.ellipsis,
                                  style: TextStyle(
                                    fontSize: 12.5,
                                    color: scheme.onSurfaceVariant,
                                    height: 1.25,
                                  ),
                                ),
                              Text(
                                l10n.storeListingsCount(
                                  store.activeListingCount,
                                ),
                                maxLines: 1,
                                style: TextStyle(
                                  fontSize: 11.5,
                                  color: scheme.primary,
                                  fontWeight: FontWeight.w600,
                                ),
                              ),
                            ],
                          ),
                        ),
                        const SizedBox(width: 4),
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
