namespace RentoX.Application.Stores;

public interface IStoreSubmissionService
{
    Task<StoreStatusResult> SubmitAsync(
        Guid ownerId,
        CancellationToken cancellationToken = default);
}