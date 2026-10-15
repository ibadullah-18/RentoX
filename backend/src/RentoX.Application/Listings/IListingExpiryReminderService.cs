namespace RentoX.Application.Listings;

/// <summary>Tells owners that a listing of theirs is about to run out, once per listing.</summary>
public interface IListingExpiryReminderService
{
    /// <param name="before">How long before the end the reminder goes out.</param>
    /// <param name="batchSize">The most reminders written in one run.</param>
    /// <returns>How many reminders were written.</returns>
    Task<int> RemindAsync(
        TimeSpan before,
        int batchSize,
        CancellationToken cancellationToken = default);
}
