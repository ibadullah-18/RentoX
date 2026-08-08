using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RentoX.Application.Authorization;
using RentoX.Domain.Common.Exceptions;
using System.Diagnostics.Contracts;

namespace RentoX.Infrastructure.Identity;

public sealed class IdentitySeeder(
    RoleManager<AppRole> roleManager,
    UserManager<AppUser> userManager,
    IdentitySeedOptions options)
    : IIdentitySeeder
{
    public async Task SeedAsync(
        CancellationToken cancellationToken = default)
    {
        foreach (string roleName in RoleNames.All)
        {
            bool roleExists =
                await roleManager.RoleExistsAsync(roleName);

            if (!roleExists)
            {
                IdentityResult roleResult =
                    await roleManager.CreateAsync(
                        new AppRole
                        {
                            Name = roleName
                        });

                if (!roleResult.Succeeded)
                {
                    throw new DomainException(
                        $"Role could not be created: {roleName}");
                }
            }
        }

        List<AppUser> users =
            await userManager.Users.ToListAsync(
                cancellationToken);

        foreach (AppUser user in users)
        {
            IList<string> roles =
                await userManager.GetRolesAsync(user);

            if (roles.Count == 0)
            {
                await AddRoleAsync(
                    user,
                    RoleNames.User);
            }
        }

        if (string.IsNullOrWhiteSpace(
                options.SuperAdminPhoneNumber))
        {
            return;
        }

        AppUser? superAdmin =
            await userManager.Users
                .SingleOrDefaultAsync(
                    user =>
                        user.PhoneNumber ==
                        options.SuperAdminPhoneNumber,
                    cancellationToken);

        if (superAdmin is null)
        {
            return;
        }

        await AddRoleAsync(
            superAdmin,
            RoleNames.SuperAdmin);
    }

    private async Task AddRoleAsync(
        AppUser user,
        string roleName)
    {
        bool alreadyInRole =
            await userManager.IsInRoleAsync(
                user,
                roleName);

        if (alreadyInRole)
        {
            return;
        }

        IdentityResult result =
            await userManager.AddToRoleAsync(
                user,
                roleName);

        if (!result.Succeeded)
        {
            throw new DomainException(
                $"Role could not be assigned: {roleName}");
        }
    }
}

