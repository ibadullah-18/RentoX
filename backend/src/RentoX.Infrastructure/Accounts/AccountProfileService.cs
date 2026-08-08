using Microsoft.EntityFrameworkCore;
using RentoX.Application.Accounts;
using RentoX.Domain.Users;
using RentoX.Domain.Users.Enums;
using RentoX.Infrastructure.Persistence;

namespace RentoX.Infrastructure.Accounts;

public sealed class AccountProfileService(
    RentoXDbContext dbContext)
    : IAccountProfileService
{
    public async Task<AccountProfileResult?> GetAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        UserProfile? profile =
            await dbContext.Set<UserProfile>()
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    item => item.Id == userId,
                    cancellationToken);

        if (profile is null)
        {
            return null;
        }

        string? phoneNumber =
            await dbContext.Users
                .Where(user => user.Id == userId)
                .Select(user => user.PhoneNumber)
                .SingleOrDefaultAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(phoneNumber))
        {
            return null;
        }

        return CreateResult(profile, phoneNumber);
    }

    public async Task<AccountProfileResult?> UpdateAsync(
        Guid userId,
        string fullName,
        string? bio,
        int preferredLanguage,
        CancellationToken cancellationToken = default)
    {
        if (!Enum.IsDefined(
                typeof(PreferredLanguage),
                preferredLanguage))
        {
            return null;
        }

        UserProfile? profile =
            await dbContext.Set<UserProfile>()
                .SingleOrDefaultAsync(
                    item => item.Id == userId,
                    cancellationToken);

        if (profile is null)
        {
            return null;
        }

        string? phoneNumber =
            await dbContext.Users
                .Where(user => user.Id == userId)
                .Select(user => user.PhoneNumber)
                .SingleOrDefaultAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(phoneNumber))
        {
            return null;
        }

        profile.Update(
            fullName,
            bio,
            (PreferredLanguage)preferredLanguage);

        await dbContext.SaveChangesAsync(cancellationToken);

        return CreateResult(profile, phoneNumber);
    }

    private static AccountProfileResult CreateResult(
        UserProfile profile,
        string phoneNumber)
    {
        return new AccountProfileResult(
            profile.Id,
            phoneNumber,
            profile.FullName,
            profile.Bio,
            (int)profile.PreferredLanguage,
            (int)profile.Status);
    }
}