using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RentoX.Application.Abstractions.Time;
using RentoX.Application.Authentication;
using RentoX.Domain.Common.Exceptions;
using RentoX.Domain.Users;
using RentoX.Domain.Users.Enums;
using RentoX.Infrastructure.Identity;
using RentoX.Infrastructure.Persistence;


namespace RentoX.Infrastructure.Authentication;

public sealed class RegistrationAccountService(
    RentoXDbContext dbContext,
    UserManager<AppUser> userManager,
    IClock clock)
    : IRegistrationAccountService
{
    public async Task<Guid> CreateAsync(
        string phoneNumber,
        string fullName,
        PreferredLanguage preferredLanguage,
        CancellationToken cancellationToken = default)
    {
        await using Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction?
            transaction =
                dbContext.Database.CurrentTransaction is null
                    ? await dbContext.Database.BeginTransactionAsync(
                        cancellationToken)
                    : null;

        bool phoneExists = await userManager.Users.AnyAsync(
            user => user.PhoneNumber == phoneNumber,
            cancellationToken);

        if (phoneExists)
        {
            throw new DomainException(
                "This phone number is already registered.");
        }

        Guid userId = Guid.NewGuid();

        AppUser appUser = new()
        {
            Id = userId,
            UserName = phoneNumber,
            PhoneNumber = phoneNumber,
            PhoneNumberConfirmed = true,
            RegisteredAtUtc = clock.UtcNow
        };

        IdentityResult identityResult =
            await userManager.CreateAsync(appUser);

        if (!identityResult.Succeeded)
        {
            string errorMessage = string.Join(
                " ",
                identityResult.Errors.Select(
                    error => error.Description));

            throw new DomainException(errorMessage);
        }

        UserProfile profile = UserProfile.Create(
            userId,
            fullName,
            preferredLanguage);

        dbContext.UserProfiles.Add(profile);

        await dbContext.SaveChangesAsync(
            cancellationToken);

        if (transaction is not null)
        {
            await transaction.CommitAsync(cancellationToken);
        }

        return userId;
    }
}
