import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../../app/router.dart';
import '../../../core/l10n/app_localizations.dart';
import '../../../core/theme/app_icons.dart';
import '../../../core/theme/app_spacing.dart';
import '../../../shared/widgets/async_states.dart';
import '../../../shared/widgets/listing_grid.dart';
import '../../shell/tab_page.dart';
import 'favorites_controller.dart';

class FavoritesPage extends ConsumerWidget {
  const FavoritesPage({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final l10n = AppL10n.of(context);
    final favorites = ref.watch(favoritesProvider);
    // Listings un-favourited on this page disappear immediately; the list is
    // re-fetched in the background to stay in sync with the server.
    final overrides = ref.watch(favoriteOverridesProvider);

    return TabPage(
      onRefresh: () async {
        ref.invalidate(favoritesProvider);
        await ref.read(favoritesProvider.future);
      },
      onNearEnd: () => ref.read(favoritesProvider.notifier).loadMore(),
      slivers: [
        ...favorites.when(
          // Keep showing the list while it silently re-syncs after a change.
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
                onRetry: () => ref.invalidate(favoritesProvider),
              ),
            ),
          ],
          data: (state) {
            final items = state.items
                .where((l) => overrides[l.id] != false)
                .toList(growable: false);

            return [
              SliverToBoxAdapter(
                child: _Title(
                  title: l10n.favoritesTitle,
                  count: items.isEmpty
                      ? null
                      : l10n.favoritesCount(items.length),
                ),
              ),
              if (items.isEmpty)
                SliverFillRemaining(
                  hasScrollBody: false,
                  child: EmptyView(
                    icon: AppIcons.heart,
                    title: l10n.favoritesEmpty,
                    message: l10n.favoritesEmptyHint,
                    action: FilledButton(
                      onPressed: () => context.go(Routes.home),
                      style: FilledButton.styleFrom(
                        minimumSize: const Size(200, 52),
                      ),
                      child: Text(l10n.browseListings),
                    ),
                  ),
                )
              else ...[
                SliverListingGrid(items: items),
                if (state.loadingMore)
                  const SliverToBoxAdapter(
                    child: Padding(
                      padding: EdgeInsets.symmetric(vertical: AppSpacing.xl),
                      child: Center(
                        child: SizedBox(
                          width: 26,
                          height: 26,
                          child: CircularProgressIndicator(strokeWidth: 2.4),
                        ),
                      ),
                    ),
                  ),
              ],
            ];
          },
        ),
      ],
    );
  }
}

class _Title extends StatelessWidget {
  const _Title({required this.title, this.count});

  final String title;
  final String? count;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    return Padding(
      padding: const EdgeInsets.fromLTRB(
        AppSpacing.lg,
        AppSpacing.md,
        AppSpacing.lg,
        AppSpacing.lg,
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text(
            title,
            style: theme.textTheme.headlineMedium?.copyWith(
              fontWeight: FontWeight.w800,
            ),
          ),
          if (count != null) ...[
            const SizedBox(height: 2),
            Text(
              count!,
              style: TextStyle(color: theme.colorScheme.onSurfaceVariant),
            ),
          ],
        ],
      ),
    );
  }
}
