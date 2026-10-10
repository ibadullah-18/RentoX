import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:intl/intl.dart';

import '../../../core/l10n/app_localizations.dart';
import '../../../core/l10n/locale_controller.dart';
import '../../../core/theme/app_colors.dart';
import '../../../core/theme/app_spacing.dart';
import '../../../shared/widgets/async_states.dart';
import '../../catalog/domain/catalog_models.dart';
import '../../listing_create/data/listing_create_repository.dart';
import '../../listing_create/domain/field_models.dart';
import '../domain/field_filter.dart';
import '../domain/search_options.dart';
import 'price_filter_sheet.dart';
import 'search_controller.dart';

typedef SheetFilters = ({
  double? min,
  double? max,
  Map<String, FieldFilterValue> fields,
  SellerType seller,
  SearchSort sort,
});

/// The category's fields the search may filter on (brand, year, fuel...).
/// Free text fields are not filterable.
final filterFieldsProvider = FutureProvider.autoDispose
    .family<List<FieldDefinition>, String>((ref, categoryId) async {
      final language = ref.watch(localeProvider).languageCode;
      final all = await ref
          .watch(listingCreateRepositoryProvider)
          .categoryFields(categoryId, language);
      return all
          .where((f) => f.isFilterable && f.type != FieldType.text)
          .toList();
    });

/// Bottom sheet with the price range and, when a category is chosen, that
/// category's own filters. Returns what was chosen (everything empty =
/// reset) or `null` when dismissed.
Future<SheetFilters?> showFiltersSheet(
  BuildContext context, {
  required SearchFilters current,
}) {
  return showModalBottomSheet<SheetFilters>(
    context: context,
    isScrollControlled: true,
    useSafeArea: true,
    useRootNavigator: true,
    showDragHandle: true,
    shape: const RoundedRectangleBorder(
      borderRadius: BorderRadius.vertical(top: Radius.circular(AppRadius.xl)),
    ),
    builder: (_) => FractionallySizedBox(
      heightFactor: 0.92,
      child: FiltersSheet(current: current),
    ),
  );
}

class FiltersSheet extends ConsumerStatefulWidget {
  const FiltersSheet({super.key, required this.current});

  final SearchFilters current;

  @override
  ConsumerState<FiltersSheet> createState() => _FiltersSheetState();
}

class _FiltersSheetState extends ConsumerState<FiltersSheet> {
  late final _min = TextEditingController(
    text: _initial(widget.current.minPrice),
  );
  late final _max = TextEditingController(
    text: _initial(widget.current.maxPrice),
  );
  late final Map<String, FieldFilterValue> _fields = Map.of(
    widget.current.fields,
  );
  late SellerType _seller = widget.current.seller;
  late SearchSort _sort = widget.current.sort;
  bool _invalid = false;

  static String _initial(double? v) {
    if (v == null) return '';
    return v == v.roundToDouble() ? v.round().toString() : v.toString();
  }

  @override
  void dispose() {
    _min.dispose();
    _max.dispose();
    super.dispose();
  }

  void _set(String id, FieldFilterValue value) {
    setState(() {
      _invalid = false;
      if (value.isEmpty) {
        _fields.remove(id);
      } else {
        _fields[id] = value;
      }
    });
  }

  void _apply() {
    final min = parsePrice(_min.text);
    final max = parsePrice(_max.text);
    var bad = min != null && max != null && min > max;
    for (final v in _fields.values) {
      if (v.min != null && v.max != null && v.min! > v.max!) bad = true;
      if (v.from != null && v.to != null && v.from!.isAfter(v.to!)) bad = true;
    }
    if (bad) {
      setState(() => _invalid = true);
      return;
    }
    Navigator.of(
      context,
    ).pop((min: min, max: max, fields: _fields, seller: _seller, sort: _sort));
  }

  void _reset() => Navigator.of(context).pop((
    min: null,
    max: null,
    fields: const <String, FieldFilterValue>{},
    seller: SellerType.all,
    sort: SearchSort.date,
  ));

  @override
  Widget build(BuildContext context) {
    final l10n = AppL10n.of(context);
    final theme = Theme.of(context);
    final scheme = theme.colorScheme;
    final categoryId = widget.current.categoryId;
    final fields = categoryId == null
        ? null
        : ref.watch(filterFieldsProvider(categoryId));
    final keyboard = MediaQuery.viewInsetsOf(context).bottom;

    Widget priceField(TextEditingController c, String label) => Expanded(
      child: TextField(
        controller: c,
        keyboardType: const TextInputType.numberWithOptions(decimal: true),
        inputFormatters: [
          FilteringTextInputFormatter.allow(RegExp(r'[0-9.,]')),
        ],
        onChanged: (_) {
          if (_invalid) setState(() => _invalid = false);
        },
        decoration: InputDecoration(labelText: label, suffixText: 'AZN'),
      ),
    );

    return Column(
      children: [
        Expanded(
          child: ListView(
            padding: const EdgeInsets.fromLTRB(
              AppSpacing.lg,
              0,
              AppSpacing.lg,
              AppSpacing.lg,
            ),
            children: [
              Text(
                l10n.filters,
                style: theme.textTheme.titleLarge?.copyWith(
                  fontWeight: FontWeight.w800,
                ),
              ),
              const SizedBox(height: AppSpacing.lg),
              _Label(l10n.priceFilter),
              Row(
                children: [
                  priceField(_min, l10n.priceMin),
                  const SizedBox(width: AppSpacing.md),
                  priceField(_max, l10n.priceMax),
                ],
              ),
              const SizedBox(height: AppSpacing.xl),
              _Label(l10n.sellerType),
              Wrap(
                spacing: AppSpacing.sm,
                runSpacing: AppSpacing.sm,
                children: [
                  for (final (type, label) in [
                    (SellerType.all, l10n.sellerAll),
                    (SellerType.store, l10n.sellerStore),
                    (SellerType.individual, l10n.sellerIndividual),
                  ])
                    _Choice(
                      label: label,
                      selected: _seller == type,
                      onTap: () => setState(() => _seller = type),
                    ),
                ],
              ),
              const SizedBox(height: AppSpacing.xl),
              _Label(l10n.sortTitle),
              Wrap(
                spacing: AppSpacing.sm,
                runSpacing: AppSpacing.sm,
                children: [
                  for (final (sort, label) in [
                    (SearchSort.date, l10n.sortDate),
                    (SearchSort.priceAsc, l10n.sortPriceAsc),
                    (SearchSort.priceDesc, l10n.sortPriceDesc),
                  ])
                    _Choice(
                      label: label,
                      selected: _sort == sort,
                      onTap: () => setState(() => _sort = sort),
                    ),
                ],
              ),
              if (categoryId == null) ...[
                const SizedBox(height: AppSpacing.xl),
                Text(
                  l10n.filtersPickCategory,
                  style: TextStyle(color: scheme.onSurfaceVariant),
                ),
              ] else
                ...(fields!.when(
                  loading: () => const [
                    Padding(
                      padding: EdgeInsets.symmetric(vertical: 32),
                      child: Center(child: CircularProgressIndicator()),
                    ),
                  ],
                  error: (e, _) => [
                    ErrorView(
                      error: e,
                      onRetry: () =>
                          ref.invalidate(filterFieldsProvider(categoryId)),
                    ),
                  ],
                  data: (defs) => [
                    for (final def in defs) ...[
                      const SizedBox(height: AppSpacing.xl),
                      _FieldFilter(
                        definition: def,
                        value: _fields[def.id] ?? const FieldFilterValue(),
                        onChanged: (v) => _set(def.id, v),
                      ),
                    ],
                  ],
                )),
              if (_invalid) ...[
                const SizedBox(height: AppSpacing.md),
                Text(
                  l10n.priceRangeInvalid,
                  style: const TextStyle(color: AppColors.danger, fontSize: 13),
                ),
              ],
            ],
          ),
        ),
        Padding(
          padding: EdgeInsets.fromLTRB(
            AppSpacing.lg,
            AppSpacing.sm,
            AppSpacing.lg,
            keyboard + AppSpacing.lg,
          ),
          child: Row(
            children: [
              Expanded(
                child: OutlinedButton(
                  onPressed: _reset,
                  child: Text(l10n.resetAction),
                ),
              ),
              const SizedBox(width: AppSpacing.md),
              Expanded(
                child: FilledButton(
                  onPressed: _apply,
                  child: Text(l10n.applyAction),
                ),
              ),
            ],
          ),
        ),
      ],
    );
  }
}

class _Label extends StatelessWidget {
  const _Label(this.text);

  final String text;

  @override
  Widget build(BuildContext context) => Padding(
    padding: const EdgeInsets.only(bottom: AppSpacing.sm),
    child: Text(
      text,
      style: TextStyle(
        fontWeight: FontWeight.w700,
        color: Theme.of(context).colorScheme.onSurfaceVariant,
      ),
    ),
  );
}

/// The editor of one field's condition, by the field's type.
class _FieldFilter extends StatelessWidget {
  const _FieldFilter({
    required this.definition,
    required this.value,
    required this.onChanged,
  });

  final FieldDefinition definition;
  final FieldFilterValue value;
  final ValueChanged<FieldFilterValue> onChanged;

  @override
  Widget build(BuildContext context) {
    final l10n = AppL10n.of(context);

    switch (definition.type) {
      case FieldType.singleSelect:
      case FieldType.multiSelect:
        return Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            _Label(definition.label),
            Wrap(
              spacing: AppSpacing.sm,
              runSpacing: AppSpacing.sm,
              children: [
                for (final option in definition.options)
                  _Choice(
                    label: option.label,
                    selected: value.optionIds.contains(option.id),
                    onTap: () {
                      final next = {...value.optionIds};
                      if (!next.remove(option.id)) next.add(option.id);
                      onChanged(
                        FieldFilterValue(
                          optionIds: next,
                          min: value.min,
                          max: value.max,
                        ),
                      );
                    },
                  ),
              ],
            ),
          ],
        );

      case FieldType.wholeNumber:
      case FieldType.fractionalNumber:
        return Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            _Label(definition.label),
            Row(
              children: [
                _NumberBox(
                  label: l10n.priceMin,
                  value: value.min,
                  onChanged: (v) =>
                      onChanged(FieldFilterValue(min: v, max: value.max)),
                ),
                const SizedBox(width: AppSpacing.md),
                _NumberBox(
                  label: l10n.priceMax,
                  value: value.max,
                  onChanged: (v) =>
                      onChanged(FieldFilterValue(min: value.min, max: v)),
                ),
              ],
            ),
          ],
        );

      case FieldType.boolean:
        return Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            _Label(definition.label),
            Wrap(
              spacing: AppSpacing.sm,
              children: [
                _Choice(
                  label: l10n.yes,
                  selected: value.flag == true,
                  onTap: () => onChanged(
                    FieldFilterValue(flag: value.flag == true ? null : true),
                  ),
                ),
                _Choice(
                  label: l10n.no,
                  selected: value.flag == false,
                  onTap: () => onChanged(
                    FieldFilterValue(flag: value.flag == false ? null : false),
                  ),
                ),
              ],
            ),
          ],
        );

      case FieldType.date:
        return Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            _Label(definition.label),
            Row(
              children: [
                _DateBox(
                  label: l10n.filterDateFrom,
                  value: value.from,
                  onChanged: (d) =>
                      onChanged(FieldFilterValue(from: d, to: value.to)),
                ),
                const SizedBox(width: AppSpacing.md),
                _DateBox(
                  label: l10n.filterDateTo,
                  value: value.to,
                  onChanged: (d) =>
                      onChanged(FieldFilterValue(from: value.from, to: d)),
                ),
              ],
            ),
          ],
        );

      case FieldType.text:
        return const SizedBox.shrink();
    }
  }
}

class _NumberBox extends StatefulWidget {
  const _NumberBox({
    required this.label,
    required this.value,
    required this.onChanged,
  });

  final String label;
  final double? value;
  final ValueChanged<double?> onChanged;

  @override
  State<_NumberBox> createState() => _NumberBoxState();
}

class _NumberBoxState extends State<_NumberBox> {
  late final _c = TextEditingController(
    text: widget.value == null
        ? ''
        : (widget.value! == widget.value!.roundToDouble()
              ? widget.value!.round().toString()
              : widget.value.toString()),
  );

  @override
  void dispose() {
    _c.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) => Expanded(
    child: TextField(
      controller: _c,
      keyboardType: const TextInputType.numberWithOptions(decimal: true),
      inputFormatters: [FilteringTextInputFormatter.allow(RegExp(r'[0-9.,]'))],
      onChanged: (text) => widget.onChanged(parsePrice(text)),
      decoration: InputDecoration(labelText: widget.label),
    ),
  );
}

class _DateBox extends StatelessWidget {
  const _DateBox({
    required this.label,
    required this.value,
    required this.onChanged,
  });

  final String label;
  final DateTime? value;
  final ValueChanged<DateTime?> onChanged;

  @override
  Widget build(BuildContext context) {
    final scheme = Theme.of(context).colorScheme;
    final locale = Localizations.localeOf(context).toString();
    final text = value == null
        ? label
        : DateFormat.yMMMd(locale).format(value!);

    return Expanded(
      child: OutlinedButton(
        onPressed: () async {
          final now = DateTime.now();
          final picked = await showDatePicker(
            context: context,
            initialDate: value ?? now,
            firstDate: DateTime(now.year - 30),
            lastDate: DateTime(now.year + 30),
          );
          if (picked != null) onChanged(picked);
        },
        style: OutlinedButton.styleFrom(
          minimumSize: const Size.fromHeight(52),
          foregroundColor: value == null ? scheme.onSurfaceVariant : null,
        ),
        child: Row(
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            Flexible(child: Text(text, overflow: TextOverflow.ellipsis)),
            if (value != null) ...[
              const SizedBox(width: 6),
              GestureDetector(
                onTap: () => onChanged(null),
                child: const Icon(Icons.close_rounded, size: 18),
              ),
            ],
          ],
        ),
      ),
    );
  }
}

class _Choice extends StatelessWidget {
  const _Choice({
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
          duration: const Duration(milliseconds: 160),
          padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 10),
          decoration: BoxDecoration(
            color: selected ? scheme.primary : scheme.surface,
            borderRadius: BorderRadius.circular(AppRadius.md),
            border: Border.all(
              color: selected ? scheme.primary : scheme.outline,
            ),
          ),
          child: Text(
            label,
            style: TextStyle(
              fontWeight: FontWeight.w600,
              color: selected ? scheme.onPrimary : scheme.onSurface,
            ),
          ),
        ),
      ),
    );
  }
}
