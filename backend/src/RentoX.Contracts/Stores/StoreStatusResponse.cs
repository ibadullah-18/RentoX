namespace RentoX.Contracts.Stores;

public sealed record StoreStatusResponse(
    Guid StoreId,
    int Status,
    string? RejectionReason,
    DateTimeOffset? UpdatedAtUtc);