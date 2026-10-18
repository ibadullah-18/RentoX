using Microsoft.EntityFrameworkCore;
using RentoX.Application.Stores;
using RentoX.Domain.Common.Exceptions;
using RentoX.Domain.Stores;
using RentoX.Infrastructure.Persistence;

namespace RentoX.Infrastructure.Stores;

public sealed class StoreSubmissionService(
    RentoXDbContext dbContext,
    RentoX.Application.Support.ISupportEventPublisher? live = null)
    : IStoreSubmissionService
{
    public async Task<StoreStatusResult> SubmitAsync(
        Guid ownerId,
        CancellationToken cancellationToken = default)
    {
        StoreProfile store =
            await dbContext.StoreProfiles
                .SingleOrDefaultAsync(
                    item =>
                        item.OwnerId == ownerId,
                    cancellationToken)
            ?? throw new DomainException(
                "Store was not found.");

        store.SubmitForReview();

        await dbContext.SaveChangesAsync(
            cancellationToken);

        if (live is not null)
        {
            await live.ReviewWaitingAsync("store", store.Id, store.Name, cancellationToken);
        }

        return new StoreStatusResult(
            store.Id,
            (int)store.Status,
            store.RejectionReason,
            store.UpdatedAtUtc);
    }
}