import 'package:flutter/foundation.dart';

/// What the user chose for one category field in the filters: any of some
/// options, a number range, a yes/no or a date range. Options of one field
/// are alternatives; different fields must all match (the backend does the
/// combining).
@immutable
class FieldFilterValue {
  const FieldFilterValue({
    this.optionIds = const {},
    this.min,
    this.max,
    this.flag,
    this.from,
    this.to,
  });

  final Set<String> optionIds;
  final double? min;
  final double? max;
  final bool? flag;
  final DateTime? from;
  final DateTime? to;

  bool get isEmpty =>
      optionIds.isEmpty &&
      min == null &&
      max == null &&
      flag == null &&
      from == null &&
      to == null;

  static String _date(DateTime d) =>
      '${d.year.toString().padLeft(4, '0')}-'
      '${d.month.toString().padLeft(2, '0')}-'
      '${d.day.toString().padLeft(2, '0')}';

  /// One element of the `filters` query parameter.
  Map<String, dynamic> toJson(String fieldId) => {
    'fieldId': fieldId,
    if (optionIds.isNotEmpty) 'optionIds': optionIds.toList()..sort(),
    'min': ?min,
    'max': ?max,
    'flag': ?flag,
    if (from != null) 'from': _date(from!),
    if (to != null) 'to': _date(to!),
  };

  @override
  bool operator ==(Object other) =>
      other is FieldFilterValue &&
      setEquals(other.optionIds, optionIds) &&
      other.min == min &&
      other.max == max &&
      other.flag == flag &&
      other.from == from &&
      other.to == to;

  @override
  int get hashCode =>
      Object.hash(Object.hashAllUnordered(optionIds), min, max, flag, from, to);
}

/// The `filters` value for the search request: only non-empty conditions,
/// in a stable order. `null` when there are none.
List<Map<String, dynamic>>? encodeFieldFilters(
  Map<String, FieldFilterValue> filters,
) {
  final entries = filters.entries.where((e) => !e.value.isEmpty).toList()
    ..sort((a, b) => a.key.compareTo(b.key));
  if (entries.isEmpty) return null;
  return [for (final e in entries) e.value.toJson(e.key)];
}
