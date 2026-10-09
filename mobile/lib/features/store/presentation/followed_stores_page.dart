import 'package:cached_network_image/cached_network_image.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../../app/router.dart';
import '../../../core/l10n/app_localizations.dart';
import '../../../core/theme/app_icons.dart';
import '../../../core/theme/app_spacing.dart';
import '../../../shared/widgets/async_states.dart';
import '../../../shared/widgets/sub_page.dart';
import '../domain/store_models.dart';
import 'store_controller.dart';

/// Stores the user follows.
class FollowedStoresPage extends ConsumerWidget {
  const FollowedStoresPage({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final l10n = AppL10n.of(context);
    final stores = ref.watch(followedStoresProvider);
    final top = SubPage.topInset(context);

    return SubPage(
      title: l10n.storeFollowing,
      child: stores.when(
        loading: () => const Center(child: CircularProgressIndicator()),
        error: (e, _) => ErrorView(
          error: e,
          onRetry: () => ref.invalidate(followedStoresProvider),
        ),
        data: (list) => RefreshIndicator(
          edgeOffset: top,
          onRefresh: () async {
            ref.invalidate(followedStoresProvider);
            await ref.read(followedStoresProvider.future);
          },
          child: list.isEmpty
              ? ListView(
                  physics: const AlwaysScrollableScrollPhysics(),
                  children: [
                    SizedBox(height: top + 80),
                    EmptyView(
                      icon: AppIcons.store,
                      title: l10n.storeFollowingEmpty,
                      message: l10n.storeFollowingEmptyHint,
                    ),
                  ],
                )
              : ListView.separated(
                  physics: const AlwaysScrollableScrollPhysics(),
                  padding: EdgeInsets.fromLTRB(
                    AppSpacing.lg,
                    top + AppSpacing.md,
                    AppSpacing.lg,
                    MediaQuery.paddingOf(context).bottom + AppSpacing.xl,
                  ),
                  itemCount: list.length,
                  separatorBuilder: (_, _) =>
                      const SizedBox(height: AppSpacing.sm),
                  itemBuilder: (context, i) => _Tile(store: list[i]),
                ),
        ),
      ),
    );
  }
}

class _Tile extends StatelessWidget {
  const _Tile({required this.store});

  final FollowedStore store;

  @override
  Widget build(BuildContext context) {
    final l10n = AppL10n.of(context);
    final scheme = Theme.of(context).colorScheme;
    final placeholder = ColoredBox(
      color: scheme.primary.withValues(alpha: 0.12),
      child: Icon(AppIcons.store, color: scheme.primary),
    );

    return Material(
      color: scheme.surface,
      borderRadius: BorderRadius.circular(AppRadius.xl),
      child: InkWell(
        borderRadius: BorderRadius.circular(AppRadius.xl),
        onTap: () => context.push(Routes.store(store.slug)),
        child: Container(
          padding: const EdgeInsets.all(AppSpacing.md),
          decoration: BoxDecoration(
            borderRadius: BorderRadius.circular(AppRadius.xl),
            border: Border.all(color: scheme.outlineVariant),
          ),
          child: Row(
            children: [
              ClipRRect(
                borderRadius: BorderRadius.circular(AppRadius.lg),
                child: SizedBox(
                  width: 56,
                  height: 56,
                  child: store.logoUrl == null
                      ? placeholder
                      : CachedNetworkImage(
                          imageUrl: store.logoUrl!,
                          fit: BoxFit.cover,
                          placeholder: (_, _) => placeholder,
                          errorWidget: (_, _, _) => placeholder,
                        ),
                ),
              ),
              const SizedBox(width: AppSpacing.md),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      store.name,
                      maxLines: 1,
                      overflow: TextOverflow.ellipsis,
                      style: const TextStyle(
                        fontWeight: FontWeight.w800,
                        fontSize: 16,
                      ),
                    ),
                    const SizedBox(height: 2),
                    Text(
                      '${l10n.storeListingsCount(store.activeListingCount)} · '
                      '${l10n.storeFollowers(store.followerCount)}',
                      style: TextStyle(
                        color: scheme.onSurfaceVariant,
                        fontSize: 13.5,
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
