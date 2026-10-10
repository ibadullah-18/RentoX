import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/l10n/app_localizations.dart';
import '../../../core/theme/app_icons.dart';
import '../../../core/theme/app_spacing.dart';
import '../../../shared/widgets/async_states.dart';
import '../../../shared/widgets/sub_page.dart';
import 'store_controller.dart';
import 'store_tile.dart';

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
                  itemBuilder: (context, i) => StoreTile(store: list[i]),
                ),
        ),
      ),
    );
  }
}
