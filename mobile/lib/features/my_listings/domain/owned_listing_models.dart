import '../../../core/config/app_config.dart';
import '../../catalog/domain/catalog_models.dart';

/// Mirrors the backend `ListingStatus` enum.
enum ListingStatus {
  draft(1),
  pendingReview(2),
  active(3),
  rejected(4),
  expired(5),
  deactivated(6),
  deleted(7),
  paymentRequired(8);

  const ListingStatus(this.id);
  final int id;

  static ListingStatus fromId(int id) =>
      values.firstWhere((s) => s.id == id, orElse: () => draft);

  /// Can be sent for moderation (needs at least one photo on the server).
  bool get canSubmit => this == draft || this == rejected;

  bool get canPay => this == paymentRequired;

  /// Promotions (VIP) are only sold for live listings.
  bool get canPromote => this == active;

  bool get canDeactivate => this == active;

  bool get canReactivate => this == deactivated;
}

class OwnedListingSummary {
  const OwnedListingSummary({
    required this.id,
    required this.title,
    required this.price,
    required this.currency,
    required this.unit,
    required this.status,
    required this.imageCount,
    required this.createdAt,
    this.coverImageUrl,
    this.publishedAt,
    this.expiresAt,
  });

  final String id;
  final String title;
  final double price;
  final String currency;
  final RentalPeriodUnit unit;
  final ListingStatus status;
  final int imageCount;
  final DateTime createdAt;
  final String? coverImageUrl;
  final DateTime? publishedAt;
  final DateTime? expiresAt;

  factory OwnedListingSummary.fromJson(Map<String, dynamic> json) =>
      OwnedListingSummary(
        id: json['id'] as String,
        title: (json['title'] as String?) ?? '',
        price: (json['price'] as num).toDouble(),
        currency: (json['currency'] as String?) ?? 'AZN',
        unit: RentalPeriodUnit.fromId(json['rentalPeriodUnit'] as int),
        status: ListingStatus.fromId(json['status'] as int),
        imageCount: (json['imageCount'] as num?)?.toInt() ?? 0,
        createdAt:
            DateTime.tryParse((json['createdAtUtc'] as String?) ?? '') ??
            DateTime.now(),
        coverImageUrl: AppConfig.resolveUrl(json['coverImageUrl'] as String?),
        publishedAt: _date(json['publishedAtUtc']),
        expiresAt: _date(json['expiresAtUtc']),
      );
}

/// The owner's own view of a listing (any status), as read back from the
/// server after it has been saved.
class OwnedListingDetails {
  const OwnedListingDetails({
    required this.id,
    required this.categoryId,
    required this.title,
    required this.description,
    required this.price,
    required this.currency,
    required this.unit,
    required this.status,
    required this.images,
    required this.fields,
    this.rejectionReason,
    this.publishedAt,
    this.expiresAt,
  });

  final String id;
  final String categoryId;
  final String title;
  final String description;
  final double price;
  final String currency;
  final RentalPeriodUnit unit;
  final ListingStatus status;
  final List<ListingImage> images;
  final List<ListingFieldValue> fields;
  final String? rejectionReason;
  final DateTime? publishedAt;
  final DateTime? expiresAt;

  factory OwnedListingDetails.fromJson(Map<String, dynamic> json) {
    final images =
        ((json['images'] as List?) ?? const [])
            .cast<Map<String, dynamic>>()
            .map(ListingImage.fromJson)
            .toList()
          ..sort((a, b) => a.displayOrder.compareTo(b.displayOrder));

    return OwnedListingDetails(
      id: json['id'] as String,
      categoryId: json['categoryId'] as String,
      title: (json['title'] as String?) ?? '',
      description: (json['description'] as String?) ?? '',
      price: (json['price'] as num).toDouble(),
      currency: (json['currency'] as String?) ?? 'AZN',
      unit: RentalPeriodUnit.fromId(json['rentalPeriodUnit'] as int),
      status: ListingStatus.fromId(json['status'] as int),
      rejectionReason: json['rejectionReason'] as String?,
      publishedAt: _date(json['publishedAtUtc']),
      expiresAt: _date(json['expiresAtUtc']),
      images: images,
      fields: ((json['fields'] as List?) ?? const [])
          .cast<Map<String, dynamic>>()
          .map(ListingFieldValue.fromJson)
          .toList(growable: false),
    );
  }
}

DateTime? _date(Object? raw) => raw is String ? DateTime.tryParse(raw) : null;
