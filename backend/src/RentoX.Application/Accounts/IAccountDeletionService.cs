namespace RentoX.Application.Accounts;

public interface IAccountDeletionService
{
    /// <summary>
    /// Deletes the user's account: the sign-in is closed, every session and
    /// device is dropped, listings and the store are removed from view, and
    /// the personal details are erased. Conversations stay for the other
    /// person, showing a "deleted user". Returns <c>false</c> when the
    /// account does not exist (or was already deleted).
    /// </summary>
    Task<bool> DeleteAsync(
        Guid userId,
        CancellationToken cancellationToken = default);
}
