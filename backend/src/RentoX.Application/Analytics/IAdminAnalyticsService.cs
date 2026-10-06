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

public interface IAdminAnalyticsService
{
    Task<AdminDashboardResult> GetDashboardAsync(
        CancellationToken cancellationToken = default);
}