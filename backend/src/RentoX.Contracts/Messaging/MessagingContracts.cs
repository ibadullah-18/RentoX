namespace RentoX.Contracts.Messaging;

public sealed record StartConversationRequest(
    Guid ListingId,
    string Body);

public sealed record SendMessageRequest(
    string Body);

public sealed record MessageResponse(
    Guid Id,
    Guid ConversationId,
    Guid SenderId,
    string Body,
    DateTimeOffset SentAtUtc,
    DateTimeOffset? ReadAtUtc)
{
    public IReadOnlyList<MessageImageResponse> Images { get; init; } = [];
}

public sealed record MessageImageResponse(
    Guid Id,
    string Url,
    string ContentType,
    long SizeBytes,
    int DisplayOrder);

public sealed record StartConversationResponse(
    Guid ConversationId,
    bool Created,
    MessageResponse Message);

public sealed record ConversationSummaryResponse(
    Guid Id,
    Guid ListingId,
    string ListingTitle,
    Guid OtherUserId,
    string? LastMessage,
    DateTimeOffset? LastMessageAtUtc,
    int UnreadCount);

public sealed record UnreadMessageCountResponse(int Count);
