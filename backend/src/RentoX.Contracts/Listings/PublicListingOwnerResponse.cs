namespace RentoX.Contracts.Listings;

public sealed record PublicListingOwnerResponse(
    Guid Id,
    string FullName,
    string PhoneNumber);