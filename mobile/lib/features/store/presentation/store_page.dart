import 'package:cached_network_image/cached_network_image.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import 'dart:async';

import '../../../app/router.dart';
import '../../../core/l10n/app_localizations.dart';
import '../../../core/network/api_exception.dart';
import '../../../core/theme/app_icons.dart';
import '../../../core/theme/app_spacing.dart';
import '../../../shared/utils/external_links.dart';
import '../../../shared/widgets/async_states.dart';
import '../../../shared/widgets/brand_icon.dart';
import '../../../shared/widgets/glass.dart';
import '../../../shared/widgets/listing_grid.dart';
import '../../auth/presentation/auth_controller.dart';
import '../../listing_details/presentation/message_sheet.dart';
import '../../messages/presentation/inbox_controller.dart';
import '../data/store_repository.dart';
import '../../report/domain/report_models.dart';
import '../../report/presentation/report_sheet.dart';
import '../../support/presentation/support_widgets.dart';
import '../../catalog/domain/catalog_models.dart' show ListingSummary;
import '../domain/store_models.dart';
import 'store_controller.dart';

String _kindName(SocialKind k) => switch (k) {
  SocialKind.website => 'Website',
  SocialKind.instagram => 'Instagram',
  SocialKind.tiktok => 'TikTok',
  SocialKind.youtube => 'YouTube',
  SocialKind.facebook => 'Facebook',
};

Brand? _brandOf(SocialKind k) => switch (k) {
  SocialKind.instagram => Brand.instagram,
  SocialKind.tiktok => Brand.tiktok,
  SocialKind.youtube => Brand.youtube,
  SocialKind.facebook => Brand.facebook,
  SocialKind.website => null,
};

/// The seven days, Monday first (the order the backend counts them in).
List<String> weekdayNames(AppL10n l10n) => [
  l10n.weekdayMon,
  l10n.weekdayTue,
  l10n.weekdayWed,
  l10n.weekdayThu,
  l10n.weekdayFri,
  l10n.weekdaySat,
  l10n.weekdaySun,
];

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
  bool _moreVip = false;
  bool _moreRegular = false;

  static const _vipFirst = 4;
  static const _regularFirst = 6;

  @override
  void initState() {
    super.initState();
    // The page was opened: the owner counts it (the owner own visits are left out by the server).
    unawaited(
      ref.read(storeRepositoryProvider).reportEvent(widget.store.slug, 'view'),
    );
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

  void _toggleVip() => setState(() => _moreVip = !_moreVip);

  void _toggleRegular() => setState(() => _moreRegular = !_moreRegular);

  void _signInThen() {
    final from = GoRouterState.of(context).uri.toString();
    context.push(
      Uri(path: Routes.phone, queryParameters: {'from': from}).toString(),
    );
  }

  /// "Write to the store" starts a conversation about the newest of its listings.
  Future<void> _write(String listingId) async {
    if (ref.read(authControllerProvider).value == null) {
      _signInThen();
      return;
    }
    final conversationId = await showMessageSheet(
      context,
      listingId: listingId,
      initialText: AppL10n.of(context).storeFirstMessage,
    );
    if (conversationId == null || !mounted) return;
    unawaited(ref.read(storeRepositoryProvider).reportEvent(s.slug, 'message'));
    unawaited(ref.read(inboxProvider.notifier).refresh());
    unawaited(context.push(Routes.chat(conversationId)));
  }

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
                _Actions(
                  store: s,
                  onWrite: listings.value?.items.isNotEmpty == true
                      ? () => _write(listings.value!.items.first.id)
                      : null,
                ),
                const SizedBox(height: AppSpacing.lg),
                _Contacts(store: s),
                if (s.hours.isNotEmpty) ...[
                  const SizedBox(height: AppSpacing.md),
                  _HoursCard(store: s),
                ],
                if (ref.watch(myStoreProvider).value?.id != s.id) ...[
                  const SizedBox(height: AppSpacing.sm),
                  ReportProblemRow(
                    hint: l10n.reportStoreHint,
                    onTap: () => startReport(
                      context,
                      ref,
                      target: ReportTarget.store,
                      targetId: s.id,
                    ),
                  ),
                ],
                const SizedBox(height: AppSpacing.xl),
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
                    ..._sections(context, page.items),
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

extension on _StoreBodyState {
  /// The listings in two parts: the VIP ones (4 at first) and the ordinary ones (6 at first).
  List<Widget> _sections(BuildContext context, List<ListingSummary> items) {
    final l10n = AppL10n.of(context);
    final theme = Theme.of(context);
    final vip = [
      for (final l in items)
        if (l.isVip) l,
    ];
    final regular = [
      for (final l in items)
        if (!l.isVip) l,
    ];

    Widget title(String text, int count) => SliverToBoxAdapter(
      child: Padding(
        padding: const EdgeInsets.fromLTRB(
          AppSpacing.lg,
          0,
          AppSpacing.lg,
          AppSpacing.md,
        ),
        child: Text(
          '$text ($count)',
          style: theme.textTheme.titleLarge?.copyWith(
            fontWeight: FontWeight.w800,
          ),
        ),
      ),
    );

    Widget toggle(bool open, int total, int first, VoidCallback onTap) =>
        total <= first
        ? const SliverToBoxAdapter(child: SizedBox.shrink())
        : SliverToBoxAdapter(
            child: Center(
              child: TextButton(
                onPressed: onTap,
                child: Text(open ? l10n.storeShowLess : l10n.storeShowAll),
              ),
            ),
          );

    return [
      if (vip.isNotEmpty) ...[
        title(l10n.storeVipTitle, vip.length),
        SliverListingGrid(
          items: _moreVip ? vip : vip.take(_StoreBodyState._vipFirst).toList(),
        ),
        toggle(_moreVip, vip.length, _StoreBodyState._vipFirst, _toggleVip),
      ],
      if (regular.isNotEmpty) ...[
        title(l10n.storeListingsTitle, regular.length),
        SliverListingGrid(
          items: _moreRegular
              ? regular
              : regular.take(_StoreBodyState._regularFirst).toList(),
        ),
        toggle(
          _moreRegular,
          regular.length,
          _StoreBodyState._regularFirst,
          _toggleRegular,
        ),
      ],
    ];
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
        Text(l10n.storeFollowers(store.followerCount), style: style),
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

/// Write to the store, call it, and the round logos of its WhatsApp and networks.
class _Actions extends ConsumerWidget {
  const _Actions({required this.store, required this.onWrite});

  final PublicStore store;
  final VoidCallback? onWrite;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final l10n = AppL10n.of(context);
    final scheme = Theme.of(context).colorScheme;
    final mine = ref.watch(myStoreProvider).value;
    final isOwner = mine != null && mine.id == store.id;
    final repo = ref.read(storeRepositoryProvider);

    Widget round(Widget icon, String label, VoidCallback onTap) => Semantics(
      button: true,
      label: label,
      child: InkResponse(
        onTap: onTap,
        radius: 28,
        child: Container(
          width: 48,
          height: 48,
          decoration: BoxDecoration(
            color: scheme.surface,
            shape: BoxShape.circle,
            border: Border.all(color: scheme.outlineVariant),
          ),
          child: Center(child: icon),
        ),
      ),
    );

    final wa = store.whatsAppUri;
    return Wrap(
      spacing: AppSpacing.md,
      runSpacing: AppSpacing.md,
      crossAxisAlignment: WrapCrossAlignment.center,
      children: [
        if (!isOwner && onWrite != null)
          FilledButton.tonalIcon(
            onPressed: onWrite,
            style: FilledButton.styleFrom(minimumSize: const Size(0, 48)),
            icon: const Icon(AppIcons.messages, size: 20),
            label: Text(l10n.storeWriteTo),
          ),
        if (store.phone.isNotEmpty)
          OutlinedButton.icon(
            onPressed: () {
              unawaited(repo.reportEvent(store.slug, 'call'));
              openExternal(context, Uri(scheme: 'tel', path: store.phone));
            },
            style: OutlinedButton.styleFrom(minimumSize: const Size(0, 48)),
            icon: const Icon(AppIcons.call, size: 20),
            label: Text(l10n.storeCall),
          ),
        if (wa != null)
          round(const BrandIcon(Brand.whatsapp, size: 26), 'WhatsApp', () {
            unawaited(repo.reportEvent(store.slug, 'whatsapp'));
            openExternal(context, wa);
          }),
        for (final (kind, url) in store.links)
          if (_brandOf(kind) case final brand?)
            round(BrandIcon(brand, size: 24), _kindName(kind), () {
              final uri = Uri.tryParse(url);
              if (uri != null) openExternal(context, uri);
            }),
      ],
    );
  }
}

/// The hours in one line (a clock, open or closed, today). Tapping it opens the whole week.
class _HoursCard extends StatefulWidget {
  const _HoursCard({required this.store});

  final PublicStore store;

  @override
  State<_HoursCard> createState() => _HoursCardState();
}

class _HoursCardState extends State<_HoursCard> {
  bool _shown = false;

  @override
  Widget build(BuildContext context) {
    final store = widget.store;
    final l10n = AppL10n.of(context);
    final scheme = Theme.of(context).colorScheme;
    final names = weekdayNames(l10n);
    final now = DateTime.now().toUtc().add(const Duration(hours: 4));
    final open = StoreHours.openAt(store.hours, DateTime.now());
    final todayHours = store.hours
        .where((h) => h.day == now.weekday)
        .firstOrNull;

    return Card(
      clipBehavior: Clip.antiAlias,
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          InkWell(
            onTap: () => setState(() => _shown = !_shown),
            child: Padding(
              padding: const EdgeInsets.symmetric(
                horizontal: AppSpacing.lg,
                vertical: AppSpacing.md,
              ),
              child: Row(
                children: [
                  Container(
                    width: 36,
                    height: 36,
                    decoration: BoxDecoration(
                      color: scheme.primary.withValues(alpha: 0.12),
                      shape: BoxShape.circle,
                    ),
                    child: Icon(
                      AppIcons.schedule,
                      size: 20,
                      color: scheme.primary,
                    ),
                  ),
                  const SizedBox(width: AppSpacing.md),
                  Expanded(
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Text(
                          l10n.storeHoursTitle,
                          style: const TextStyle(fontWeight: FontWeight.w800),
                        ),
                        Text(
                          todayHours == null
                              ? l10n.storeTodayClosed
                              : '${StoreHours.clock(todayHours.openMinute)} – '
                                    '${StoreHours.clock(todayHours.closeMinute)}',
                          style: TextStyle(
                            fontSize: 12.5,
                            color: scheme.onSurfaceVariant,
                          ),
                        ),
                      ],
                    ),
                  ),
                  if (open != null)
                    Container(
                      padding: const EdgeInsets.symmetric(
                        horizontal: 10,
                        vertical: 4,
                      ),
                      decoration: BoxDecoration(
                        color: (open ? Colors.green : scheme.onSurfaceVariant)
                            .withValues(alpha: 0.14),
                        borderRadius: BorderRadius.circular(AppRadius.sm),
                      ),
                      child: Text(
                        open ? l10n.storeOpenNow : l10n.storeClosedNow,
                        style: TextStyle(
                          fontSize: 12.5,
                          fontWeight: FontWeight.w700,
                          color: open ? Colors.green : scheme.onSurfaceVariant,
                        ),
                      ),
                    ),
                  const SizedBox(width: 4),
                  AnimatedRotation(
                    turns: _shown ? 0.5 : 0,
                    duration: const Duration(milliseconds: 160),
                    child: Icon(
                      Icons.expand_more_rounded,
                      color: scheme.onSurfaceVariant,
                    ),
                  ),
                ],
              ),
            ),
          ),
          if (_shown)
            Padding(
              padding: const EdgeInsets.fromLTRB(
                AppSpacing.md,
                0,
                AppSpacing.md,
                AppSpacing.md,
              ),
              child: Column(
                children: [
                  for (var i = 0; i < 7; i++)
                    Builder(
                      builder: (context) {
                        final day = store.hours
                            .where((h) => h.day == i + 1)
                            .firstOrNull;
                        final today = now.weekday == i + 1;
                        return Container(
                          padding: const EdgeInsets.symmetric(
                            horizontal: 10,
                            vertical: 7,
                          ),
                          decoration: today
                              ? BoxDecoration(
                                  color: scheme.primary.withValues(alpha: 0.10),
                                  borderRadius: BorderRadius.circular(
                                    AppRadius.sm,
                                  ),
                                )
                              : null,
                          child: Row(
                            children: [
                              Expanded(
                                child: Text(
                                  names[i],
                                  style: TextStyle(
                                    fontWeight: today
                                        ? FontWeight.w800
                                        : FontWeight.w500,
                                    color: today ? scheme.primary : null,
                                  ),
                                ),
                              ),
                              Text(
                                day == null
                                    ? l10n.storeDayClosed
                                    : '${StoreHours.clock(day.openMinute)} – '
                                          '${StoreHours.clock(day.closeMinute)}',
                                style: TextStyle(
                                  color: day == null
                                      ? scheme.onSurfaceVariant
                                      : null,
                                ),
                              ),
                            ],
                          ),
                        );
                      },
                    ),
                ],
              ),
            ),
        ],
      ),
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
      if (store.website.isNotEmpty)
        row(AppIcons.link, store.website, () {
          final uri = Uri.tryParse(store.website);
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

/// Search and price range over the listings of this store.
