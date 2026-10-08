import 'dart:async';

import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:intl/intl.dart';

import '../../../app/router.dart';
import '../../../core/l10n/app_localizations.dart';
import '../../../core/theme/app_icons.dart';
import '../../../core/theme/app_spacing.dart';
import '../../../shared/widgets/async_states.dart';
import '../../../shared/widgets/glass.dart';
import '../../../shared/widgets/listing_grid.dart';
import '../../catalog/data/catalog_repository.dart';
import '../data/recent_searches.dart';
import 'price_filter_sheet.dart';
import 'search_controller.dart';

class SearchPage extends ConsumerStatefulWidget {
  const SearchPage({
    super.key,
    this.initialQuery = '',
    this.initialCategoryId,
    this.autofocus = false,
  });

  final String initialQuery;
  final String? initialCategoryId;
  final bool autofocus;

  @override
  ConsumerState<SearchPage> createState() => _SearchPageState();
}

class _SearchPageState extends ConsumerState<SearchPage> {
  static const _debounce = Duration(milliseconds: 400);
  static const _barHeight = 70.0;

  late final SearchSeed _seed = SearchSeed(
    query: widget.initialQuery.trim(),
    categoryId: widget.initialCategoryId,
  );
  late final TextEditingController _text;
  final _scroll = ScrollController();
  final _focus = FocusNode();
  Timer? _timer;

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
    if (_scroll.hasClients && _scroll.position.extentAfter < 600) {
      ref.read(searchResultsProvider(_seed).notifier).loadMore();
    }
  }

  void _onChanged(String value) {
    setState(() {}); // clear button visibility
    _timer?.cancel();
    _timer = Timer(_debounce, () {
      if (!mounted) return;
      ref.read(searchFiltersProvider(_seed).notifier).setQuery(value);
    });
  }

  void _submit(String value) {
    _timer?.cancel();
    ref.read(searchFiltersProvider(_seed).notifier).setQuery(value);
    ref.read(recentSearchesProvider.notifier).add(value);
    _focus.unfocus();
  }

  void _applyPhrase(String phrase) {
    _text.text = phrase;
    _text.selection = TextSelection.collapsed(offset: phrase.length);
    _submit(phrase);
  }

  void _clearText() {
    _text.clear();
    _timer?.cancel();
    ref.read(searchFiltersProvider(_seed).notifier).setQuery('');
    setState(() {});
  }

  Future<void> _openFilters() async {
    final filters = ref.read(searchFiltersProvider(_seed));
    final result = await showPriceFilterSheet(
      context,
      min: filters.minPrice,
      max: filters.maxPrice,
    );
    if (result == null || !mounted) return;
    ref
        .read(searchFiltersProvider(_seed).notifier)
        .setPrice(min: result.min, max: result.max);
  }

  void _goBack() => context.canPop() ? context.pop() : context.go(Routes.home);

  @override
  Widget build(BuildContext context) {
    final l10n = AppL10n.of(context);
    final filters = ref.watch(searchFiltersProvider(_seed));
    final results = ref.watch(searchResultsProvider(_seed));
    final recents = ref.watch(recentSearchesProvider);
    final categories = ref.watch(categoriesProvider).value ?? const [];
    final padding = MediaQuery.paddingOf(context);
    final locale = Localizations.localeOf(context).toString();

    final showRecents = filters.isEmpty && recents.isNotEmpty;

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
              SliverToBoxAdapter(
                child: SizedBox(
                  height: 46,
                  child: ListView(
                    scrollDirection: Axis.horizontal,
                    padding: AppSpacing.screenPadding,
                    children: [
                      _CategoryChip(
                        label: l10n.allCategories,
                        selected: filters.categoryId == null,
                        onTap: () => ref
                            .read(searchFiltersProvider(_seed).notifier)
                            .setCategory(null),
                      ),
                      for (final c in categories) ...[
                        const SizedBox(width: AppSpacing.sm),
                        _CategoryChip(
                          label: c.name,
                          selected: filters.categoryId == c.id,
                          onTap: () => ref
                              .read(searchFiltersProvider(_seed).notifier)
                              .setCategory(c.id),
                        ),
                      ],
                    ],
                  ),
                ),
              ),
              if (filters.hasPrice)
                SliverToBoxAdapter(
                  child: Padding(
                    padding: const EdgeInsets.fromLTRB(
                      AppSpacing.lg,
                      AppSpacing.sm,
                      AppSpacing.lg,
                      0,
                    ),
                    child: Align(
                      alignment: AlignmentDirectional.centerStart,
                      child: _RemovableChip(
                        label: _priceLabel(filters, locale),
                        onRemove: () => ref
                            .read(searchFiltersProvider(_seed).notifier)
                            .setPrice(),
                      ),
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
              ...results.when(
                // Keep the previous results on screen while a refinement loads.
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
                        onRetry: () =>
                            ref.invalidate(searchResultsProvider(_seed)),
                      ),
                    ),
                  ),
                ],
                data: (state) => [
                  SliverToBoxAdapter(
                    child: Padding(
                      padding: const EdgeInsets.fromLTRB(
                        AppSpacing.lg,
                        AppSpacing.lg,
                        AppSpacing.lg,
                        AppSpacing.md,
                      ),
                      child: Text(
                        l10n.resultsFound(state.totalCount),
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
                          icon: AppIcons.search,
                          title: l10n.noResults,
                          message: l10n.noResultsHint,
                          action: filters.hasActiveFilters
                              ? OutlinedButton(
                                  onPressed: () => ref
                                      .read(
                                        searchFiltersProvider(_seed).notifier,
                                      )
                                      .clearFilters(),
                                  style: OutlinedButton.styleFrom(
                                    minimumSize: const Size(200, 52),
                                  ),
                                  child: Text(l10n.clearFilters),
                                )
                              : null,
                        ),
                      ),
                    )
                  else ...[
                    SliverListingGrid(
                      items: state.items,
                      onOpen: (_) {
                        if (filters.query.isNotEmpty) {
                          ref
                              .read(recentSearchesProvider.notifier)
                              .add(filters.query);
                        }
                      },
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
                  ],
                ],
              ),
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
                  const SizedBox(width: 10),
                  GlassIconButton(
                    icon: AppIcons.filter,
                    semanticLabel: l10n.filters,
                    showDot: filters.hasPrice,
                    onPressed: _openFilters,
                  ),
                ],
              ),
            ),
          ),
        ],
      ),
    );
  }

  String _priceLabel(SearchFilters f, String locale) {
    final fmt = NumberFormat.decimalPattern(locale);
    final min = f.minPrice == null ? null : fmt.format(f.minPrice);
    final max = f.maxPrice == null ? null : fmt.format(f.maxPrice);
    if (min != null && max != null) return '$min – $max AZN';
    if (min != null) return '≥ $min AZN';
    return '≤ $max AZN';
  }
}

class _CategoryChip extends StatelessWidget {
  const _CategoryChip({
    required this.label,
    required this.selected,
    required this.onTap,
  });

  final String label;
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
          duration: const Duration(milliseconds: 180),
          padding: const EdgeInsets.symmetric(horizontal: 16),
          alignment: Alignment.center,
          decoration: BoxDecoration(
            color: selected ? scheme.primary : scheme.surface,
            borderRadius: BorderRadius.circular(AppRadius.pill),
            border: Border.all(
              color: selected ? scheme.primary : scheme.outline,
            ),
          ),
          child: Text(
            label,
            style: TextStyle(
              fontWeight: FontWeight.w600,
              fontSize: 14,
              color: selected ? scheme.onPrimary : scheme.onSurface,
            ),
          ),
        ),
      ),
    );
  }
}

class _RemovableChip extends StatelessWidget {
  const _RemovableChip({required this.label, required this.onRemove});

  final String label;
  final VoidCallback onRemove;

  @override
  Widget build(BuildContext context) {
    final scheme = Theme.of(context).colorScheme;
    return GestureDetector(
      onTap: onRemove,
      child: Container(
        padding: const EdgeInsets.fromLTRB(14, 7, 8, 7),
        decoration: BoxDecoration(
          color: scheme.primary.withValues(alpha: 0.12),
          borderRadius: BorderRadius.circular(AppRadius.pill),
        ),
        child: Row(
          mainAxisSize: MainAxisSize.min,
          children: [
            Text(
              label,
              style: TextStyle(
                fontWeight: FontWeight.w700,
                fontSize: 13.5,
                color: scheme.primary,
              ),
            ),
            const SizedBox(width: 4),
            Icon(AppIcons.close, size: 16, color: scheme.primary),
          ],
        ),
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
                GestureDetector(
                  onTap: () => onTap(phrase),
                  child: Container(
                    padding: const EdgeInsets.symmetric(
                      horizontal: 12,
                      vertical: 8,
                    ),
                    decoration: BoxDecoration(
                      color: scheme.surface,
                      borderRadius: BorderRadius.circular(AppRadius.pill),
                      border: Border.all(color: scheme.outline),
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
            ],
          ),
        ],
      ),
    );
  }
}
