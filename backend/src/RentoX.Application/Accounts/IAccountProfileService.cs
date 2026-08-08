namespace RentoX.Application.Accounts;

public interface IAccountProfileService
{
    Task<AccountProfileResult?> GetAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<AccountProfileResult?> UpdateAsync(
        Guid userId,
        string fullName,
        string? bio,
        int preferredLanguage,
        CancellationToken cancellationToken = default);
}