/// Where a tapped push notification should take the user. Built from the
/// `data` map the backend sends (`notificationId`, `actionUrl`,
/// `relatedEntityId`).
class PushTarget {
  const PushTarget({this.notificationId, this.listingId, this.ticketId});

  final String? notificationId;

  /// Set for notifications about one of the user's listings.
  final String? listingId;

  /// Set for replies and status changes on a support request.
  final String? ticketId;

  static final _listing = RegExp(
    r'/listings/([0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12})',
  );

  static final _ticket = RegExp(
    r'/support/tickets/([0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12})',
  );

  factory PushTarget.fromData(Map<String, String> data) {
    String? clean(String? v) => (v == null || v.isEmpty) ? null : v;
    return PushTarget(
      notificationId: clean(data['notificationId']),
      listingId: _listing.firstMatch(data['actionUrl'] ?? '')?.group(1),
      ticketId: _ticket.firstMatch(data['actionUrl'] ?? '')?.group(1),
    );
  }
}
