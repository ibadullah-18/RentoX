namespace RentoX.Application.Listings;

public interface IListingUpdateService
{
    Task<UpdateListingResult> UpdateAsync(
        UpdateListingCommand command,
        CancellationToken cancellationToken = default);
}