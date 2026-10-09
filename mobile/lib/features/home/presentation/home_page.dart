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
import '../../notifications/presentation/notifications_controller.dart';
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
        ref.invalidate(unreadNotificationsProvider);
        await ref.read(homeListingsProvider.future);
      },
      slivers: [
        const SliverToBoxAdapter(child: _SearchRow()),
        SliverToBoxAdapter(child: _SectionHeader(title: l10n.categories)),
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
          data: (page) => _listingSlivers(context, l10n, page.items),
        ),
      ],
    );
  }

  List<Widget> _listingSlivers(
    BuildContext context,
    AppL10n l10n,
    List<ListingSummary> items,
  ) {
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
        const SliverToBoxAdapter(child: _TrustBanner()),
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
            onSeeAll: () => context.push(Routes.search),
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
        child: _SectionHeader(
          title: l10n.forYou,
          onSeeAll: () => context.push(Routes.search),
        ),
      ),
      SliverListingGrid(items: items),
      const SliverToBoxAdapter(child: _TrustBanner()),
    ];
  }
}

class _SearchRow extends StatelessWidget {
  const _SearchRow();

  @override
  Widget build(BuildContext context) {
    final l10n = AppL10n.of(context);
    final scheme = Theme.of(context).colorScheme;

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
              onTap: () => context.push(Routes.searchWith(focus: true)),
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
                            l10n.searchHint,
                            maxLines: 1,
                            overflow: TextOverflow.ellipsis,
                            style: TextStyle(
                              fontSize: 15,
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
            onPressed: () => context.push(Routes.search),
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
    final categories = ref.watch(categoriesProvider);
    final dark = Theme.of(context).brightness == Brightness.dark;
    final scheme = Theme.of(context).colorScheme;

    return SizedBox(
      height: 100,
      child: categories.when(
        loading: () => const SizedBox.shrink(),
        error: (_, _) => const SizedBox.shrink(),
        data: (items) => ListView.separated(
          scrollDirection: Axis.horizontal,
          padding: AppSpacing.screenPadding,
          itemCount: items.length,
          separatorBuilder: (_, _) => const SizedBox(width: 14),
          itemBuilder: (context, i) {
            final category = items[i];
            final tint = _tints[i % _tints.length];
            return GestureDetector(
              onTap: () =>
                  context.push(Routes.searchWith(categoryId: category.id)),
              behavior: HitTestBehavior.opaque,
              child: SizedBox(
                width: 76,
                child: Column(
                  children: [
                    Container(
                      width: 62,
                      height: 62,
                      decoration: BoxDecoration(
                        color: dark ? tint.withValues(alpha: 0.16) : tint,
                        borderRadius: BorderRadius.circular(AppRadius.lg),
                      ),
                      child: Icon(
                        AppIcons.categoryIcon(category.slug),
                        size: 30,
                        color: dark ? scheme.onSurface : AppColors.ink,
                      ),
                    ),
                    const SizedBox(height: 8),
                    Text(
                      category.name,
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
            );
          },
        ),
      ),
    );
  }
}

class _TrustBanner extends StatelessWidget {
  const _TrustBanner();

  @override
  Widget build(BuildContext context) {
    final l10n = AppL10n.of(context);
    final dark = Theme.of(context).brightness == Brightness.dark;

    return Padding(
      padding: const EdgeInsets.fromLTRB(
        AppSpacing.lg,
        AppSpacing.xl,
        AppSpacing.lg,
        0,
      ),
      child: Container(
        padding: const EdgeInsets.all(14),
        decoration: BoxDecoration(
          color: dark
              ? AppColors.success.withValues(alpha: 0.12)
              : AppColors.successSoft,
          borderRadius: BorderRadius.circular(AppRadius.lg),
        ),
        child: Row(
          children: [
            Container(
              width: 44,
              height: 44,
              decoration: BoxDecoration(
                color: AppColors.success.withValues(alpha: 0.18),
                borderRadius: BorderRadius.circular(AppRadius.lg),
              ),
              child: const Icon(
                AppIcons.shield,
                color: AppColors.successInk,
                size: 24,
              ),
            ),
            const SizedBox(width: 12),
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(
                    l10n.trustTitle,
                    style: TextStyle(
                      fontWeight: FontWeight.w700,
                      color: dark ? AppColors.success : AppColors.successInk,
                    ),
                  ),
                  const SizedBox(height: 2),
                  Text(
                    l10n.trustSubtitle,
                    style: TextStyle(
                      fontSize: 13,
                      color: Theme.of(context).colorScheme.onSurfaceVariant,
                    ),
                  ),
                ],
              ),
            ),
          ],
        ),
      ),
    );
  }
}
