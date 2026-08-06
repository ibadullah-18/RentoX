using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using RentoX.Application.Abstractions.Authentication;
using RentoX.Application.Abstractions.Time;
using RentoX.Domain.Common;

namespace RentoX.Infrastructure.Persistence.Interceptors;

public sealed class AuditableEntityInterceptor(
    IClock clock,
    ICurrentUserContext currentUserContext)
    : SaveChangesInterceptor
{
    public override ValueTask<InterceptionResult<int>>
        SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
    {
        if (eventData.Context is null)
        {
            return base.SavingChangesAsync(
                eventData,
                result,
                cancellationToken);
        }

        foreach (var entry in eventData.Context.ChangeTracker
                     .Entries<AuditableEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.MarkAsCreated(
                        clock.UtcNow,
                        currentUserContext.UserId);
                    break;

                case EntityState.Modified:
                    entry.Entity.MarkAsUpdated(
                        clock.UtcNow,
                        currentUserContext.UserId);
                    break;

                case EntityState.Deleted:
                    entry.State = EntityState.Modified;

                    entry.Entity.MarkAsDeleted(clock.UtcNow);
                    entry.Entity.MarkAsUpdated(
                        clock.UtcNow,
                        currentUserContext.UserId);
                    break;
            }
        }

        return base.SavingChangesAsync(
            eventData,
            result,
            cancellationToken);
    }
}