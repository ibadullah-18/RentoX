namespace RentoX.Contracts.Listings;

public sealed record PublicListingOwnerResponse(
    Guid Id,
    string FullName,
    string PhoneNumber)
{
    /// <summary>The owner's active store, when they have one.</summary>
    public PublicListingOwnerStoreResponse? Store { get; init; }
}

public sealed record PublicListingOwnerStoreResponse(
    Guid Id,
    string Name,
    string Slug,
    string? LogoImageUrl);