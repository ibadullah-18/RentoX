using Microsoft.EntityFrameworkCore;
using RentoX.Application.Users;
using RentoX.Domain.Users;
using RentoX.Infrastructure.Persistence;

namespace RentoX.Infrastructure.Users;

public sealed class UserProfileRepository(
    RentoXDbContext dbContext)
    : IUserProfileRepository
{
    public Task<UserProfile?> GetByIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return dbContext.UserProfiles.SingleOrDefaultAsync(
            profile => profile.Id == userId,
            cancellationToken);
    }

    public void Add(UserProfile userProfile)
    {
        ArgumentNullException.ThrowIfNull(userProfile);

        dbContext.UserProfiles.Add(userProfile);
    }
}