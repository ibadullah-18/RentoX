namespace RentoX.Application.Accounts;

public interface IProfileImageService
{
    Task<ProfileImageResult> UploadAsync(
        UploadProfileImageCommand command,
        CancellationToken cancellationToken = default);

    Task<ProfileImageContentResult?> OpenAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(
        Guid userId,
        CancellationToken cancellationToken = default);
}