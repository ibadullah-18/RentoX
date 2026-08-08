using Microsoft.EntityFrameworkCore;
using RentoX.Application.Authentication;
using RentoX.Infrastructure.Persistence;

namespace RentoX.Infrastructure.Authentication;

public sealed class LoginAccountService(
    RentoXDbContext dbContext)
    : ILoginAccountService
{
    public async Task<Guid?> FindUserIdAsync(
        string phoneNumber,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.Users
            .Where(user =>
                user.PhoneNumber == phoneNumber)
            .Select(user => (Guid?)user.Id)
            .SingleOrDefaultAsync(cancellationToken);
    }
}