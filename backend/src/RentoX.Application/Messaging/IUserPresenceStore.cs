namespace RentoX.Application.Messaging;

public interface IUserPresenceStore
{
    Task<bool> RefreshAsync(
        Guid userId,
        string connectionId,
        CancellationToken cancellationToken = default);

    Task RemoveAsync(
        Guid userId,
        string connectionId,
        CancellationToken cancellationToken = default);

    // Null means the presence service is unavailable.
    Task<bool?> IsOnlineAsync(
        Guid userId,
        CancellationToken cancellationToken = default);
}
