namespace RentoX.Application.Listings;

public sealed record CreateListingCommand(
    Guid OwnerId,
    Guid CategoryId,
    string Title,
    string Description,
    decimal Price,
    string Currency,
    int RentalPeriodUnit,
    IReadOnlyList<CreateListingFieldInput> Fields);