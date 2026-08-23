namespace RentoX.Application.Listings;

public interface IListingSubmissionService
{
    Task<SubmitListingResult> SubmitAsync(
        SubmitListingCommand command,
        CancellationToken cancellationToken = default);
}