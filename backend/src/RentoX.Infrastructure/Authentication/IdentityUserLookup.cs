using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RentoX.Application.Authentication;
using RentoX.Infrastructure.Identity;

namespace RentoX.Infrastructure.Authentication;

public sealed class IdentityUserLookup(
    UserManager<AppUser> userManager)
    : IIdentityUserLookup
{
    public Task<bool> PhoneExistsAsync(
        string phoneNumber,
        CancellationToken cancellationToken = default)
    {
        return userManager.Users.AnyAsync(
            user => user.PhoneNumber == phoneNumber,
            cancellationToken);
    }
}