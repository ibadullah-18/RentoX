namespace RentoX.Application.Listings;

public interface IListingCreationService
{
    Task<CreateListingResult> CreateAsync(
        CreateListingCommand command,
        CancellationToken cancellationToken = default);
}