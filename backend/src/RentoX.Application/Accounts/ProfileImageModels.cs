namespace RentoX.Application.Accounts;

public sealed record UploadProfileImageCommand(
    Guid UserId,
    Stream Content,
    string FileName,
    string ContentType,
    long SizeBytes);

public sealed record ProfileImageResult(
    Guid UserId,
    string Url);

public sealed record ProfileImageContentResult(
    Stream Content,
    string ContentType);