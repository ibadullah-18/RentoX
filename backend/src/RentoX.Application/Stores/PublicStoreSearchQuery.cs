namespace RentoX.Application.Stores;

/// <summary>
/// Looks up public stores. Without <see cref="Search"/> the most active
/// stores come first.
/// </summary>
public sealed record PublicStoreSearchQuery(
    string? Search,
    int Page = 1,
    int PageSize = 20);
