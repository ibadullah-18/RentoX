using Microsoft.EntityFrameworkCore;
using RentoX.Application.Accounts;
using RentoX.Domain.Users;
using RentoX.Infrastructure.Persistence;

namespace RentoX.Infrastructure.Accounts;

public sealed class AccountNotificationSettingsService(RentoXDbContext dbContext)
    : IAccountNotificationSettingsService
{
    public async Task<bool?> GetAnnouncementsEnabledAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await dbContext.Set<UserProfile>()
            .AsNoTracking()
            .Where(p => p.Id == userId)
            .Select(p => (bool?)p.AnnouncementsEnabled)
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<bool> SetAnnouncementsEnabledAsync(Guid userId, bool enabled, CancellationToken cancellationToken = default)
    {
        UserProfile? profile = await dbContext.Set<UserProfile>()
            .SingleOrDefaultAsync(p => p.Id == userId, cancellationToken);
        if (profile is null)
        {
            return false;
        }

        profile.SetAnnouncementsEnabled(enabled);
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}
