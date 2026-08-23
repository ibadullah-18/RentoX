namespace RentoX.Application.Stores;

public sealed record StoreStatusResult(
    Guid StoreId,
    int Status,
    string? RejectionReason,
    DateTimeOffset? UpdatedAtUtc);