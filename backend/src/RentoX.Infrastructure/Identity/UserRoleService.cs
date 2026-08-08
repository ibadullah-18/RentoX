using Microsoft.AspNetCore.Identity;
using RentoX.Application.Authorization;
using RentoX.Domain.Common.Exceptions;

namespace RentoX.Infrastructure.Identity;

public sealed class UserRoleService(
    UserManager<AppUser> userManager)
    : IUserRoleService
{
    public async Task AssignDefaultRoleAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        AppUser? user =
            await userManager.FindByIdAsync(
                userId.ToString("D"));

        if (user is null)
        {
            throw new DomainException(
                "User account was not found.");
        }

        bool alreadyInRole =
            await userManager.IsInRoleAsync(
                user,
                RoleNames.User);

        if (alreadyInRole)
        {
            return;
        }

        IdentityResult result =
            await userManager.AddToRoleAsync(
                user,
                RoleNames.User);

        if (!result.Succeeded)
        {
            throw new DomainException(
                "Default user role could not be assigned.");
        }
    }
}