namespace RentoX.Application.Messaging;

public sealed record MessageResult(
    Guid Id,
    Guid ConversationId,
    Guid SenderId,
    string Body,
    DateTimeOffset SentAtUtc,
    DateTimeOffset? ReadAtUtc)
{
    public IReadOnlyList<MessageImageResult> Images { get; init; } = [];
}

public sealed record MessageImageResult(
    Guid Id,
    string Url,
    string ContentType,
    long SizeBytes,
    int DisplayOrder);

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
    int UnreadCount,
    /// <summary>The other person has a profile photo (so the picture is asked for only when it exists).</summary>
    bool OtherHasPhoto = false);
