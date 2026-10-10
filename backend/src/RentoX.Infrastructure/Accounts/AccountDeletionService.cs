using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RentoX.Application.Abstractions.Time;
using RentoX.Application.Accounts;
using RentoX.Application.Authorization;
using RentoX.Application.Files;
using RentoX.Domain.Authentication;
using RentoX.Domain.Common.Exceptions;
using RentoX.Domain.Listings;
using RentoX.Domain.Listings.Enums;
using RentoX.Domain.Notifications;
using RentoX.Domain.Stores;
using RentoX.Domain.Users;
using RentoX.Infrastructure.Identity;
using RentoX.Infrastructure.Persistence;

namespace RentoX.Infrastructure.Accounts;

public sealed partial class AccountDeletionService(
    RentoXDbContext dbContext,
    IFileStorage fileStorage,
    IClock clock,
    ILogger<AccountDeletionService> logger)
    : IAccountDeletionService
{
    public async Task<bool> DeleteAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty)
        {
            throw new DomainException("User id is required.");
        }

        AppUser? user =
            await dbContext.Users.SingleOrDefaultAsync(
                item => item.Id == userId,
                cancellationToken);

        if (user is null || user.PhoneNumber is null)
        {
            // Already deleted (the phone number is released on deletion).
            return false;
        }

        bool isStaff =
            await (
                from userRole in dbContext.Set<IdentityUserRole<Guid>>()
                join role in dbContext.Roles
                    on userRole.RoleId equals role.Id
                where userRole.UserId == userId &&
                      (role.Name == RoleNames.Admin ||
                       role.Name == RoleNames.SuperAdmin)
                select userRole.UserId)
            .AnyAsync(cancellationToken);

        if (isStaff)
        {
            throw new DomainException(
                "Staff accounts cannot be deleted from the app.");
        }

        DateTimeOffset now = clock.UtcNow;

        // Listings: out of every list and search.
        List<Listing> listings =
            await dbContext.Listings
                .Where(listing =>
                    listing.OwnerId == userId &&
                    listing.Status != ListingStatus.Deleted)
                .ToListAsync(cancellationToken);

        foreach (Listing listing in listings)
        {
            listing.MarkDeleted(now);
        }

        // The store, and the people following it.
        List<string> filesToRemove = [];

        StoreProfile? store =
            await dbContext.Set<StoreProfile>()
                .SingleOrDefaultAsync(
                    item => item.OwnerId == userId,
                    cancellationToken);

        if (store is not null)
        {
            if (!string.IsNullOrWhiteSpace(store.LogoImageKey))
            {
                filesToRemove.Add(store.LogoImageKey);
            }

            if (!string.IsNullOrWhiteSpace(store.CoverImageKey))
            {
                filesToRemove.Add(store.CoverImageKey);
            }

            store.MarkDeleted(now);

            dbContext.StoreFollowers.RemoveRange(
                await dbContext.StoreFollowers
                    .Where(follower => follower.StoreId == store.Id)
                    .ToListAsync(cancellationToken));
        }

        // What this person followed and liked.
        dbContext.StoreFollowers.RemoveRange(
            await dbContext.StoreFollowers
                .Where(follower => follower.UserId == userId)
                .ToListAsync(cancellationToken));

        dbContext.Favorites.RemoveRange(
            await dbContext.Favorites
                .Where(favorite => favorite.UserId == userId)
                .ToListAsync(cancellationToken));

        // Phones stop receiving pushes; sessions end.
        foreach (PushDevice device in await dbContext.Set<PushDevice>()
                     .Where(item => item.UserId == userId && item.IsActive)
                     .ToListAsync(cancellationToken))
        {
            device.Deactivate(now);
        }

        foreach (RefreshToken token in await dbContext.RefreshTokens
                     .Where(item =>
                         item.UserId == userId &&
                         item.RevokedAtUtc == null)
                     .ToListAsync(cancellationToken))
        {
            token.Revoke(now);
        }

        // Personal details are erased; the profile row stays so that
        // conversations can still say "deleted user".
        UserProfile? profile =
            await dbContext.UserProfiles.SingleOrDefaultAsync(
                item => item.Id == userId,
                cancellationToken);

        if (profile is not null)
        {
            if (!string.IsNullOrWhiteSpace(profile.ProfileImageKey))
            {
                filesToRemove.Add(profile.ProfileImageKey);
            }

            profile.Anonymize();
        }

        // The sign-in: free the phone number (the person may register
        // again later as a new user) and lock the old account for good.
        user.PhoneNumber = null;
        user.PhoneNumberConfirmed = false;
        user.Email = null;
        user.EmailConfirmed = false;
        user.UserName = $"deleted-{userId:N}";
        user.NormalizedUserName = user.UserName.ToUpperInvariant();
        user.NormalizedEmail = null;
        user.SecurityStamp = Guid.NewGuid().ToString("N");
        user.LockoutEnabled = true;
        user.LockoutEnd = DateTimeOffset.MaxValue;

        await dbContext.SaveChangesAsync(cancellationToken);

        foreach (string key in filesToRemove)
        {
            try
            {
                await fileStorage.DeleteAsync(key, cancellationToken);
            }
            catch (Exception exception)
                when (exception is IOException
                      or UnauthorizedAccessException)
            {
                LogFileLeftBehind(logger, exception, key);
            }
        }

        return true;
    }

    [LoggerMessage(
        EventId = 3001,
        Level = LogLevel.Warning,
        Message = "Could not remove file {Key} of a deleted account.")]
    private static partial void LogFileLeftBehind(
        ILogger logger,
        Exception exception,
        string key);
}
