using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RentoX.Application.Abstractions.Authentication;
using RentoX.Application.Abstractions.Time;
using RentoX.Application.Admin;
using RentoX.Application.Authorization;
using RentoX.Domain.Auditing;
using RentoX.Domain.Authentication;
using RentoX.Domain.Common.Exceptions;
using RentoX.Domain.Listings;
using RentoX.Domain.Listings.Enums;
using RentoX.Domain.Stores;
using RentoX.Domain.Stores.Enums;
using RentoX.Domain.Users;
using RentoX.Domain.Users.Enums;
using RentoX.Infrastructure.Persistence;

namespace RentoX.Infrastructure.Admin;

public sealed class UserModerationService(
    RentoXDbContext dbContext,
    IClock clock,
    ICurrentUserContext currentUserContext,
    IUserStatusGuard statusGuard)
    : IUserModerationService
{
    public async Task BlockAsync(Guid userId, string reason, CancellationToken cancellationToken = default)
    {
        Guid actor = RequireActor();
        if (userId == actor)
        {
            throw new DomainException("You cannot block yourself.");
        }

        UserProfile profile = await dbContext.Set<UserProfile>()
            .SingleOrDefaultAsync(p => p.Id == userId, cancellationToken)
            ?? throw new DomainException("User was not found.");

        if (await IsStaffAsync(userId, cancellationToken))
        {
            throw new DomainException("Staff accounts cannot be blocked.");
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        Guid operation = Guid.NewGuid();
        DateTimeOffset now = clock.UtcNow;
        UserStatus previous = profile.Status;

        profile.Block(reason);
        dbContext.Set<AuditLogEntry>().Add(
            AuditLogEntry.ForUserStatusChange(operation, actor, userId, previous, profile.Status, now));

        // Every session ends: the refresh tokens cannot renew an access token.
        List<RefreshToken> tokens = await dbContext.RefreshTokens
            .Where(t => t.UserId == userId && t.RevokedAtUtc == null)
            .ToListAsync(cancellationToken);
        foreach (RefreshToken token in tokens)
        {
            token.Revoke(now);
        }

        // Nothing of theirs stays on show: the listings go down with the reason.
        string why = "Hesab bloklandı. " + profile.StatusReason;
        List<Listing> listings = await dbContext.Listings
            .Where(l => l.OwnerId == userId &&
                (l.Status == ListingStatus.Active ||
                 l.Status == ListingStatus.Deactivated ||
                 l.Status == ListingStatus.PendingReview ||
                 l.Status == ListingStatus.PaymentRequired))
            .ToListAsync(cancellationToken);
        foreach (Listing listing in listings)
        {
            ListingStatus before = listing.Status;
            listing.TakeDown(why.Length > 1000 ? why[..1000] : why);
            dbContext.Set<AuditLogEntry>().Add(
                AuditLogEntry.ForListingStatusChange(operation, actor, listing.Id, before, listing.Status, now));
        }

        StoreProfile? store = await dbContext.StoreProfiles
            .SingleOrDefaultAsync(s => s.OwnerId == userId && s.Status == StoreStatus.Active, cancellationToken);
        if (store is not null)
        {
            StoreStatus before = store.Status;
            store.Suspend(why.Length > 1000 ? why[..1000] : why);
            dbContext.Set<AuditLogEntry>().Add(
                AuditLogEntry.ForStoreStatusChange(operation, actor, store.Id, before, store.Status, now));
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        statusGuard.Forget(userId);
    }

    public async Task UnblockAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        Guid actor = RequireActor();

        UserProfile profile = await dbContext.Set<UserProfile>()
            .SingleOrDefaultAsync(p => p.Id == userId, cancellationToken)
            ?? throw new DomainException("User was not found.");

        UserStatus previous = profile.Status;
        profile.Unblock();

        dbContext.Set<AuditLogEntry>().Add(
            AuditLogEntry.ForUserStatusChange(Guid.NewGuid(), actor, userId, previous, profile.Status, clock.UtcNow));

        await dbContext.SaveChangesAsync(cancellationToken);
        statusGuard.Forget(userId);
    }

    private async Task<bool> IsStaffAsync(Guid userId, CancellationToken cancellationToken)
    {
        return await (
            from userRole in dbContext.Set<IdentityUserRole<Guid>>()
            join role in dbContext.Roles on userRole.RoleId equals role.Id
            where userRole.UserId == userId &&
                  (role.Name == RoleNames.Admin || role.Name == RoleNames.SuperAdmin)
            select userRole.UserId)
            .AnyAsync(cancellationToken);
    }

    private Guid RequireActor()
    {
        Guid? id = currentUserContext.UserId;
        if (!currentUserContext.IsAuthenticated || !id.HasValue || id.Value == Guid.Empty)
        {
            throw new DomainException("An authenticated audit actor is required.");
        }

        return id.Value;
    }
}
