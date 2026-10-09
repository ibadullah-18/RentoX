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
import '../domain/owned_listing_models.dart';
import 'my_listings_controller.dart';
import 'status_chip.dart';

class MyListingsPage extends ConsumerStatefulWidget {
  const MyListingsPage({super.key});

  @override
  ConsumerState<MyListingsPage> createState() => _MyListingsPageState();
}

class _MyListingsPageState extends ConsumerState<MyListingsPage> {
  static const _barHeight = 70.0;
  final _scroll = ScrollController();

  @override
  void initState() {
    super.initState();
    _scroll.addListener(() {
      if (_scroll.hasClients && _scroll.position.extentAfter < 600) {
        ref.read(myListingsProvider.notifier).loadMore();
      }
    });
  }

  @override
  void dispose() {
    _scroll.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final l10n = AppL10n.of(context);
    final padding = MediaQuery.paddingOf(context);
    final listings = ref.watch(myListingsProvider);

    return Scaffold(
      body: Stack(
        children: [
          RefreshIndicator(
            edgeOffset: padding.top + _barHeight,
            onRefresh: () async {
              ref.invalidate(myListingsProvider);
              await ref.read(myListingsProvider.future);
            },
            child: CustomScrollView(
              controller: _scroll,
              physics: const AlwaysScrollableScrollPhysics(),
              slivers: [
                SliverToBoxAdapter(
                  child: SizedBox(height: padding.top + _barHeight),
                ),
                ...listings.when(
                  skipLoadingOnReload: true,
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
                        onRetry: () => ref.invalidate(myListingsProvider),
                      ),
                    ),
                  ],
                  data: (state) {
                    if (state.items.isEmpty) {
                      return [
                        SliverFillRemaining(
                          hasScrollBody: false,
                          child: EmptyView(
                            icon: AppIcons.listings,
                            title: l10n.myListingsEmpty,
                            message: l10n.myListingsEmptyHint,
                            action: FilledButton(
                              onPressed: () => context.push(Routes.create),
                              style: FilledButton.styleFrom(
                                minimumSize: const Size(200, 52),
                              ),
                              child: Text(l10n.navCreate),
                            ),
                          ),
                        ),
                      ];
                    }
                    return [
                      SliverPadding(
                        padding: AppSpacing.screenPadding,
                        sliver: SliverList.separated(
                          itemCount: state.items.length,
                          separatorBuilder: (_, _) =>
                              const SizedBox(height: AppSpacing.md),
                          itemBuilder: (_, i) =>
                              _OwnedCard(item: state.items[i]),
                        ),
                      ),
                      if (state.loadingMore)
                        const SliverToBoxAdapter(
                          child: Padding(
                            padding: EdgeInsets.symmetric(
                              vertical: AppSpacing.xl,
                            ),
                            child: Center(
                              child: SizedBox(
                                width: 26,
                                height: 26,
                                child: CircularProgressIndicator(
                                  strokeWidth: 2.4,
                                ),
                              ),
                            ),
                          ),
                        ),
                    ];
                  },
                ),
                SliverToBoxAdapter(
                  child: SizedBox(height: padding.bottom + 32),
                ),
              ],
            ),
          ),
          Positioned(
            top: 0,
            left: 0,
            right: 0,
            child: FrostedBar(
              height: _barHeight,
              child: Row(
                children: [
                  GlassIconButton(
                    icon: AppIcons.back,
                    semanticLabel: l10n.backAction,
                    onPressed: () => context.canPop()
                        ? context.pop()
                        : context.go(Routes.profile),
                  ),
                  const SizedBox(width: AppSpacing.md),
                  Expanded(
                    child: Text(
                      l10n.myListingsTitle,
                      style: Theme.of(context).textTheme.titleLarge
                          ?.copyWith(fontWeight: FontWeight.w800),
                    ),
                  ),
                  GlassIconButton(
                    icon: AppIcons.add,
                    semanticLabel: l10n.navCreate,
                    onPressed: () => context.push(Routes.create),
                  ),
                ],
              ),
            ),
          ),
        ],
      ),
    );
  }
}

class _OwnedCard extends StatelessWidget {
  const _OwnedCard({required this.item});

  final OwnedListingSummary item;

  @override
  Widget build(BuildContext context) {
    final l10n = AppL10n.of(context);
    final scheme = Theme.of(context).colorScheme;
    final price = formatPriceValue(
      l10n,
      price: item.price,
      currency: item.currency,
      unit: item.unit,
    );

    return Material(
      color: scheme.surface,
      borderRadius: BorderRadius.circular(AppRadius.lg),
      clipBehavior: Clip.antiAlias,
      child: InkWell(
        onTap: () => context.push(Routes.myListing(item.id)),
        child: Padding(
          padding: const EdgeInsets.all(AppSpacing.sm),
          child: Row(
            children: [
              ClipRRect(
                borderRadius: BorderRadius.circular(AppRadius.sm),
                child: SizedBox(
                  width: 92,
                  height: 92,
                  child: item.coverImageUrl == null
                      ? const ColoredBox(
                          color: AppColors.primarySoft,
                          child: Icon(AppIcons.image, color: AppColors.primary),
                        )
                      : CachedNetworkImage(
                          imageUrl: item.coverImageUrl!,
                          fit: BoxFit.cover,
                          errorWidget: (_, _, _) => const ColoredBox(
                            color: AppColors.primarySoft,
                            child: Icon(
                              AppIcons.image,
                              color: AppColors.primary,
                            ),
                          ),
                        ),
                ),
              ),
              const SizedBox(width: AppSpacing.md),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      item.title,
                      maxLines: 2,
                      overflow: TextOverflow.ellipsis,
                      style: const TextStyle(fontWeight: FontWeight.w700),
                    ),
                    const SizedBox(height: 4),
                    Text(
                      price,
                      style: TextStyle(
                        fontWeight: FontWeight.w800,
                        color: scheme.primary,
                      ),
                    ),
                    const SizedBox(height: 8),
                    ListingStatusChip(
                      status: effectiveStatus(
                        item.status,
                        item.expiresAt,
                        DateTime.now(),
                      ),
                    ),
                  ],
                ),
              ),
              Icon(AppIcons.forward, color: scheme.onSurfaceVariant),
            ],
          ),
        ),
      ),
    );
  }
}
