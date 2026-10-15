using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using RentoX.Application.Admin;
using RentoX.Domain.Users;
using RentoX.Domain.Users.Enums;
using RentoX.Infrastructure.Persistence;

namespace RentoX.Infrastructure.Admin;

public sealed class UserStatusGuard(RentoXDbContext dbContext, IMemoryCache cache) : IUserStatusGuard
{
    private static readonly TimeSpan Keep = TimeSpan.FromSeconds(20);

    public async Task<bool> IsAllowedAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        if (cache.TryGetValue(Key(userId), out bool known))
        {
            return known;
        }

        UserStatus? status = await dbContext.Set<UserProfile>()
            .AsNoTracking()
            .Where(p => p.Id == userId)
            .Select(p => (UserStatus?)p.Status)
            .SingleOrDefaultAsync(cancellationToken);

        // No profile (or a deleted one) is left to the other checks; only a
        // blocked or suspended account is stopped here.
        bool allowed = status is null or UserStatus.Active;
        cache.Set(Key(userId), allowed, Keep);
        return allowed;
    }

    public void Forget(Guid userId) => cache.Remove(Key(userId));

    private static string Key(Guid userId) => $"user-allowed:{userId:N}";
}
