/// Mirrors the backend `SupportTicketCategory` enum.
enum SupportCategory {
  general(1),
  account(2),
  listing(3),
  payment(4),
  store(5),
  technical(6),
  other(7);

  const SupportCategory(this.id);
  final int id;

  static SupportCategory fromId(int id) =>
      values.firstWhere((c) => c.id == id, orElse: () => other);
}

/// Mirrors the backend `SupportTicketStatus` enum.
enum SupportStatus {
  open(1),
  inProgress(2),
  resolved(3),
  closed(4);

  const SupportStatus(this.id);
  final int id;

  static SupportStatus fromId(int id) =>
      values.firstWhere((s) => s.id == id, orElse: () => open);

  /// A closed ticket accepts no more messages.
  bool get canReply => this != closed;
}

/// Limits enforced by the backend (`SupportTicket`, `SupportTicketMessage`).
abstract final class SupportRules {
  static const subjectMax = 160;
  static const bodyMax = 4000;
}

enum SupportIssue { subjectRequired, subjectTooLong, bodyRequired, bodyTooLong }

/// Client-side check of a new ticket (mirrors the backend).
Set<SupportIssue> validateTicket({
  required String subject,
  required String body,
  String reference = '',
}) {
  final s = subject.trim();
  final b = body.trim();
  return {
    if (s.isEmpty) SupportIssue.subjectRequired,
    if (s.length > SupportRules.subjectMax) SupportIssue.subjectTooLong,
    if (b.isEmpty) SupportIssue.bodyRequired,
    if (composeBody(b, reference).length > SupportRules.bodyMax)
      SupportIssue.bodyTooLong,
  };
}

/// The text actually sent: what the user wrote plus, when the ticket was
/// opened from a listing or store, a line pointing at it so the team can find
/// it (the API has no separate field for that).
String composeBody(String body, String reference) {
  final b = body.trim();
  final r = reference.trim();
  return r.isEmpty ? b : '$b\n\n— $r';
}

class SupportMessage {
  const SupportMessage({
    required this.id,
    required this.body,
    required this.isAdmin,
    required this.sentAt,
  });

  final String id;
  final String body;
  final bool isAdmin;
  final DateTime sentAt;

  factory SupportMessage.fromJson(Map<String, dynamic> j) => SupportMessage(
    id: j['id'] as String,
    body: (j['body'] as String?) ?? '',
    isAdmin: (j['isAdmin'] as bool?) ?? false,
    sentAt: DateTime.parse(j['sentAtUtc'] as String),
  );
}

class SupportTicket {
  const SupportTicket({
    required this.id,
    required this.category,
    required this.subject,
    required this.status,
    required this.createdAt,
    required this.updatedAt,
    this.messages = const [],
  });

  final String id;
  final SupportCategory category;
  final String subject;
  final SupportStatus status;
  final DateTime createdAt;
  final DateTime updatedAt;

  /// Oldest first. Empty in list rows.
  final List<SupportMessage> messages;

  factory SupportTicket.fromJson(Map<String, dynamic> j) {
    final created = DateTime.parse(j['createdAtUtc'] as String);
    final messages =
        ((j['messages'] as List?) ?? const [])
            .cast<Map<String, dynamic>>()
            .map(SupportMessage.fromJson)
            .toList()
          ..sort((a, b) => a.sentAt.compareTo(b.sentAt));
    return SupportTicket(
      id: j['id'] as String,
      category: SupportCategory.fromId((j['category'] as num).toInt()),
      subject: (j['subject'] as String?) ?? '',
      status: SupportStatus.fromId((j['status'] as num).toInt()),
      createdAt: created,
      updatedAt: j['updatedAtUtc'] == null
          ? created
          : DateTime.parse(j['updatedAtUtc'] as String),
      messages: messages,
    );
  }
}
