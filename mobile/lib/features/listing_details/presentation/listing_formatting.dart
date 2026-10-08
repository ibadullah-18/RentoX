import 'package:intl/intl.dart';

import '../../../core/l10n/app_localizations.dart';
import '../../catalog/domain/catalog_models.dart';

/// Whole numbers: years (2023) and small counts must not get a thousands
/// separator, larger values (125 000 km) do.
String formatWholeNumber(String locale, num value) {
  final whole = value.round();
  return whole.abs() < 10000
      ? '$whole'
      : NumberFormat.decimalPattern(locale).format(whole);
}

/// Human-readable value of a dynamic listing attribute, or `null` when the
/// seller left it empty (so the row can be skipped).
String? fieldDisplayValue(
  AppL10n l10n,
  String locale,
  ListingFieldValue field,
) {
  String? clean(String? s) => (s == null || s.trim().isEmpty) ? null : s.trim();

  switch (field.type) {
    case FieldType.text:
      return clean(field.textValue);
    case FieldType.wholeNumber:
      final n = field.numericValue;
      return n == null ? null : formatWholeNumber(locale, n);
    case FieldType.fractionalNumber:
      final n = field.numericValue;
      return n == null ? null : NumberFormat.decimalPattern(locale).format(n);
    case FieldType.boolean:
      final flag = field.flagValue;
      return flag == null ? null : (flag ? l10n.yes : l10n.no);
    case FieldType.singleSelect:
    case FieldType.multiSelect:
      final parts = [...field.selectionLabels, ?clean(field.customValue)];
      return parts.isEmpty ? null : parts.join(', ');
    case FieldType.date:
      final d = field.calendarValue;
      return d == null ? null : DateFormat.yMMMd(locale).format(d);
  }
}

/// "Today", "Yesterday" or "N days ago", by calendar day.
String publishedLabel(AppL10n l10n, DateTime publishedUtc, {DateTime? now}) {
  final published = publishedUtc.toLocal();
  final today = now ?? DateTime.now();
  final days = DateTime(
    today.year,
    today.month,
    today.day,
  ).difference(DateTime(published.year, published.month, published.day)).inDays;

  if (days <= 0) return l10n.publishedToday;
  if (days == 1) return l10n.publishedYesterday;
  return l10n.publishedDaysAgo(days);
}
