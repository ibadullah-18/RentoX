namespace RentoX.Application.Stores;

public sealed record StoreImageResult(
    Guid StoreId,
    StoreImageKind Kind);