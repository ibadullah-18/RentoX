import 'package:flutter/material.dart';
import 'package:flutter/services.dart';

import '../../../core/l10n/app_localizations.dart';
import '../../../core/theme/app_colors.dart';
import '../../../core/theme/app_spacing.dart';

typedef PriceRange = ({double? min, double? max});

/// Parses user input like `12`, `12.5` or `12,5`; empty or invalid -> null.
double? parsePrice(String text) {
  final cleaned = text.trim().replaceAll(',', '.');
  if (cleaned.isEmpty) return null;
  final value = double.tryParse(cleaned);
  return (value == null || value < 0) ? null : value;
}

/// Bottom sheet to choose a price range. Returns the new range (both null =
/// reset) or `null` when dismissed.
Future<PriceRange?> showPriceFilterSheet(
  BuildContext context, {
  double? min,
  double? max,
}) {
  return showModalBottomSheet<PriceRange>(
    context: context,
    isScrollControlled: true,
    useSafeArea: true,
    showDragHandle: true,
    shape: const RoundedRectangleBorder(
      borderRadius: BorderRadius.vertical(top: Radius.circular(AppRadius.xl)),
    ),
    builder: (_) => _PriceFilterSheet(min: min, max: max),
  );
}

class _PriceFilterSheet extends StatefulWidget {
  const _PriceFilterSheet({this.min, this.max});

  final double? min;
  final double? max;

  @override
  State<_PriceFilterSheet> createState() => _PriceFilterSheetState();
}

class _PriceFilterSheetState extends State<_PriceFilterSheet> {
  late final _min = TextEditingController(text: _initial(widget.min));
  late final _max = TextEditingController(text: _initial(widget.max));
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

  void _apply() {
    final min = parsePrice(_min.text);
    final max = parsePrice(_max.text);
    if (min != null && max != null && min > max) {
      setState(() => _invalid = true);
      return;
    }
    Navigator.of(context).pop((min: min, max: max));
  }

  @override
  Widget build(BuildContext context) {
    final l10n = AppL10n.of(context);
    final theme = Theme.of(context);
    final keyboard = MediaQuery.viewInsetsOf(context).bottom;

    Widget field(TextEditingController c, String label) => Expanded(
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

    return Padding(
      padding: EdgeInsets.fromLTRB(
        AppSpacing.lg,
        0,
        AppSpacing.lg,
        keyboard + AppSpacing.lg,
      ),
      child: Column(
        mainAxisSize: MainAxisSize.min,
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text(
            l10n.priceFilter,
            style: theme.textTheme.titleLarge?.copyWith(
              fontWeight: FontWeight.w800,
            ),
          ),
          const SizedBox(height: AppSpacing.lg),
          Row(
            children: [
              field(_min, l10n.priceMin),
              const SizedBox(width: AppSpacing.md),
              field(_max, l10n.priceMax),
            ],
          ),
          if (_invalid) ...[
            const SizedBox(height: AppSpacing.sm),
            Text(
              l10n.priceRangeInvalid,
              style: const TextStyle(color: AppColors.danger, fontSize: 13),
            ),
          ],
          const SizedBox(height: AppSpacing.xl),
          Row(
            children: [
              Expanded(
                child: OutlinedButton(
                  onPressed: () =>
                      Navigator.of(context).pop((min: null, max: null)),
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
        ],
      ),
    );
  }
}
