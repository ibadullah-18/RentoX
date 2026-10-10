import '../../catalog/domain/catalog_models.dart';

class FieldOption {
  const FieldOption({
    required this.id,
    required this.value,
    required this.label,
  });

  final String id;
  final String value;
  final String label;

  factory FieldOption.fromJson(Map<String, dynamic> json) => FieldOption(
    id: json['id'] as String,
    value: (json['value'] as String?) ?? '',
    label: (json['label'] as String?) ?? (json['value'] as String?) ?? '',
  );
}

/// One attribute a category asks for (brand, year, fuel type...), as defined
/// by the backend. Drives the dynamic part of the create-listing form.
class FieldDefinition {
  const FieldDefinition({
    required this.id,
    required this.label,
    required this.type,
    required this.isRequired,
    required this.allowCustomValue,
    required this.displayOrder,
    this.isFilterable = false,
    this.options = const [],
  });

  final String id;
  final String label;
  final FieldType type;
  final bool isRequired;
  final bool allowCustomValue;
  final int displayOrder;

  /// Whether the search filters may use this field.
  final bool isFilterable;
  final List<FieldOption> options;

  factory FieldDefinition.fromJson(Map<String, dynamic> json) {
    final options =
        ((json['options'] as List?) ?? const [])
            .cast<Map<String, dynamic>>()
            .map(FieldOption.fromJson)
            .toList()
          ..sort((a, b) => a.label.compareTo(b.label));

    return FieldDefinition(
      id: json['id'] as String,
      label: (json['label'] as String?) ?? (json['key'] as String?) ?? '',
      type: FieldType.fromId((json['type'] as num).toInt()),
      isRequired: (json['isRequired'] as bool?) ?? false,
      allowCustomValue: (json['allowCustomValue'] as bool?) ?? false,
      displayOrder: (json['displayOrder'] as num?)?.toInt() ?? 0,
      isFilterable: (json['isFilterable'] as bool?) ?? false,
      options: options,
    );
  }
}

enum FieldError { required, invalidNumber, notWhole }

/// What the user entered for one [FieldDefinition]. Text-like and numeric
/// fields keep the raw text so a half-typed number can still be shown and
/// validated; conversion to the API shape happens in [toRequest].
class FieldAnswer {
  const FieldAnswer({
    this.text = '',
    this.flag,
    this.date,
    this.optionIds = const {},
    this.custom = '',
  });

  final String text;
  final bool? flag;
  final DateTime? date;
  final Set<String> optionIds;
  final String custom;

  FieldAnswer copyWith({
    String? text,
    bool? flag,
    bool clearFlag = false,
    DateTime? date,
    bool clearDate = false,
    Set<String>? optionIds,
    String? custom,
  }) => FieldAnswer(
    text: text ?? this.text,
    flag: clearFlag ? null : (flag ?? this.flag),
    date: clearDate ? null : (date ?? this.date),
    optionIds: optionIds ?? this.optionIds,
    custom: custom ?? this.custom,
  );

  /// Parses `12`, `12.5` or `12,5`; `null` when not a number.
  static double? parseNumber(String raw) {
    final cleaned = raw.trim().replaceAll(',', '.');
    if (cleaned.isEmpty) return null;
    return double.tryParse(cleaned);
  }

  bool isEmptyFor(FieldDefinition def) {
    switch (def.type) {
      case FieldType.text:
      case FieldType.wholeNumber:
      case FieldType.fractionalNumber:
        return text.trim().isEmpty;
      case FieldType.boolean:
        return flag == null;
      case FieldType.date:
        return date == null;
      case FieldType.singleSelect:
      case FieldType.multiSelect:
        return optionIds.isEmpty && custom.trim().isEmpty;
    }
  }

  /// The problem with this answer, or `null` when it is acceptable.
  FieldError? validate(FieldDefinition def) {
    if (isEmptyFor(def)) {
      return def.isRequired ? FieldError.required : null;
    }
    switch (def.type) {
      case FieldType.wholeNumber:
        final n = parseNumber(text);
        if (n == null) return FieldError.invalidNumber;
        if (n != n.truncateToDouble()) return FieldError.notWhole;
      case FieldType.fractionalNumber:
        if (parseNumber(text) == null) return FieldError.invalidNumber;
      case FieldType.text:
      case FieldType.boolean:
      case FieldType.date:
      case FieldType.singleSelect:
      case FieldType.multiSelect:
        break;
    }
    return null;
  }

  /// The `CreateListingFieldRequest` body, or `null` when nothing was entered
  /// (optional fields are simply left out).
  Map<String, dynamic>? toRequest(FieldDefinition def) {
    if (isEmptyFor(def)) return null;

    final body = <String, dynamic>{'fieldId': def.id};
    switch (def.type) {
      case FieldType.text:
        body['textValue'] = text.trim();
      case FieldType.wholeNumber:
      case FieldType.fractionalNumber:
        body['numericValue'] = parseNumber(text);
      case FieldType.boolean:
        body['flagValue'] = flag;
      case FieldType.date:
        final d = date!;
        body['calendarValue'] =
            '${d.year.toString().padLeft(4, '0')}-'
            '${d.month.toString().padLeft(2, '0')}-'
            '${d.day.toString().padLeft(2, '0')}';
      case FieldType.singleSelect:
        // The API accepts either one option or a custom value, never both.
        if (optionIds.isNotEmpty) {
          body['optionIds'] = [optionIds.first];
        } else {
          body['customValue'] = custom.trim();
        }
      case FieldType.multiSelect:
        if (optionIds.isNotEmpty) body['optionIds'] = optionIds.toList();
        if (custom.trim().isNotEmpty) body['customValue'] = custom.trim();
    }
    return body;
  }
}
