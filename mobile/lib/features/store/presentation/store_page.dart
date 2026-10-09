import 'package:cached_network_image/cached_network_image.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../../app/router.dart';
import '../../../core/l10n/app_localizations.dart';
import '../../../core/network/api_exception.dart';
import '../../../core/theme/app_icons.dart';
import '../../../core/theme/app_spacing.dart';
import '../../../shared/utils/external_links.dart';
import '../../../shared/widgets/async_states.dart';
import '../../../shared/widgets/glass.dart';
import '../../../shared/widgets/listing_grid.dart';
import '../../auth/presentation/auth_controller.dart';
import '../../support/domain/support_models.dart';
import '../../support/presentation/support_widgets.dart';
import '../domain/store_models.dart';
import 'store_controller.dart';

String _kindName(SocialKind k) => switch (k) {
  SocialKind.website => 'Website',
  SocialKind.instagram => 'Instagram',
  SocialKind.tiktok => 'TikTok',
  SocialKind.facebook => 'Facebook',
};

/// A live store: who they are, how to reach them and everything they rent.
class StorePage extends ConsumerWidget {
  const StorePage({super.key, required this.slug});

  final String slug;

  void _back(BuildContext context) =>
      context.canPop() ? context.pop() : context.go(Routes.home);

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final l10n = AppL10n.of(context);
    final store = ref.watch(publicStoreProvider(slug));

    Widget plain(Widget child) => Scaffold(
      body: SafeArea(
        child: Stack(
          children: [
            child,
            Padding(
              padding: const EdgeInsets.all(AppSpacing.lg),
              child: GlassIconButton(
                icon: AppIcons.back,
                semanticLabel: l10n.backAction,
                onPressed: () => _back(context),
              ),
            ),
          ],
        ),
      ),
    );

    return store.when(
      loading: () => plain(const Center(child: CircularProgressIndicator())),
      error: (e, _) => plain(
        e is ApiException && e.statusCode == 404
            ? EmptyView(icon: AppIcons.store, title: l10n.storeNotFound)
            : ErrorView(
                error: e,
                onRetry: () => ref.invalidate(publicStoreProvider(slug)),
              ),
      ),
      data: (s) => _StoreBody(store: s, onBack: () => _back(context)),
    );
  }
}

class _StoreBody extends ConsumerStatefulWidget {
  const _StoreBody({required this.store, required this.onBack});

  final PublicStore store;
  final VoidCallback onBack;

  @override
  ConsumerState<_StoreBody> createState() => _StoreBodyState();
}

class _StoreBodyState extends ConsumerState<_StoreBody> {
  final _scroll = ScrollController();

  @override
  void initState() {
    super.initState();
    _scroll.addListener(() {
      if (_scroll.hasClients && _scroll.position.extentAfter < 600) {
        ref.read(storeListingsProvider(widget.store.slug).notifier).loadMore();
      }
    });
  }

  @override
  void dispose() {
    _scroll.dispose();
    super.dispose();
  }

  PublicStore get s => widget.store;

  @override
  Widget build(BuildContext context) {
    final l10n = AppL10n.of(context);
    final theme = Theme.of(context);
    final scheme = theme.colorScheme;
    final padding = MediaQuery.paddingOf(context);
    final listings = ref.watch(storeListingsProvider(s.slug));

    return Scaffold(
      body: CustomScrollView(
        controller: _scroll,
        slivers: [
          SliverToBoxAdapter(
            child: SizedBox(
              height: padding.top + 130 + 44,
              child: Stack(
                clipBehavior: Clip.none,
                children: [
                  Positioned(
                    left: 0,
                    right: 0,
                    top: 0,
                    height: padding.top + 130,
                    child: s.coverUrl == null
                        ? ColoredBox(
                            color: scheme.primary.withValues(alpha: 0.14),
                          )
                        : CachedNetworkImage(
                            imageUrl: s.coverUrl!,
                            fit: BoxFit.cover,
                            errorWidget: (_, _, _) => ColoredBox(
                              color: scheme.primary.withValues(alpha: 0.14),
                            ),
                          ),
                  ),
                  PhotoTopScrim(height: padding.top + 84),
                  Positioned(
                    top: padding.top + AppSpacing.sm,
                    left: AppSpacing.md,
                    child: GlassIconButton(
                      onPhoto: true,
                      icon: AppIcons.back,
                      semanticLabel: l10n.backAction,
                      onPressed: widget.onBack,
                    ),
                  ),
                  Positioned(
                    left: AppSpacing.lg,
                    bottom: 0,
                    child: Container(
                      width: 88,
                      height: 88,
                      decoration: BoxDecoration(
                        color: scheme.surface,
                        borderRadius: BorderRadius.circular(AppRadius.xl),
                        border: Border.all(color: scheme.surface, width: 4),
                      ),
                      child: ClipRRect(
                        borderRadius: BorderRadius.circular(AppRadius.xl - 3),
                        child: s.logoUrl == null
                            ? ColoredBox(
                                color: scheme.primary.withValues(alpha: 0.12),
                                child: Icon(
                                  AppIcons.store,
                                  color: scheme.primary,
                                  size: 36,
                                ),
                              )
                            : CachedNetworkImage(
                                imageUrl: s.logoUrl!,
                                fit: BoxFit.cover,
                                errorWidget: (_, _, _) =>
                                    const Icon(AppIcons.store),
                              ),
                      ),
                    ),
                  ),
                ],
              ),
            ),
          ),
          SliverPadding(
            padding: const EdgeInsets.fromLTRB(
              AppSpacing.lg,
              AppSpacing.md,
              AppSpacing.lg,
              0,
            ),
            sliver: SliverList.list(
              children: [
                Row(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Expanded(
                      child: Text(
                        s.name,
                        style: theme.textTheme.headlineSmall?.copyWith(
                          fontWeight: FontWeight.w800,
                        ),
                      ),
                    ),
                    const SizedBox(width: AppSpacing.md),
                    _FollowButton(store: s),
                  ],
                ),
                const SizedBox(height: AppSpacing.sm),
                _Stats(store: s),
                const SizedBox(height: AppSpacing.lg),
                Text(
                  s.description,
                  style: theme.textTheme.bodyLarge?.copyWith(height: 1.5),
                ),
                const SizedBox(height: AppSpacing.lg),
                _Contacts(store: s),
                if (ref.watch(myStoreProvider).value?.id != s.id) ...[
                  const SizedBox(height: AppSpacing.sm),
                  ReportProblemRow(
                    hint: l10n.reportStoreHint,
                    onTap: () => context.push(
                      Routes.newTicket(
                        category: SupportCategory.store.id,
                        subject: clip(
                          '${l10n.supportCatStore}: ${s.name}',
                          SupportRules.subjectMax,
                        ),
                        ref: '${l10n.supportRefStore(s.name)} (ID: ${s.id})',
                      ),
                    ),
                  ),
                ],
                const SizedBox(height: AppSpacing.xl),
                Text(
                  l10n.storeListingsTitle,
                  style: theme.textTheme.titleLarge?.copyWith(
                    fontWeight: FontWeight.w800,
                  ),
                ),
                const SizedBox(height: AppSpacing.md),
              ],
            ),
          ),
          ...listings.when(
            skipLoadingOnReload: true,
            loading: () => const [
              SliverToBoxAdapter(
                child: Padding(
                  padding: EdgeInsets.all(AppSpacing.xl),
                  child: Center(child: CircularProgressIndicator()),
                ),
              ),
            ],
            error: (e, _) => [
              SliverToBoxAdapter(
                child: ErrorView(
                  error: e,
                  onRetry: () => ref.invalidate(storeListingsProvider(s.slug)),
                ),
              ),
            ],
            data: (page) => page.items.isEmpty
                ? [
                    SliverToBoxAdapter(
                      child: Padding(
                        padding: const EdgeInsets.all(AppSpacing.xl),
                        child: Center(
                          child: Text(
                            l10n.storeNoListings,
                            style: TextStyle(color: scheme.onSurfaceVariant),
                          ),
                        ),
                      ),
                    ),
                  ]
                : [
                    SliverListingGrid(items: page.items),
                    if (page.loadingMore)
                      const SliverToBoxAdapter(
                        child: Padding(
                          padding: EdgeInsets.all(AppSpacing.lg),
                          child: Center(
                            child: SizedBox(
                              width: 24,
                              height: 24,
                              child: CircularProgressIndicator(
                                strokeWidth: 2.4,
                              ),
                            ),
                          ),
                        ),
                      ),
                  ],
          ),
          SliverToBoxAdapter(child: SizedBox(height: padding.bottom + 32)),
        ],
      ),
    );
  }
}

class _Stats extends StatelessWidget {
  const _Stats({required this.store});

  final PublicStore store;

  @override
  Widget build(BuildContext context) {
    final l10n = AppL10n.of(context);
    final scheme = Theme.of(context).colorScheme;
    final style = TextStyle(color: scheme.onSurfaceVariant, fontSize: 13.5);
    return Wrap(
      spacing: AppSpacing.lg,
      runSpacing: 4,
      children: [
        Text(l10n.storeListingsCount(store.activeListingCount), style: style),
        Text(l10n.storeViews(store.totalViewCount), style: style),
      ],
    );
  }
}

/// Follow / following, hidden on the user's own store.
class _FollowButton extends ConsumerWidget {
  const _FollowButton({required this.store});

  final PublicStore store;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final l10n = AppL10n.of(context);
    final signedIn = ref.watch(authControllerProvider).value != null;
    final mine = ref.watch(myStoreProvider).value;
    if (mine != null && mine.id == store.id) return const SizedBox.shrink();

    final status = signedIn ? ref.watch(followProvider(store.id)).value : null;
    final following = status?.isFollowing ?? false;

    Future<void> onTap() async {
      if (!signedIn) {
        final from = GoRouterState.of(context).uri.toString();
        context.push(
          Uri(path: Routes.phone, queryParameters: {'from': from}).toString(),
        );
        return;
      }
      final messenger = ScaffoldMessenger.of(context);
      try {
        await ref.read(followProvider(store.id).notifier).toggle();
      } catch (_) {
        messenger.showSnackBar(SnackBar(content: Text(l10n.actionFailed)));
      }
    }

    final label = following ? l10n.storeFollowingNow : l10n.storeFollow;
    final icon = following ? AppIcons.following : AppIcons.follow;
    return following
        ? OutlinedButton.icon(
            onPressed: onTap,
            style: OutlinedButton.styleFrom(minimumSize: const Size(0, 44)),
            icon: Icon(icon, size: 18),
            label: Text(label),
          )
        : FilledButton.icon(
            onPressed: onTap,
            style: FilledButton.styleFrom(minimumSize: const Size(0, 44)),
            icon: Icon(icon, size: 18),
            label: Text(label),
          );
  }
}

class _Contacts extends StatelessWidget {
  const _Contacts({required this.store});

  final PublicStore store;

  @override
  Widget build(BuildContext context) {
    final l10n = AppL10n.of(context);
    final scheme = Theme.of(context).colorScheme;

    Widget row(IconData icon, String text, VoidCallback? onTap) => InkWell(
      onTap: onTap,
      child: Padding(
        padding: const EdgeInsets.symmetric(
          horizontal: AppSpacing.lg,
          vertical: 13,
        ),
        child: Row(
          children: [
            Icon(icon, size: 20, color: scheme.primary),
            const SizedBox(width: AppSpacing.md),
            Expanded(
              child: Text(text, maxLines: 2, overflow: TextOverflow.ellipsis),
            ),
            if (onTap != null)
              Icon(AppIcons.forward, size: 18, color: scheme.onSurfaceVariant),
          ],
        ),
      ),
    );

    final rows = <Widget>[
      row(
        AppIcons.call,
        store.phone,
        () => openExternal(context, Uri(scheme: 'tel', path: store.phone)),
      ),
      if (store.email.isNotEmpty)
        row(
          AppIcons.mail,
          store.email,
          () => openExternal(context, Uri(scheme: 'mailto', path: store.email)),
        ),
      if (store.address.isNotEmpty) row(AppIcons.place, store.address, null),
      for (final (kind, url) in store.links)
        row(AppIcons.link, _kindName(kind), () {
          final uri = Uri.tryParse(url);
          if (uri != null) openExternal(context, uri);
        }),
    ];

    return Card(
      child: Column(
        children: [
          Padding(
            padding: const EdgeInsets.fromLTRB(
              AppSpacing.lg,
              AppSpacing.md,
              AppSpacing.lg,
              0,
            ),
            child: Align(
              alignment: AlignmentDirectional.centerStart,
              child: Text(
                l10n.storeContactTitle,
                style: const TextStyle(fontWeight: FontWeight.w800),
              ),
            ),
          ),
          for (var i = 0; i < rows.length; i++) ...[
            if (i > 0) const Divider(height: 1, indent: 56),
            rows[i],
          ],
        ],
      ),
    );
  }
}
