using Microsoft.EntityFrameworkCore;
using RentoX.Application.Abstractions.Time;
using RentoX.Application.Analytics;
using RentoX.Domain.Favorites;
using RentoX.Domain.Listings;
using RentoX.Domain.Listings.Enums;
using RentoX.Domain.Messaging;
using RentoX.Domain.Stores;
using RentoX.Domain.Stores.Enums;
using RentoX.Domain.Support;
using RentoX.Domain.Wallets;
using RentoX.Infrastructure.Identity;
using RentoX.Infrastructure.Persistence;

namespace RentoX.Infrastructure.Analytics;

public sealed class AdminAnalyticsService(
    RentoXDbContext dbContext,
    IClock clock)
    : IAdminAnalyticsService
{
    public async Task<AdminDashboardResult> GetDashboardAsync(
        CancellationToken cancellationToken = default)
    {
        DateTimeOffset now = clock.UtcNow;
        DateTimeOffset from7 = now.AddDays(-7);
        DateTimeOffset from30 = now.AddDays(-30);

        // Administrative totals include soft-deleted records.
        // These queries are only exposed through the admin endpoint.
        IQueryable<Listing> allListings =
            dbContext.Set<Listing>()
                .IgnoreQueryFilters()
                .AsNoTracking();

        IQueryable<StoreProfile> allStores =
            dbContext.Set<StoreProfile>()
                .IgnoreQueryFilters()
                .AsNoTracking();

        List<AdminActivityResult> activity = [];

        activity.Add(await ReadActivityAsync(
            "users",
            dbContext.Set<AppUser>()
                .AsNoTracking()
                .Select(user => user.RegisteredAtUtc),
            now, from7, from30, cancellationToken));

        activity.Add(await ReadActivityAsync(
            "listings",
            allListings.Select(listing => listing.CreatedAtUtc),
            now, from7, from30, cancellationToken));

        activity.Add(await ReadActivityAsync(
            "conversations",
            dbContext.Set<Conversation>()
                .AsNoTracking()
                .Select(conversation => conversation.CreatedAtUtc),
            now, from7, from30, cancellationToken));

        activity.Add(await ReadActivityAsync(
            "messages",
            dbContext.Set<Message>()
                .AsNoTracking()
                .Select(message => message.SentAtUtc),
            now, from7, from30, cancellationToken));

        activity.Add(await ReadActivityAsync(
            "supportTickets",
            dbContext.Set<SupportTicket>()
                .AsNoTracking()
                .Select(ticket => ticket.CreatedAtUtc),
            now, from7, from30, cancellationToken));

        List<AdminStatusCountResult> statusCounts = [];

        statusCounts.AddRange(
            await ReadStatusCountsAsync<ListingStatus>(
                "listings",
                allListings.Select(listing => (int)listing.Status),
                cancellationToken));

        statusCounts.AddRange(
            await ReadStatusCountsAsync<StoreStatus>(
                "stores",
                allStores.Select(store => (int)store.Status),
                cancellationToken));

        statusCounts.AddRange(
            await ReadStatusCountsAsync<SupportTicketStatus>(
                "support",
                dbContext.Set<SupportTicket>()
                    .AsNoTracking()
                    .Select(ticket => (int)ticket.Status),
                cancellationToken));

        AdminWalletBalanceResult[] balances =
            await dbContext.Set<Wallet>()
                .AsNoTracking()
                .GroupBy(wallet => wallet.Currency)
                .OrderBy(group => group.Key)
                .Select(group => new AdminWalletBalanceResult(
                    group.Key,
                    group.LongCount(),
                    group.Sum(wallet => wallet.Balance)))
                .ToArrayAsync(cancellationToken);

        // The current data model stores currency on the wallet,
        // not on each transaction.
        var transactions =
            from transaction in dbContext.Set<WalletTransaction>()
                .AsNoTracking()
            join wallet in dbContext.Set<Wallet>().AsNoTracking()
                on transaction.WalletId equals wallet.Id
            where transaction.OccurredAtUtc <= now
            select new
            {
                wallet.Currency,
                transaction.Direction,
                transaction.Type,
                transaction.Amount,
                transaction.OccurredAtUtc
            };

        AdminWalletActivityResult[] walletActivity =
            await transactions
                .GroupBy(item => new
                {
                    item.Currency,
                    item.Direction,
                    item.Type
                })
                .OrderBy(group => group.Key.Currency)
                .ThenBy(group => group.Key.Direction)
                .ThenBy(group => group.Key.Type)
                .Select(group => new AdminWalletActivityResult(
                    group.Key.Currency,
                    (int)group.Key.Direction,
                    (int)group.Key.Type,
                    group.LongCount(),
                    group.Sum(item => item.Amount),
                    group.Sum(item =>
                        item.OccurredAtUtc >= from7
                            ? item.Amount
                            : 0m),
                    group.Sum(item =>
                        item.OccurredAtUtc >= from30
                            ? item.Amount
                            : 0m)))
                .ToArrayAsync(cancellationToken);

        // Rankings respect normal query filters and exclude Deleted.
        // ViewCount is the existing listing counter, not a new count
        // of ListingView records.
        var rankedListings =
            dbContext.Set<Listing>()
                .AsNoTracking()
                .Where(listing =>
                    listing.Status != ListingStatus.Deleted)
                .Select(listing => new
                {
                    listing.Id,
                    listing.Title,
                    listing.Status,
                    listing.ViewCount,
                    FavoriteCount = dbContext.Set<Favorite>()
                        .LongCount(favorite =>
                            favorite.ListingId == listing.Id)
                });

        AdminTopListingResult[] mostViewed =
            await rankedListings
                .Where(listing => listing.ViewCount > 0)
                .OrderByDescending(listing => listing.ViewCount)
                .ThenBy(listing => listing.Id)
                .Take(10)
                .Select(listing => new AdminTopListingResult(
                    listing.Id,
                    listing.Title,
                    (int)listing.Status,
                    listing.ViewCount,
                    listing.FavoriteCount))
                .ToArrayAsync(cancellationToken);

        AdminTopListingResult[] mostFavorited =
            await rankedListings
                .Where(listing => listing.FavoriteCount > 0)
                .OrderByDescending(listing => listing.FavoriteCount)
                .ThenBy(listing => listing.Id)
                .Take(10)
                .Select(listing => new AdminTopListingResult(
                    listing.Id,
                    listing.Title,
                    (int)listing.Status,
                    listing.ViewCount,
                    listing.FavoriteCount))
                .ToArrayAsync(cancellationToken);

        return new AdminDashboardResult(
            now,
            from7,
            from30,
            activity,
            statusCounts,
            balances,
            walletActivity,
            mostViewed,
            mostFavorited);
    }

    private static async Task<AdminActivityResult> ReadActivityAsync(
        string metric,
        IQueryable<DateTimeOffset> timestamps,
        DateTimeOffset now,
        DateTimeOffset from7,
        DateTimeOffset from30,
        CancellationToken cancellationToken)
    {
        var counts = await timestamps
            .Where(timestamp => timestamp <= now)
            .GroupBy(timestamp => 1)
            .Select(group => new
            {
                Total = group.LongCount(),
                Last7Days = group.LongCount(timestamp =>
                    timestamp >= from7),
                Last30Days = group.LongCount(timestamp =>
                    timestamp >= from30)
            })
            .SingleOrDefaultAsync(cancellationToken);

        return new AdminActivityResult(
            metric,
            counts?.Total ?? 0,
            counts?.Last7Days ?? 0,
            counts?.Last30Days ?? 0);
    }

    private static async Task<AdminStatusCountResult[]>
        ReadStatusCountsAsync<TStatus>(
            string module,
            IQueryable<int> statuses,
            CancellationToken cancellationToken)
        where TStatus : struct, Enum
    {
        Dictionary<int, long> counts = await statuses
            .GroupBy(status => status)
            .Select(group => new
            {
                Status = group.Key,
                Count = group.LongCount()
            })
            .ToDictionaryAsync(
                item => item.Status,
                item => item.Count,
                cancellationToken);

        return Enum.GetValues<TStatus>()
            .Select(value => Convert.ToInt32(
                value,
                System.Globalization.CultureInfo.InvariantCulture))
            .OrderBy(value => value)
            .Select(value => new AdminStatusCountResult(
                module,
                value,
                counts.GetValueOrDefault(value)))
            .ToArray();
    }
}