using RentoX.Domain.Users;

namespace RentoX.Application.Users;

public interface IUserProfileRepository
{
    Task<UserProfile?> GetByIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    void Add(UserProfile userProfile);
}