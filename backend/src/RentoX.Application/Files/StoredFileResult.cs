namespace RentoX.Application.Files;

public sealed record StoredFileResult(
    string StorageKey,
    string ContentType,
    long SizeBytes);