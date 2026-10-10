namespace RentoX.Application.Listings;

public sealed record PublicListingOwnerResult(
    Guid Id,
    string FullName,
    string PhoneNumber)
{
    /// <summary>The owner's active store, when they have one.</summary>
    public PublicListingOwnerStoreResult? Store { get; init; }
}

public sealed record PublicListingOwnerStoreResult(
    Guid Id,
    string Name,
    string Slug,
    bool HasLogoImage,
    long ImageVersion);