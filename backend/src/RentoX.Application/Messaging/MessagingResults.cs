namespace RentoX.Application.Messaging;

public sealed record MessageResult(
    Guid Id,
    Guid ConversationId,
    Guid SenderId,
    string Body,
    DateTimeOffset SentAtUtc,
    DateTimeOffset? ReadAtUtc);

public sealed record StartConversationResult(
    Guid ConversationId,
    bool Created,
    MessageResult Message);

public sealed record ConversationSummaryResult(
    Guid Id,
    Guid ListingId,
    string ListingTitle,
    Guid OtherUserId,
    string? LastMessage,
    DateTimeOffset? LastMessageAtUtc,
    int UnreadCount);