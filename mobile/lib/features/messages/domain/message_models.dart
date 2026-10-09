import 'dart:typed_data';

import '../../../core/config/app_config.dart';

class ChatImage {
  const ChatImage({required this.id, required this.url});

  final String id;

  /// Absolute URL. Chat images are private: requests need the bearer token.
  final String url;

  factory ChatImage.fromJson(Map<String, dynamic> json) => ChatImage(
    id: json['id'] as String,
    url: AppConfig.resolveUrl(json['url'] as String?) ?? '',
  );
}

enum SendState { sent, sending, failed }

class ChatMessage {
  const ChatMessage({
    required this.id,
    required this.conversationId,
    required this.senderId,
    required this.body,
    required this.sentAt,
    this.readAt,
    this.images = const [],
    this.sendState = SendState.sent,
    this.localImages = const [],
  });

  final String id;
  final String conversationId;
  final String senderId;
  final String body;
  final DateTime sentAt;
  final DateTime? readAt;
  final List<ChatImage> images;
  final SendState sendState;

  /// Bytes of images that are still uploading (shown while [sendState] is
  /// `sending`/`failed`).
  final List<Uint8List> localImages;

  bool get isRead => readAt != null;

  ChatMessage copyWith({DateTime? readAt, SendState? sendState}) => ChatMessage(
    id: id,
    conversationId: conversationId,
    senderId: senderId,
    body: body,
    sentAt: sentAt,
    readAt: readAt ?? this.readAt,
    images: images,
    sendState: sendState ?? this.sendState,
    localImages: localImages,
  );

  factory ChatMessage.fromJson(Map<String, dynamic> json) => ChatMessage(
    id: json['id'] as String,
    conversationId: json['conversationId'] as String,
    senderId: json['senderId'] as String,
    body: (json['body'] as String?) ?? '',
    sentAt: DateTime.parse(json['sentAtUtc'] as String),
    readAt: json['readAtUtc'] == null
        ? null
        : DateTime.parse(json['readAtUtc'] as String),
    images: ((json['images'] as List?) ?? const [])
        .cast<Map<String, dynamic>>()
        .map(ChatImage.fromJson)
        .toList(growable: false),
  );
}

class ConversationSummary {
  const ConversationSummary({
    required this.id,
    required this.listingId,
    required this.listingTitle,
    required this.otherUserId,
    required this.unreadCount,
    this.lastMessage,
    this.lastMessageAt,
  });

  final String id;
  final String listingId;
  final String listingTitle;
  final String otherUserId;
  final int unreadCount;
  final String? lastMessage;
  final DateTime? lastMessageAt;

  ConversationSummary copyWith({
    String? lastMessage,
    DateTime? lastMessageAt,
    int? unreadCount,
    bool clearLastMessage = false,
  }) => ConversationSummary(
    id: id,
    listingId: listingId,
    listingTitle: listingTitle,
    otherUserId: otherUserId,
    unreadCount: unreadCount ?? this.unreadCount,
    lastMessage: clearLastMessage ? null : (lastMessage ?? this.lastMessage),
    lastMessageAt: lastMessageAt ?? this.lastMessageAt,
  );

  factory ConversationSummary.fromJson(Map<String, dynamic> json) =>
      ConversationSummary(
        id: json['id'] as String,
        listingId: json['listingId'] as String,
        listingTitle: (json['listingTitle'] as String?) ?? '',
        otherUserId: json['otherUserId'] as String,
        unreadCount: (json['unreadCount'] as num?)?.toInt() ?? 0,
        lastMessage: json['lastMessage'] as String?,
        lastMessageAt: json['lastMessageAtUtc'] == null
            ? null
            : DateTime.parse(json['lastMessageAtUtc'] as String),
      );
}

class BlockStatus {
  const BlockStatus({required this.isBlockedByMe, required this.canSend});

  final bool isBlockedByMe;
  final bool canSend;

  static const open = BlockStatus(isBlockedByMe: false, canSend: true);

  factory BlockStatus.fromJson(Map<String, dynamic> json) => BlockStatus(
    isBlockedByMe: (json['isBlockedByMe'] as bool?) ?? false,
    canSend: (json['canSendMessages'] as bool?) ?? true,
  );
}

/// Mirrors the backend `ConversationReportReason` enum.
enum ReportReason {
  spam(1),
  fraud(2),
  harassment(3),
  prohibitedContent(4),
  other(5);

  const ReportReason(this.id);
  final int id;
}

/// Longest message the backend accepts.
const maxMessageLength = 2000;

/// Images per message and size limit, as enforced by the backend.
const maxChatImages = 5;
