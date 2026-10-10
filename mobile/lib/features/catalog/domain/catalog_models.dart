import '../../../core/config/app_config.dart';

class Paged<T> {
  const Paged({
    required this.items,
    required this.page,
    required this.totalPages,
    required this.totalCount,
  });

  final List<T> items;
  final int page;
  final int totalPages;
  final int totalCount;

  bool get hasMore => page < totalPages;

  factory Paged.fromJson(
    Map<String, dynamic> json,
    T Function(Map<String, dynamic>) parse,
  ) => Paged(
    items: (json['items'] as List)
        .cast<Map<String, dynamic>>()
        .map(parse)
        .toList(growable: false),
    page: json['page'] as int,
    totalPages: json['totalPages'] as int,
    totalCount: json['totalCount'] as int,
  );
}

class Category {
  const Category({
    required this.id,
    required this.slug,
    required this.name,
    this.iconUrl,
    this.children = const [],
  });

  final String id;
  final String slug;
  final String name;
  final String? iconUrl;
  final List<Category> children;

  factory Category.fromJson(Map<String, dynamic> json) => Category(
    id: json['id'] as String,
    slug: (json['slug'] as String?) ?? '',
    name: (json['name'] as String?) ?? '',
    iconUrl: json['iconUrl'] as String?,
    children: ((json['children'] as List?) ?? const [])
        .cast<Map<String, dynamic>>()
        .map(Category.fromJson)
        .toList(growable: false),
  );
}

/// Mirrors the backend `RentalPeriodUnit` enum.
enum RentalPeriodUnit {
  hour(1),
  day(2),
  week(3),
  month(4),
  event(5),
  negotiable(6);

  const RentalPeriodUnit(this.id);
  final int id;

  static RentalPeriodUnit fromId(int id) =>
      values.firstWhere((u) => u.id == id, orElse: () => negotiable);
}

class ListingSummary {
  const ListingSummary({
    required this.id,
    required this.title,
    required this.price,
    required this.currency,
    required this.unit,
    required this.isVip,
    required this.isFavorite,
    required this.viewCount,
    required this.favoriteCount,
    this.categoryName,
    this.coverImageUrl,
  });

  final String id;
  final String title;
  final double price;
  final String currency;
  final RentalPeriodUnit unit;
  final bool isVip;
  final bool isFavorite;
  final int viewCount;
  final int favoriteCount;
  final String? categoryName;
  final String? coverImageUrl;

  factory ListingSummary.fromJson(Map<String, dynamic> json) => ListingSummary(
    id: json['id'] as String,
    title: (json['title'] as String?) ?? '',
    price: (json['price'] as num).toDouble(),
    currency: (json['currency'] as String?) ?? 'AZN',
    unit: RentalPeriodUnit.fromId(json['rentalPeriodUnit'] as int),
    isVip: (json['isVip'] as bool?) ?? false,
    isFavorite: (json['isFavorite'] as bool?) ?? false,
    viewCount: (json['viewCount'] as num?)?.toInt() ?? 0,
    favoriteCount: (json['favoriteCount'] as num?)?.toInt() ?? 0,
    categoryName: json['categoryName'] as String?,
    coverImageUrl: AppConfig.resolveUrl(json['coverImageUrl'] as String?),
  );
}

/// Mirrors the backend `CategoryFieldType` enum.
enum FieldType {
  text(1),
  wholeNumber(2),
  fractionalNumber(3),
  boolean(4),
  singleSelect(5),
  multiSelect(6),
  date(7);

  const FieldType(this.id);
  final int id;

  static FieldType fromId(int id) =>
      values.firstWhere((t) => t.id == id, orElse: () => text);
}

class ListingImage {
  const ListingImage({
    required this.id,
    required this.url,
    required this.displayOrder,
    required this.isCover,
  });

  final String id;
  final String url;
  final int displayOrder;
  final bool isCover;

  factory ListingImage.fromJson(Map<String, dynamic> json) => ListingImage(
    id: json['id'] as String,
    url: AppConfig.resolveUrl(json['url'] as String?) ?? '',
    displayOrder: (json['displayOrder'] as num?)?.toInt() ?? 0,
    isCover: (json['isCover'] as bool?) ?? false,
  );
}

/// One dynamic attribute of a listing (brand, year, fuel type...).
class ListingFieldValue {
  const ListingFieldValue({
    this.fieldId = '',
    required this.label,
    required this.type,
    this.textValue,
    this.numericValue,
    this.flagValue,
    this.calendarValue,
    this.customValue,
    this.selectionLabels = const [],
    this.selectionIds = const [],
  });

  /// The category field this value belongs to (needed to edit it).
  final String fieldId;
  final String label;
  final FieldType type;
  final String? textValue;
  final double? numericValue;
  final bool? flagValue;
  final DateTime? calendarValue;
  final String? customValue;
  final List<String> selectionLabels;
  final List<String> selectionIds;

  factory ListingFieldValue.fromJson(Map<String, dynamic> json) =>
      ListingFieldValue(
        fieldId: (json['fieldId'] as String?) ?? '',
        label: (json['label'] as String?) ?? (json['key'] as String?) ?? '',
        type: FieldType.fromId((json['type'] as num?)?.toInt() ?? 1),
        textValue: json['textValue'] as String?,
        numericValue: (json['numericValue'] as num?)?.toDouble(),
        flagValue: json['flagValue'] as bool?,
        calendarValue: json['calendarValue'] == null
            ? null
            : DateTime.tryParse(json['calendarValue'] as String),
        customValue: json['customValue'] as String?,
        selectionLabels: ((json['selections'] as List?) ?? const [])
            .cast<Map<String, dynamic>>()
            .map((s) => (s['label'] ?? s['value'] ?? '').toString())
            .where((s) => s.isNotEmpty)
            .toList(growable: false),
        selectionIds: ((json['selections'] as List?) ?? const [])
            .cast<Map<String, dynamic>>()
            .map((s) => (s['optionId'] ?? '').toString())
            .where((s) => s.isNotEmpty)
            .toList(growable: false),
      );
}

/// The seller's live store, shown on the listing so buyers can open it.
class ListingOwnerStore {
  const ListingOwnerStore({
    required this.id,
    required this.name,
    required this.slug,
    this.logoUrl,
  });

  final String id;
  final String name;
  final String slug;
  final String? logoUrl;

  static ListingOwnerStore? tryParse(Object? json) {
    if (json is! Map<String, dynamic>) return null;
    final slug = (json['slug'] as String?) ?? '';
    if (slug.isEmpty) return null;
    return ListingOwnerStore(
      id: (json['id'] as String?) ?? '',
      name: (json['name'] as String?) ?? '',
      slug: slug,
      logoUrl: AppConfig.resolveUrl(json['logoImageUrl'] as String?),
    );
  }
}

class ListingOwner {
  const ListingOwner({
    required this.id,
    required this.fullName,
    this.phoneNumber,
    this.store,
  });

  final String id;
  final String fullName;
  final String? phoneNumber;
  final ListingOwnerStore? store;

  factory ListingOwner.fromJson(Map<String, dynamic> json) => ListingOwner(
    id: json['id'] as String,
    fullName: (json['fullName'] as String?) ?? '',
    phoneNumber: json['phoneNumber'] as String?,
    store: ListingOwnerStore.tryParse(json['store']),
  );
}

class ListingDetails {
  const ListingDetails({
    required this.id,
    required this.title,
    required this.description,
    required this.price,
    required this.currency,
    required this.unit,
    required this.isVip,
    required this.isFavorite,
    required this.viewCount,
    required this.favoriteCount,
    required this.publishedAt,
    required this.owner,
    required this.images,
    required this.fields,
    this.categoryName,
    this.categoryId,
  });

  final String id;
  final String title;
  final String description;
  final double price;
  final String currency;
  final RentalPeriodUnit unit;
  final bool isVip;
  final bool isFavorite;
  final int viewCount;
  final int favoriteCount;
  final DateTime publishedAt;
  final ListingOwner owner;
  final List<ListingImage> images;
  final List<ListingFieldValue> fields;
  final String? categoryName;
  final String? categoryId;

  factory ListingDetails.fromJson(Map<String, dynamic> json) {
    final images =
        ((json['images'] as List?) ?? const [])
            .cast<Map<String, dynamic>>()
            .map(ListingImage.fromJson)
            .toList()
          ..sort((a, b) => a.displayOrder.compareTo(b.displayOrder));

    return ListingDetails(
      id: json['id'] as String,
      title: (json['title'] as String?) ?? '',
      description: (json['description'] as String?) ?? '',
      price: (json['price'] as num).toDouble(),
      currency: (json['currency'] as String?) ?? 'AZN',
      unit: RentalPeriodUnit.fromId(json['rentalPeriodUnit'] as int),
      isVip: (json['isVip'] as bool?) ?? false,
      isFavorite: (json['isFavorite'] as bool?) ?? false,
      viewCount: (json['viewCount'] as num?)?.toInt() ?? 0,
      favoriteCount: (json['favoriteCount'] as num?)?.toInt() ?? 0,
      publishedAt:
          DateTime.tryParse((json['publishedAtUtc'] as String?) ?? '') ??
          DateTime.now(),
      categoryName: json['categoryName'] as String?,
      categoryId: json['categoryId'] as String?,
      owner: ListingOwner.fromJson(json['owner'] as Map<String, dynamic>),
      images: images,
      fields: ((json['fields'] as List?) ?? const [])
          .cast<Map<String, dynamic>>()
          .map(ListingFieldValue.fromJson)
          .toList(growable: false),
    );
  }
}

/// One line of the search drop-down: a word from listing titles (or a
/// category) with the category chain it was found in.
class SearchSuggestion {
  const SearchSuggestion({
    required this.isCategory,
    required this.text,
    required this.categoryId,
    required this.path,
  });

  final bool isCategory;
  final String text;
  final String? categoryId;

  /// Category names from the top one down; for a category suggestion this
  /// is the chain *above* it.
  final List<String> path;

  factory SearchSuggestion.fromJson(Map<String, dynamic> j) => SearchSuggestion(
    isCategory: j['kind'] == 'category',
    text: (j['text'] as String?) ?? '',
    categoryId: j['categoryId'] as String?,
    path: ((j['categoryPath'] as List?) ?? const []).cast<String>(),
  );
}
