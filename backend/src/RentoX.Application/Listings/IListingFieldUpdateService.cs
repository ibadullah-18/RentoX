namespace RentoX.Application.Listings;

public interface IListingFieldUpdateService
{
    Task<UpdateListingFieldsResult> UpdateAsync(
        UpdateListingFieldsCommand command,
        CancellationToken cancellationToken = default);
}