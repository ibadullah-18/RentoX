import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/l10n/app_localizations.dart';
import '../../../core/theme/app_icons.dart';
import '../../../core/theme/app_spacing.dart';
import '../../../shared/widgets/async_states.dart';
import '../data/locations_repository.dart';
import '../domain/location_models.dart';

/// Normal letters for matching: "ə" finds "e" and so on.
String _fold(String s) => s
    .toLowerCase()
    .replaceAll('ə', 'e')
    .replaceAll('ı', 'i')
    .replaceAll('ö', 'o')
    .replaceAll('ü', 'u')
    .replaceAll('ğ', 'g')
    .replaceAll('ç', 'c')
    .replaceAll('ş', 's');

/// Choose a city and, for cities that have districts, a district.
///
/// With [forFilter] the user may also pick "all cities" / "all districts";
/// picking "all cities" returns [PlaceChoice.none]. Returns `null` when the
/// sheet is dismissed.
Future<PlaceChoice?> showLocationPicker(
  BuildContext context, {
  PlaceChoice current = PlaceChoice.none,
  bool forFilter = false,
}) {
  return showModalBottomSheet<PlaceChoice>(
    context: context,
    isScrollControlled: true,
    useSafeArea: true,
    useRootNavigator: true,
    showDragHandle: true,
    shape: const RoundedRectangleBorder(
      borderRadius: BorderRadius.vertical(top: Radius.circular(AppRadius.xl)),
    ),
    builder: (_) => _LocationSheet(current: current, forFilter: forFilter),
  );
}

class _LocationSheet extends ConsumerStatefulWidget {
  const _LocationSheet({required this.current, required this.forFilter});

  final PlaceChoice current;
  final bool forFilter;

  @override
  ConsumerState<_LocationSheet> createState() => _LocationSheetState();
}

class _LocationSheetState extends ConsumerState<_LocationSheet> {
  /// The city whose districts are shown (second step); `null` = the cities.
  City? _city;
  String _query = '';

  void _pop(PlaceChoice choice) => Navigator.of(context).pop(choice);

  @override
  Widget build(BuildContext context) {
    final l10n = AppL10n.of(context);
    final theme = Theme.of(context);

    return SizedBox(
      height: MediaQuery.sizeOf(context).height * 0.78,
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Padding(
            padding: const EdgeInsets.fromLTRB(
              AppSpacing.sm,
              0,
              AppSpacing.lg,
              AppSpacing.sm,
            ),
            child: Row(
              children: [
                if (_city != null)
                  IconButton(
                    tooltip: l10n.backAction,
                    onPressed: () => setState(() {
                      _city = null;
                      _query = '';
                    }),
                    icon: const Icon(AppIcons.back),
                  )
                else
                  const SizedBox(width: AppSpacing.md),
                Expanded(
                  child: Text(
                    _city == null ? l10n.locationChooseCity : _city!.name,
                    maxLines: 1,
                    overflow: TextOverflow.ellipsis,
                    style: theme.textTheme.titleLarge?.copyWith(
                      fontWeight: FontWeight.w800,
                    ),
                  ),
                ),
              ],
            ),
          ),
          if (_city == null)
            Padding(
              padding: const EdgeInsets.fromLTRB(
                AppSpacing.lg,
                0,
                AppSpacing.lg,
                AppSpacing.sm,
              ),
              child: TextField(
                onChanged: (v) => setState(() => _query = v),
                decoration: InputDecoration(
                  hintText: l10n.locationSearchHint,
                  prefixIcon: const Icon(AppIcons.search),
                  isDense: true,
                ),
              ),
            ),
          Expanded(
            child: _city == null ? _cities(context) : _districts(context),
          ),
        ],
      ),
    );
  }

  Widget _cities(BuildContext context) {
    final l10n = AppL10n.of(context);
    final cities = ref.watch(citiesProvider);

    return cities.when(
      loading: () => const Center(child: CircularProgressIndicator()),
      error: (e, _) =>
          ErrorView(error: e, onRetry: () => ref.invalidate(citiesProvider)),
      data: (all) {
        final q = _fold(_query.trim());
        final shown = q.isEmpty
            ? all
            : all.where((c) => _fold(c.name).contains(q)).toList();

        return ListView(
          padding: EdgeInsets.fromLTRB(
            AppSpacing.lg,
            0,
            AppSpacing.lg,
            MediaQuery.paddingOf(context).bottom + AppSpacing.lg,
          ),
          children: [
            if (widget.forFilter && q.isEmpty)
              _Row(
                label: l10n.locationAllCities,
                selected: widget.current.isNone,
                onTap: () => _pop(PlaceChoice.none),
              ),
            for (final city in shown)
              _Row(
                label: city.name,
                trailing: city.hasDistricts ? AppIcons.forward : null,
                selected: widget.current.city == city,
                onTap: () {
                  if (city.hasDistricts) {
                    setState(() => _city = city);
                  } else {
                    _pop(PlaceChoice(city));
                  }
                },
              ),
            if (shown.isEmpty)
              Padding(
                padding: const EdgeInsets.all(AppSpacing.xl),
                child: Center(child: Text(l10n.locationNothingFound)),
              ),
          ],
        );
      },
    );
  }

  Widget _districts(BuildContext context) {
    final l10n = AppL10n.of(context);
    final city = _city!;
    final districts = ref.watch(districtsProvider(city.id));

    return districts.when(
      loading: () => const Center(child: CircularProgressIndicator()),
      error: (e, _) => ErrorView(
        error: e,
        onRetry: () => ref.invalidate(districtsProvider(city.id)),
      ),
      data: (all) => ListView(
        padding: EdgeInsets.fromLTRB(
          AppSpacing.lg,
          0,
          AppSpacing.lg,
          MediaQuery.paddingOf(context).bottom + AppSpacing.lg,
        ),
        children: [
          if (widget.forFilter)
            _Row(
              label: l10n.locationAllDistricts,
              selected:
                  widget.current.city == city &&
                  widget.current.district == null,
              onTap: () => _pop(PlaceChoice(city)),
            ),
          for (final district in all)
            _Row(
              label: district.name,
              selected: widget.current.district == district,
              onTap: () => _pop(PlaceChoice(city, district)),
            ),
        ],
      ),
    );
  }
}

class _Row extends StatelessWidget {
  const _Row({
    required this.label,
    required this.onTap,
    this.selected = false,
    this.trailing,
  });

  final String label;
  final bool selected;
  final IconData? trailing;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    final scheme = Theme.of(context).colorScheme;
    return InkWell(
      borderRadius: BorderRadius.circular(AppRadius.md),
      onTap: onTap,
      child: Padding(
        padding: const EdgeInsets.symmetric(horizontal: 6, vertical: 14),
        child: Row(
          children: [
            Expanded(
              child: Text(
                label,
                style: TextStyle(
                  fontSize: 16,
                  fontWeight: selected ? FontWeight.w800 : FontWeight.w500,
                  color: selected ? scheme.primary : scheme.onSurface,
                ),
              ),
            ),
            if (selected)
              Icon(Icons.check_rounded, color: scheme.primary)
            else if (trailing != null)
              Icon(trailing, color: scheme.onSurfaceVariant),
          ],
        ),
      ),
    );
  }
}
