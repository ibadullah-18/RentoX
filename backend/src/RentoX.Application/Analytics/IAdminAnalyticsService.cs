namespace RentoX.Application.Analytics;

public sealed record AdminActivityResult(
    string Metric,
    long Total,
    long Last7Days,
    long Last30Days);

public sealed record AdminStatusCountResult(
    string Module,
    int Status,
    long Count);

public sealed record AdminWalletBalanceResult(
    string Currency,
    long WalletCount,
    decimal TotalBalance);

public sealed record AdminWalletActivityResult(
    string Currency,
    int Direction,
    int Type,
    long TransactionCount,
    decimal TotalAmount,
    decimal Last7DaysAmount,
    decimal Last30DaysAmount);

public sealed record AdminTopListingResult(
    Guid Id,
    string Title,
    int Status,
    long ViewCount,
    long FavoriteCount);

public sealed record AdminDashboardResult(
    DateTimeOffset GeneratedAtUtc,
    DateTimeOffset Last7DaysFromUtc,
    DateTimeOffset Last30DaysFromUtc,
    IReadOnlyList<AdminActivityResult> Activity,
    IReadOnlyList<AdminStatusCountResult> StatusCounts,
    IReadOnlyList<AdminWalletBalanceResult> WalletBalances,
    IReadOnlyList<AdminWalletActivityResult> WalletActivity,
    IReadOnlyList<AdminTopListingResult> MostViewedListings,
    IReadOnlyList<AdminTopListingResult> MostFavoritedListings);

/// <summary>What happened on one day (UTC): new people and new listings.</summary>
public sealed record AdminDailyPoint(DateOnly Date, int NewUsers, int NewListings);

/// <summary>How many listings are on show in a category (only the category itself, not the ones below it).</summary>
public sealed record AdminCategoryCount(Guid CategoryId, int ActiveListings);

public interface IAdminAnalyticsService
{
    /// <summary>One point per day for the last <paramref name="days"/> days (7 to 90), oldest first, empty days included.</summary>
    /// <summary>Active listings per category, most first; categories without any are left out.</summary>
    Task<IReadOnlyList<AdminCategoryCount>> GetCategoryCountsAsync(
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AdminDailyPoint>> GetDailyAsync(
        int days,
        CancellationToken cancellationToken = default);

    Task<AdminDashboardResult> GetDashboardAsync(
        CancellationToken cancellationToken = default);
}