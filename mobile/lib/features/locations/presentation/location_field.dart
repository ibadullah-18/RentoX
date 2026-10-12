import 'package:flutter/material.dart';

import '../../../core/l10n/app_localizations.dart';
import '../../../core/theme/app_colors.dart';
import '../../../core/theme/app_icons.dart';
import '../../../core/theme/app_spacing.dart';
import '../domain/location_models.dart';
import 'location_picker.dart';

/// A button-like field showing the chosen place; tapping it opens the
/// location picker. Used by the listing forms and the search filters.
class LocationField extends StatelessWidget {
  const LocationField({
    super.key,
    required this.value,
    required this.onChanged,
    this.label,
    this.required = false,
    this.forFilter = false,
    this.enabled = true,
    this.errorText,
  });

  final PlaceChoice value;
  final ValueChanged<PlaceChoice> onChanged;

  /// Small caption above the field.
  final String? label;
  final bool required;
  final bool forFilter;
  final bool enabled;
  final String? errorText;

  @override
  Widget build(BuildContext context) {
    final l10n = AppL10n.of(context);
    final scheme = Theme.of(context).colorScheme;
    final has = !value.isNone;

    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        if (label != null) ...[
          Text(
            required ? '$label *' : label!,
            style: TextStyle(
              fontWeight: FontWeight.w600,
              color: scheme.onSurfaceVariant,
            ),
          ),
          const SizedBox(height: AppSpacing.sm),
        ],
        OutlinedButton(
          onPressed: enabled
              ? () async {
                  final picked = await showLocationPicker(
                    context,
                    current: value,
                    forFilter: forFilter,
                  );
                  if (picked != null) onChanged(picked);
                }
              : null,
          style: OutlinedButton.styleFrom(
            minimumSize: const Size.fromHeight(54),
            alignment: AlignmentDirectional.centerStart,
            side: BorderSide(
              color: errorText != null ? AppColors.danger : scheme.outline,
            ),
          ),
          child: Row(
            children: [
              Icon(AppIcons.place, size: 20, color: scheme.onSurfaceVariant),
              const SizedBox(width: AppSpacing.sm),
              Expanded(
                child: Text(
                  has
                      ? value.label
                      : (forFilter
                            ? l10n.locationAllCities
                            : l10n.locationChoose),
                  maxLines: 1,
                  overflow: TextOverflow.ellipsis,
                  style: TextStyle(
                    fontWeight: FontWeight.w600,
                    color: has ? scheme.onSurface : scheme.onSurfaceVariant,
                  ),
                ),
              ),
              const Icon(AppIcons.forward),
            ],
          ),
        ),
        if (errorText != null) ...[
          const SizedBox(height: 6),
          Text(
            errorText!,
            style: const TextStyle(color: AppColors.danger, fontSize: 13),
          ),
        ],
      ],
    );
  }
}
