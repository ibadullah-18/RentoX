namespace RentoX.Application.Stores;

public sealed record UploadStoreImageCommand(
    Guid OwnerId,
    StoreImageKind Kind,
    Stream Content,
    string FileName,
    string ContentType,
    long SizeBytes);