/// Mirrors the backend `NotificationType` enum.
enum NotificationKind {
  listingApproved(1),
  listingRejected(2),
  listingPaymentRequired(3),
  storeApproved(4),
  storeRejected(5),
  reportUpdated(6),
  supportReply(7),
  supportStatusChanged(8),
  other(0);

  const NotificationKind(this.id);
  final int id;

  static NotificationKind fromId(int id) =>
      values.firstWhere((k) => k.id == id, orElse: () => other);

  bool get isListing =>
      this == listingApproved ||
      this == listingRejected ||
      this == listingPaymentRequired;
}

final _guid = RegExp(
  r'/listings/([0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12})',
);

class AppNotification {
  const AppNotification({
    required this.id,
    required this.kind,
    required this.title,
    required this.body,
    required this.createdAt,
    this.relatedEntityId,
    this.actionUrl,
    this.readAt,
  });

  final String id;
  final NotificationKind kind;
  final String title;
  final String body;
  final DateTime createdAt;
  final String? relatedEntityId;
  final String? actionUrl;
  final DateTime? readAt;

  bool get isRead => readAt != null;

  /// The listing this notification is about (listing notifications only).
  String? get listingId {
    if (!kind.isListing) return null;
    final fromUrl = _guid.firstMatch(actionUrl ?? '')?.group(1);
    return fromUrl ?? relatedEntityId;
  }

  AppNotification asRead([DateTime? at]) => isRead
      ? this
      : AppNotification(
          id: id,
          kind: kind,
          title: title,
          body: body,
          createdAt: createdAt,
          relatedEntityId: relatedEntityId,
          actionUrl: actionUrl,
          readAt: at ?? DateTime.now().toUtc(),
        );

  factory AppNotification.fromJson(Map<String, dynamic> json) =>
      AppNotification(
        id: json['id'] as String,
        kind: NotificationKind.fromId((json['type'] as num).toInt()),
        title: (json['title'] as String?) ?? '',
        body: (json['body'] as String?) ?? '',
        createdAt: DateTime.parse(json['createdAtUtc'] as String),
        relatedEntityId: json['relatedEntityId'] as String?,
        actionUrl: json['actionUrl'] as String?,
        readAt: json['readAtUtc'] == null
            ? null
            : DateTime.parse(json['readAtUtc'] as String),
      );
}
