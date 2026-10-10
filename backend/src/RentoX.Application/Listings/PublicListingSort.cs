namespace RentoX.Application.Listings;

/// <summary>How search results are ordered.</summary>
public enum PublicListingSort
{
    /// <summary>
    /// Best match when searching text; otherwise live VIP listings first,
    /// then newest (or most recently bumped).
    /// </summary>
    Default = 0,

    PriceAscending = 1,

    PriceDescending = 2
}
