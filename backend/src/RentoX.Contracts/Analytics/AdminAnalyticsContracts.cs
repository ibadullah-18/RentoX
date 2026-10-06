namespace RentoX.Contracts.Analytics;

public sealed record AdminActivityResponse(
    string Metric,
    long Total,
    long Last7Days,
    long Last30Days);

public sealed record AdminStatusCountResponse(
    string Module,
    int Status,
    long Count);

public sealed record AdminWalletBalanceResponse(
    string Currency,
    long WalletCount,
    decimal TotalBalance);

public sealed record AdminWalletActivityResponse(
    string Currency,
    int Direction,
    int Type,
    long TransactionCount,
    decimal TotalAmount,
    decimal Last7DaysAmount,
    decimal Last30DaysAmount);

public sealed record AdminTopListingResponse(
    Guid Id,
    string Title,
    int Status,
    long ViewCount,
    long FavoriteCount);

public sealed record AdminDashboardResponse(
    DateTimeOffset GeneratedAtUtc,
    DateTimeOffset Last7DaysFromUtc,
    DateTimeOffset Last30DaysFromUtc,
    IReadOnlyList<AdminActivityResponse> Activity,
    IReadOnlyList<AdminStatusCountResponse> StatusCounts,
    IReadOnlyList<AdminWalletBalanceResponse> WalletBalances,
    IReadOnlyList<AdminWalletActivityResponse> WalletActivity,
    IReadOnlyList<AdminTopListingResponse> MostViewedListings,
    IReadOnlyList<AdminTopListingResponse> MostFavoritedListings);