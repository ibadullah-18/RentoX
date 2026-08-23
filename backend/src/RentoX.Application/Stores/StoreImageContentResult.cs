namespace RentoX.Application.Stores;

public sealed record StoreImageContentResult(
    Stream Content,
    string ContentType);