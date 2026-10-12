import 'dart:async';

import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../../app/router.dart';
import '../../../core/l10n/app_localizations.dart';
import '../../../core/theme/app_icons.dart';
import '../../../core/theme/app_spacing.dart';
import '../../../shared/widgets/async_states.dart';
import '../../../shared/widgets/glass.dart';
import '../../catalog/data/catalog_repository.dart';
import '../../catalog/domain/catalog_models.dart';
import '../../home/presentation/home_state.dart';
import '../../store/presentation/store_tile.dart';
import '../data/recent_searches.dart';
import 'search_controller.dart';
import 'store_search_controller.dart';

class SearchPage extends ConsumerStatefulWidget {
  const SearchPage({
    super.key,
    this.initialQuery = '',
    this.autofocus = false,
    this.initialStores = false,
  });

  final String initialQuery;
  final bool autofocus;

  /// Open on the stores tab.
  final bool initialStores;

  @override
  ConsumerState<SearchPage> createState() => _SearchPageState();
}

class _SearchPageState extends ConsumerState<SearchPage> {
  static const _debounce = Duration(milliseconds: 400);
  static const _barHeight = 70.0;

  late final SearchSeed _seed = SearchSeed(query: widget.initialQuery.trim());
  late final TextEditingController _text;
  final _scroll = ScrollController();
  final _focus = FocusNode();
  Timer? _timer;

  /// Whether the stores tab (instead of the listings) is shown.
  late bool _stores = widget.initialStores;

  /// What is being typed and not searched yet: while it is non-empty the
  /// page shows word suggestions instead of listings.
  String _typed = '';

  bool get _suggesting => !_stores && _typed.trim().length >= 2;

  @override
  void initState() {
    super.initState();
    _text = TextEditingController(text: widget.initialQuery);
    _scroll.addListener(_onScroll);
  }

  @override
  void dispose() {
    _timer?.cancel();
    _scroll.dispose();
    _focus.dispose();
    _text.dispose();
    super.dispose();
  }

  void _onScroll() {
    if (_stores && _scroll.hasClients && _scroll.position.extentAfter < 600) {
      final query = ref.read(searchFiltersProvider(_seed)).query;
      ref.read(storeSearchProvider(query).notifier).loadMore();
    }
  }

  void _onChanged(String value) {
    _timer?.cancel();
    if (_stores) {
      setState(() {}); // clear button visibility
      _timer = Timer(_debounce, () {
        if (!mounted) return;
        ref.read(searchFiltersProvider(_seed).notifier).setQuery(value);
      });
      return;
    }
    // Listings: suggest words while typing, search on submit or tap.
    if (value.trim().isEmpty) {
      ref.read(searchFiltersProvider(_seed).notifier).setQuery('');
      setState(() => _typed = '');
      return;
    }
    setState(() {}); // clear button visibility
    _timer = Timer(const Duration(milliseconds: 250), () {
      if (mounted) setState(() => _typed = value.trim());
    });
  }

  /// Shows the result on the home page: the feed there follows these words
  /// and category, and its filters know what that category accepts.
  Future<void> _showOnHome(String query, String? categoryId) async {
    var tree = const <Category>[];
    if (categoryId != null) {
      try {
        tree = await ref.read(categoriesProvider.future);
      } catch (_) {
        // Without the tree the words still search everywhere.
      }
    }
    if (!mounted) return;
    ref.read(homeQueryProvider.notifier).set(query);
    ref
        .read(homeCategoryPathProvider.notifier)
        .set(
          categoryId == null ? const [] : categoryChainIds(tree, categoryId),
        );
    _focus.unfocus();
    context.go(Routes.home);
  }

  void _submit(String value) {
    _timer?.cancel();
    final query = value.trim();
    if (_stores) {
      ref.read(searchFiltersProvider(_seed).notifier).setQuery(query);
      _focus.unfocus();
      return;
    }
    if (query.isNotEmpty) ref.read(recentSearchesProvider.notifier).add(query);
    _showOnHome(query, null);
  }

  void _applyPhrase(String phrase) {
    _text.text = phrase;
    _text.selection = TextSelection.collapsed(offset: phrase.length);
    _submit(phrase);
  }

  /// A drop-down line was tapped: search that word in its category, or just
  /// open the category.
  void _applySuggestion(SearchSuggestion s) {
    _timer?.cancel();
    final word = s.isCategory ? '' : s.text;
    if (word.isNotEmpty) ref.read(recentSearchesProvider.notifier).add(word);
    _showOnHome(word, s.categoryId);
  }

  void _clearText() {
    _text.clear();
    _timer?.cancel();
    ref.read(searchFiltersProvider(_seed).notifier).setQuery('');
    setState(() => _typed = '');
  }

  void _goBack() => context.canPop() ? context.pop() : context.go(Routes.home);

  @override
  Widget build(BuildContext context) {
    final l10n = AppL10n.of(context);
    final filters = ref.watch(searchFiltersProvider(_seed));
    final recents = ref.watch(recentSearchesProvider);
    final padding = MediaQuery.paddingOf(context);

    final showRecents = !_stores && recents.isNotEmpty;

    return Scaffold(
      body: Stack(
        children: [
          CustomScrollView(
            controller: _scroll,
            keyboardDismissBehavior: ScrollViewKeyboardDismissBehavior.onDrag,
            slivers: [
              SliverToBoxAdapter(
                child: SizedBox(height: padding.top + _barHeight),
              ),
              if (_suggesting)
                ..._suggestionSlivers(context, ref)
              else ...[
                SliverToBoxAdapter(
                  child: Padding(
                    padding: const EdgeInsets.fromLTRB(
                      AppSpacing.lg,
                      0,
                      AppSpacing.lg,
                      AppSpacing.sm,
                    ),
                    child: _ModeTabs(
                      storesSelected: _stores,
                      onChanged: (stores) => setState(() => _stores = stores),
                    ),
                  ),
                ),
                if (showRecents)
                  SliverToBoxAdapter(
                    child: _Recents(
                      phrases: recents,
                      onTap: _applyPhrase,
                      onClear: () =>
                          ref.read(recentSearchesProvider.notifier).clear(),
                    ),
                  ),
                if (_stores) ..._storeSlivers(context, ref, filters.query),
              ],
              SliverToBoxAdapter(child: SizedBox(height: padding.bottom + 32)),
            ],
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
                    onPressed: _goBack,
                  ),
                  const SizedBox(width: 10),
                  Expanded(
                    child: GlassSurface(
                      radius: AppRadius.pill,
                      shadow: false,
                      padding: const EdgeInsets.only(left: 16, right: 6),
                      child: SizedBox(
                        height: 46,
                        child: Row(
                          children: [
                            Icon(
                              AppIcons.search,
                              size: 22,
                              color: Theme.of(context)
                                  .colorScheme
                                  .onSurfaceVariant,
                            ),
                            const SizedBox(width: 10),
                            Expanded(
                              child: TextField(
                                controller: _text,
                                focusNode: _focus,
                                autofocus: widget.autofocus,
                                textInputAction: TextInputAction.search,
                                onChanged: _onChanged,
                                onSubmitted: _submit,
                                style: const TextStyle(fontSize: 15.5),
                                decoration: InputDecoration(
                                  hintText: l10n.searchHint,
                                  isCollapsed: true,
                                  filled: false,
                                  contentPadding: EdgeInsets.zero,
                                  border: InputBorder.none,
                                  enabledBorder: InputBorder.none,
                                  focusedBorder: InputBorder.none,
                                ),
                              ),
                            ),
                            if (_text.text.isNotEmpty)
                              IconButton(
                                tooltip: l10n.clearSearch,
                                onPressed: _clearText,
                                icon: const Icon(AppIcons.close, size: 20),
                              ),
                          ],
                        ),
                      ),
                    ),
                  ),
                ],
              ),
            ),
          ),
        ],
      ),
    );
  }

  List<Widget> _suggestionSlivers(BuildContext context, WidgetRef ref) {
    final l10n = AppL10n.of(context);
    final scheme = Theme.of(context).colorScheme;
    final suggestions = ref.watch(searchSuggestionsProvider(_typed));
    final list = suggestions.value;

    if (list == null) {
      // Loading or failed: nothing to suggest, the user can still submit.
      return const [SliverToBoxAdapter(child: SizedBox(height: 8))];
    }

    return [
      SliverList.separated(
        itemCount: list.length,
        separatorBuilder: (_, _) => Divider(
          height: 1,
          indent: AppSpacing.lg,
          color: scheme.outlineVariant,
        ),
        itemBuilder: (context, i) {
          final s = list[i];
          final path = s.isCategory
              ? (s.path.isEmpty
                    ? l10n.searchSuggestionCategory
                    : s.path.join(' › '))
              : s.path.join(' › ');
          // The wave starts under the finger and spreads from there.
          return Material(
            type: MaterialType.transparency,
            child: InkWell(
              splashFactory: InkRipple.splashFactory,
              splashColor: scheme.primary.withValues(alpha: 0.20),
              highlightColor: scheme.primary.withValues(alpha: 0.06),
              onTap: () => _applySuggestion(s),
              child: Padding(
                padding: const EdgeInsets.symmetric(
                  horizontal: AppSpacing.lg,
                  vertical: 12,
                ),
                child: Row(
                  children: [
                    Expanded(
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          Text(
                            s.text,
                            maxLines: 1,
                            overflow: TextOverflow.ellipsis,
                            style: const TextStyle(
                              fontWeight: FontWeight.w700,
                              fontSize: 16,
                            ),
                          ),
                          if (path.isNotEmpty)
                            Text(
                              path,
                              maxLines: 1,
                              overflow: TextOverflow.ellipsis,
                              style: TextStyle(
                                fontSize: 14,
                                color: scheme.onSurfaceVariant,
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
        },
      ),
    ];
  }

  List<Widget> _storeSlivers(
    BuildContext context,
    WidgetRef ref,
    String query,
  ) {
    final l10n = AppL10n.of(context);
    final stores = ref.watch(storeSearchProvider(query));

    return stores.when(
      skipLoadingOnReload: true,
      loading: () => [
        const SliverToBoxAdapter(
          child: Padding(
            padding: EdgeInsets.symmetric(vertical: 64),
            child: Center(child: CircularProgressIndicator()),
          ),
        ),
      ],
      error: (e, _) => [
        SliverToBoxAdapter(
          child: Padding(
            padding: const EdgeInsets.symmetric(vertical: 32),
            child: ErrorView(
              error: e,
              onRetry: () => ref.invalidate(storeSearchProvider(query)),
            ),
          ),
        ),
      ],
      data: (state) => [
        SliverToBoxAdapter(
          child: Padding(
            padding: const EdgeInsets.fromLTRB(
              AppSpacing.lg,
              AppSpacing.md,
              AppSpacing.lg,
              AppSpacing.md,
            ),
            child: Text(
              l10n.storesFound(state.totalCount),
              style: TextStyle(
                fontWeight: FontWeight.w600,
                color: Theme.of(context).colorScheme.onSurfaceVariant,
              ),
            ),
          ),
        ),
        if (state.items.isEmpty)
          SliverToBoxAdapter(
            child: Padding(
              padding: const EdgeInsets.symmetric(vertical: 32),
              child: EmptyView(
                icon: AppIcons.store,
                title: l10n.noStoresFound,
                message: l10n.noStoresHint,
              ),
            ),
          )
        else
          SliverPadding(
            padding: AppSpacing.screenPadding,
            sliver: SliverList.separated(
              itemCount: state.items.length,
              separatorBuilder: (_, _) => const SizedBox(height: AppSpacing.sm),
              itemBuilder: (context, i) => StoreTile(store: state.items[i]),
            ),
          ),
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
    );
  }
}

/// "Listings | Stores" switch under the search bar.
class _ModeTabs extends StatelessWidget {
  const _ModeTabs({required this.storesSelected, required this.onChanged});

  final bool storesSelected;
  final ValueChanged<bool> onChanged;

  @override
  Widget build(BuildContext context) {
    final l10n = AppL10n.of(context);
    final scheme = Theme.of(context).colorScheme;

    Widget tab(String label, bool stores) {
      final selected = storesSelected == stores;
      return Expanded(
        child: Semantics(
          button: true,
          selected: selected,
          label: label,
          child: GestureDetector(
            onTap: () => onChanged(stores),
            behavior: HitTestBehavior.opaque,
            child: AnimatedContainer(
              duration: const Duration(milliseconds: 180),
              height: 40,
              alignment: Alignment.center,
              decoration: BoxDecoration(
                color: selected ? scheme.primary : Colors.transparent,
                borderRadius: BorderRadius.circular(AppRadius.pill),
              ),
              child: Text(
                label,
                style: TextStyle(
                  fontWeight: FontWeight.w700,
                  fontSize: 14.5,
                  color: selected ? scheme.onPrimary : scheme.onSurface,
                ),
              ),
            ),
          ),
        ),
      );
    }

    return Container(
      padding: const EdgeInsets.all(3),
      decoration: BoxDecoration(
        color: scheme.surfaceContainerHighest.withValues(alpha: 0.6),
        borderRadius: BorderRadius.circular(AppRadius.pill),
      ),
      child: Row(
        children: [
          tab(l10n.searchTabListings, false),
          tab(l10n.searchTabStores, true),
        ],
      ),
    );
  }
}

class _Recents extends StatelessWidget {
  const _Recents({
    required this.phrases,
    required this.onTap,
    required this.onClear,
  });

  final List<String> phrases;
  final ValueChanged<String> onTap;
  final VoidCallback onClear;

  @override
  Widget build(BuildContext context) {
    final l10n = AppL10n.of(context);
    final scheme = Theme.of(context).colorScheme;

    return Padding(
      padding: const EdgeInsets.fromLTRB(
        AppSpacing.lg,
        AppSpacing.xl,
        AppSpacing.lg,
        0,
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: [
              Expanded(
                child: Text(
                  l10n.searchRecent,
                  style: Theme.of(context).textTheme.titleMedium
                      ?.copyWith(fontWeight: FontWeight.w800),
                ),
              ),
              GestureDetector(
                onTap: onClear,
                behavior: HitTestBehavior.opaque,
                child: Text(
                  l10n.clearAll,
                  style: TextStyle(
                    color: scheme.primary,
                    fontWeight: FontWeight.w600,
                    fontSize: 14,
                  ),
                ),
              ),
            ],
          ),
          const SizedBox(height: AppSpacing.md),
          Wrap(
            spacing: AppSpacing.sm,
            runSpacing: AppSpacing.sm,
            children: [
              for (final phrase in phrases)
                Material(
                  color: scheme.surface,
                  shape: RoundedRectangleBorder(
                    borderRadius: BorderRadius.circular(AppRadius.pill),
                    side: BorderSide(color: scheme.outline),
                  ),
                  clipBehavior: Clip.antiAlias,
                  child: InkWell(
                    splashFactory: InkRipple.splashFactory,
                    splashColor: scheme.primary.withValues(alpha: 0.20),
                    onTap: () => onTap(phrase),
                    child: Padding(
                      padding: const EdgeInsets.symmetric(
                        horizontal: 12,
                        vertical: 8,
                      ),
                      child: Row(
                        mainAxisSize: MainAxisSize.min,
                        children: [
                          Icon(
                            AppIcons.history,
                            size: 18,
                            color: scheme.onSurfaceVariant,
                          ),
                          const SizedBox(width: 6),
                          Text(
                            phrase,
                            style: const TextStyle(
                              fontSize: 14,
                              fontWeight: FontWeight.w600,
                            ),
                          ),
                        ],
                      ),
                    ),
                  ),
                ),
            ],
          ),
        ],
      ),
    );
  }
}
