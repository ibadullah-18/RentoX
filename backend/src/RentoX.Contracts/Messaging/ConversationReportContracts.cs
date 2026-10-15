namespace RentoX.Contracts.Messaging;

public sealed record CreateConversationReportRequest(
    int Reason,
    string? Details,
    Guid? EvidenceMessageId);

public sealed record ReviewConversationReportRequest(
    int Status,
    string? ResolutionNote);

public sealed record ConversationReportResponse(
    Guid Id,
    Guid ConversationId,
    Guid ReporterId,
    int Reason,
    string? Details,
    Guid? EvidenceMessageId,
    int Status,
    DateTimeOffset CreatedAtUtc,
    Guid? ReviewedByUserId,
    DateTimeOffset? ReviewedAtUtc,
    string? ResolutionNote);

public sealed record ConversationReportSummaryResponse(
    Guid Id,
    Guid ConversationId,
    Guid ListingId,
    string ListingTitle,
    Guid ReporterId,
    Guid OtherUserId,
    int Reason,
    int Status,
    DateTimeOffset CreatedAtUtc,
    string? ReporterName = null,
    string? OtherUserName = null,
    int OpenOnConversation = 0);

public sealed record ConversationReportMessageResponse(
    Guid Id,
    Guid SenderId,
    string Body,
    DateTimeOffset SentAtUtc,
    DateTimeOffset? ReadAtUtc,
    IReadOnlyList<MessageImageResponse> Images);

public sealed record ConversationReportDetailsResponse(
    ConversationReportResponse Report,
    Guid ListingId,
    string ListingTitle,
    Guid BuyerId,
    Guid SellerId,
    IReadOnlyList<ConversationReportMessageResponse> Messages,
    string? ReporterName = null,
    string? BuyerName = null,
    string? SellerName = null);
