namespace RentoX.Application.Listings;

public sealed record PublicListingOwnerResult(
    Guid Id,
    string FullName,
    string PhoneNumber);