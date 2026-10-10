namespace RentoX.Application.Listings;

/// <summary>Whose listings to show.</summary>
public enum PublicListingSellerType
{
    Any = 0,

    /// <summary>Owners with a live store.</summary>
    Store = 1,

    /// <summary>Owners without a live store.</summary>
    Individual = 2
}
