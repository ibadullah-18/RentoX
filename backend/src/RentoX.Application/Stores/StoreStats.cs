namespace RentoX.Application.Stores;

/// <summary>One open day of a store (1 Monday … 7 Sunday; the hours as minutes after midnight).</summary>
public sealed record StoreHoursDay(int Day, int OpenMinute, int CloseMinute);

public sealed record UpdateStoreContactCommand(
    Guid OwnerId,
    string PhoneNumber,
    string? Email,
    string? Address,
    string? InstagramUrl,
    string? TiktokUrl,
    string? FacebookUrl,
    string? YoutubeUrl,
    string? WebsiteUrl,
    string? WhatsAppNumber,
    string? WhatsAppMessage,
    IReadOnlyList<StoreHoursDay>? WorkingHours);

public sealed record StoreDayPoint(DateOnly Day, int Views, int Calls, int WhatsApp, int Messages);

/// <summary>What the owner sees about their store: who follows it, how often it is looked at and contacted.</summary>
public sealed record StoreStatsResult(
    long Followers,
    long ActiveListings,
    long ListingViews,
    long ListingFavorites,
    long ProfileViews,
    long Calls,
    long WhatsApp,
    long Messages,
    long ProfileViewsLast7Days,
    long ProfileViewsLast30Days,
    long ContactsLast30Days,
    IReadOnlyList<StoreDayPoint> Last30Days);

public interface IStoreStatsService
{
    /// <summary>Counts one event on the store's page: view, call, whatsapp or message. The owner's own visits do not count.</summary>
    Task<bool> RecordAsync(string slug, string kind, Guid? viewerUserId, CancellationToken cancellationToken = default);

    Task<StoreStatsResult?> GetMineAsync(Guid ownerId, CancellationToken cancellationToken = default);
}
