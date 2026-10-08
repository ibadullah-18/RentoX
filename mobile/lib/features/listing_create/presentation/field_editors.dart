import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:intl/intl.dart';

import '../../../core/l10n/app_localizations.dart';
import '../../../core/theme/app_colors.dart';
import '../../../core/theme/app_icons.dart';
import '../../../core/theme/app_spacing.dart';
import '../../catalog/domain/catalog_models.dart';
import '../../listing_details/presentation/listing_formatting.dart';
import '../domain/field_models.dart';

String fieldErrorText(AppL10n l10n, FieldError e) => switch (e) {
  FieldError.required => l10n.fieldRequired,
  FieldError.invalidNumber => l10n.fieldInvalidNumber,
  FieldError.notWhole => l10n.fieldNotWhole,
};

/// Human-readable form of an answer (for the review step); `null` if empty.
String? answerDisplay(
  AppL10n l10n,
  String locale,
  FieldDefinition def,
  FieldAnswer a,
) {
  if (a.isEmptyFor(def)) return null;
  switch (def.type) {
    case FieldType.text:
      return a.text.trim();
    case FieldType.wholeNumber:
      final n = FieldAnswer.parseNumber(a.text);
      return n == null ? a.text.trim() : formatWholeNumber(locale, n);
    case FieldType.fractionalNumber:
      final n = FieldAnswer.parseNumber(a.text);
      return n == null
          ? a.text.trim()
          : NumberFormat.decimalPattern(locale).format(n);
    case FieldType.boolean:
      return a.flag! ? l10n.yes : l10n.no;
    case FieldType.date:
      return DateFormat.yMMMd(locale).format(a.date!);
    case FieldType.singleSelect:
    case FieldType.multiSelect:
      final labels = [
        for (final o in def.options)
          if (a.optionIds.contains(o.id)) o.label,
        if (a.custom.trim().isNotEmpty) a.custom.trim(),
      ];
      return labels.join(', ');
  }
}

/// Renders the right input for a [FieldDefinition] and reports changes.
class FieldEditor extends StatelessWidget {
  const FieldEditor({
    super.key,
    required this.definition,
    required this.answer,
    required this.onChanged,
    this.error,
    this.enabled = true,
  });

  final FieldDefinition definition;
  final FieldAnswer answer;
  final ValueChanged<FieldAnswer> onChanged;
  final FieldError? error;
  final bool enabled;

  @override
  Widget build(BuildContext context) {
    final l10n = AppL10n.of(context);
    final label = definition.isRequired
        ? '${definition.label} *'
        : definition.label;
    final errorText = error == null ? null : fieldErrorText(l10n, error!);

    switch (definition.type) {
      case FieldType.text:
      case FieldType.wholeNumber:
      case FieldType.fractionalNumber:
        final numeric = definition.type != FieldType.text;
        return _TextInput(
          label: label,
          value: answer.text,
          errorText: errorText,
          enabled: enabled,
          keyboardType: numeric
              ? TextInputType.numberWithOptions(
                  decimal: definition.type == FieldType.fractionalNumber,
                )
              : TextInputType.text,
          formatters: numeric
              ? [FilteringTextInputFormatter.allow(RegExp(r'[0-9.,]'))]
              : const [],
          onChanged: (v) => onChanged(answer.copyWith(text: v)),
        );

      case FieldType.boolean:
        return _Labeled(
          label: label,
          errorText: errorText,
          child: Wrap(
            spacing: AppSpacing.sm,
            children: [
              _Choice(
                label: l10n.yes,
                selected: answer.flag == true,
                enabled: enabled,
                onTap: () => onChanged(
                  answer.flag == true
                      ? answer.copyWith(clearFlag: true)
                      : answer.copyWith(flag: true),
                ),
              ),
              _Choice(
                label: l10n.no,
                selected: answer.flag == false,
                enabled: enabled,
                onTap: () => onChanged(
                  answer.flag == false
                      ? answer.copyWith(clearFlag: true)
                      : answer.copyWith(flag: false),
                ),
              ),
            ],
          ),
        );

      case FieldType.date:
        final locale = Localizations.localeOf(context).toString();
        return _Labeled(
          label: label,
          errorText: errorText,
          child: OutlinedButton.icon(
            onPressed: enabled
                ? () async {
                    final now = DateTime.now();
                    final picked = await showDatePicker(
                      context: context,
                      initialDate: answer.date ?? now,
                      firstDate: DateTime(1950),
                      lastDate: DateTime(now.year + 30),
                    );
                    if (picked != null) {
                      onChanged(answer.copyWith(date: picked));
                    }
                  }
                : null,
            style: OutlinedButton.styleFrom(
              minimumSize: const Size.fromHeight(52),
              alignment: AlignmentDirectional.centerStart,
            ),
            icon: const Icon(AppIcons.time),
            label: Text(
              answer.date == null
                  ? l10n.fieldPickDate
                  : DateFormat.yMMMd(locale).format(answer.date!),
            ),
          ),
        );

      case FieldType.singleSelect:
      case FieldType.multiSelect:
        final multi = definition.type == FieldType.multiSelect;
        final customActive =
            answer.custom.isNotEmpty && answer.optionIds.isEmpty;
        return _Labeled(
          label: label,
          errorText: errorText,
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Wrap(
                spacing: AppSpacing.sm,
                runSpacing: AppSpacing.sm,
                children: [
                  for (final o in definition.options)
                    _Choice(
                      label: o.label,
                      selected: answer.optionIds.contains(o.id),
                      enabled: enabled,
                      onTap: () {
                        final ids = {...answer.optionIds};
                        if (multi) {
                          ids.contains(o.id) ? ids.remove(o.id) : ids.add(o.id);
                          onChanged(answer.copyWith(optionIds: ids));
                        } else {
                          // One option at a time; it replaces any custom text.
                          onChanged(
                            FieldAnswer(
                              optionIds: ids.contains(o.id) ? {} : {o.id},
                            ),
                          );
                        }
                      },
                    ),
                  if (definition.allowCustomValue)
                    _Choice(
                      label: l10n.fieldOther,
                      selected:
                          customActive || (multi && answer.custom.isNotEmpty),
                      enabled: enabled,
                      onTap: () => onChanged(
                        answer.custom.isNotEmpty
                            ? answer.copyWith(custom: '')
                            : (multi
                                  ? answer.copyWith(custom: ' ')
                                  : FieldAnswer(custom: ' ')),
                      ),
                    ),
                ],
              ),
              if (definition.allowCustomValue && answer.custom.isNotEmpty) ...[
                const SizedBox(height: AppSpacing.sm),
                _TextInput(
                  label: l10n.fieldCustomHint,
                  value: answer.custom.trim(),
                  enabled: enabled,
                  autofocus: true,
                  // A custom value replaces a single selection.
                  onChanged: (v) => onChanged(
                    multi
                        ? answer.copyWith(custom: v.isEmpty ? ' ' : v)
                        : FieldAnswer(custom: v.isEmpty ? ' ' : v),
                  ),
                ),
              ],
            ],
          ),
        );
    }
  }
}

class _Labeled extends StatelessWidget {
  const _Labeled({required this.label, required this.child, this.errorText});

  final String label;
  final Widget child;
  final String? errorText;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Text(
          label,
          style: TextStyle(
            fontWeight: FontWeight.w600,
            color: theme.colorScheme.onSurfaceVariant,
          ),
        ),
        const SizedBox(height: AppSpacing.sm),
        child,
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

class _Choice extends StatelessWidget {
  const _Choice({
    required this.label,
    required this.selected,
    required this.onTap,
    required this.enabled,
  });

  final String label;
  final bool selected;
  final bool enabled;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    final scheme = Theme.of(context).colorScheme;
    return Semantics(
      button: true,
      selected: selected,
      label: label,
      child: GestureDetector(
        onTap: enabled ? onTap : null,
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

/// A text field that keeps its own controller (so the caret never jumps) but
/// follows external changes such as a reset.
class _TextInput extends StatefulWidget {
  const _TextInput({
    required this.label,
    required this.value,
    required this.onChanged,
    required this.enabled,
    this.errorText,
    this.keyboardType = TextInputType.text,
    this.formatters = const [],
    this.autofocus = false,
  });

  final String label;
  final String value;
  final ValueChanged<String> onChanged;
  final bool enabled;
  final String? errorText;
  final TextInputType keyboardType;
  final List<TextInputFormatter> formatters;
  final bool autofocus;

  @override
  State<_TextInput> createState() => _TextInputState();
}

class _TextInputState extends State<_TextInput> {
  late final TextEditingController _c = TextEditingController(
    text: widget.value,
  );

  @override
  void didUpdateWidget(_TextInput old) {
    super.didUpdateWidget(old);
    if (widget.value != _c.text && widget.value != old.value) {
      _c.text = widget.value;
    }
  }

  @override
  void dispose() {
    _c.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) => TextField(
    controller: _c,
    enabled: widget.enabled,
    autofocus: widget.autofocus,
    keyboardType: widget.keyboardType,
    inputFormatters: widget.formatters,
    onChanged: widget.onChanged,
    decoration: InputDecoration(
      labelText: widget.label,
      errorText: widget.errorText,
    ),
  );
}
