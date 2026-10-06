using RentoX.Application.Common;

namespace RentoX.Application.Messaging;

public sealed record CreateConversationReportCommand(
    Guid UserId,
    Guid ConversationId,
    int Reason,
    string? Details,
    Guid? EvidenceMessageId);

public sealed record ConversationReportResult(
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

public sealed record ConversationReportSummaryResult(
    Guid Id,
    Guid ConversationId,
    Guid ListingId,
    string ListingTitle,
    Guid ReporterId,
    Guid OtherUserId,
    int Reason,
    int Status,
    DateTimeOffset CreatedAtUtc);

public sealed record ConversationReportMessageResult(
    Guid Id,
    Guid SenderId,
    string Body,
    DateTimeOffset SentAtUtc,
    DateTimeOffset? ReadAtUtc,
    IReadOnlyList<MessageImageResult> Images);

public sealed record ConversationReportDetailsResult(
    ConversationReportResult Report,
    Guid ListingId,
    string ListingTitle,
    Guid BuyerId,
    Guid SellerId,
    IReadOnlyList<ConversationReportMessageResult> Messages);

public interface IConversationReportService
{
    Task<ConversationReportResult?> CreateAsync(
        CreateConversationReportCommand command,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ConversationReportResult>?> GetMineAsync(
        Guid userId,
        Guid conversationId,
        CancellationToken cancellationToken = default);

    Task<PagedResult<ConversationReportSummaryResult>> GetForAdminAsync(
        int? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<ConversationReportDetailsResult?> GetDetailsAsync(
        Guid reportId,
        CancellationToken cancellationToken = default);

    Task<ConversationReportResult?> ReviewAsync(
        Guid reviewerId,
        Guid reportId,
        int status,
        string? resolutionNote,
        CancellationToken cancellationToken = default);

    Task<MessageImageDownload?> OpenEvidenceImageAsync(
        Guid reportId,
        Guid imageId,
        CancellationToken cancellationToken = default);
}
