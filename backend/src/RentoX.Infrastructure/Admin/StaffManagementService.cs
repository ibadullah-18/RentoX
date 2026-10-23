using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RentoX.Application.Abstractions.Authentication;
using RentoX.Application.Abstractions.Time;
using RentoX.Application.Admin;
using RentoX.Application.Authorization;
using RentoX.Domain.Auditing;
using RentoX.Domain.Authentication;
using RentoX.Domain.Common.Exceptions;
using RentoX.Domain.Users;
using RentoX.Domain.Users.Enums;
using RentoX.Infrastructure.Identity;
using RentoX.Infrastructure.Persistence;

namespace RentoX.Infrastructure.Admin;

public sealed class StaffManagementService(
    RentoXDbContext dbContext,
    UserManager<AppUser> userManager,
    IClock clock,
    ICurrentUserContext currentUserContext)
    : IStaffManagementService
{
    public async Task<IReadOnlyList<StaffMember>> ListAsync(
        CancellationToken cancellationToken = default)
    {
        var rows = await (
            from userRole in dbContext.Set<IdentityUserRole<Guid>>()
            join role in dbContext.Roles on userRole.RoleId equals role.Id
            join user in dbContext.Users on userRole.UserId equals user.Id
            join profile in dbContext.Set<UserProfile>() on user.Id equals profile.Id
            where role.Name == RoleNames.Admin || role.Name == RoleNames.SuperAdmin
            select new { user.Id, profile.FullName, user.PhoneNumber, Role = role.Name })
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        // The sections chosen for each Admin (none chosen = all of them).
        var chosen = await dbContext.Set<IdentityUserClaim<Guid>>()
            .AsNoTracking()
            .Where(c => c.ClaimType == AdminSections.ClaimType)
            .Select(c => new { c.UserId, c.ClaimValue })
            .ToListAsync(cancellationToken);

        // Sessions: refresh tokens still usable; the newest one was made when the person last signed in or renewed.
        DateTimeOffset now = clock.UtcNow;
        Guid[] staffIds = rows.Select(r => r.Id).Distinct().ToArray();
        var sessions = await dbContext.RefreshTokens
            .AsNoTracking()
            .Where(t => staffIds.Contains(t.UserId))
            .GroupBy(t => t.UserId)
            .Select(g => new
            {
                UserId = g.Key,
                Active = g.Count(t => t.RevokedAtUtc == null && t.ExpiresAtUtc > now),
                Last = g.Max(t => t.CreatedAtUtc)
            })
            .ToListAsync(cancellationToken);

        // SuperAdmin first; someone who has both shows once, as SuperAdmin.
        return rows
            .GroupBy(r => r.Id)
            .Select(g =>
            {
                var first = g.First();
                string role = g.Any(r => r.Role == RoleNames.SuperAdmin) ? RoleNames.SuperAdmin : RoleNames.Admin;
                string[] sections = role == RoleNames.SuperAdmin
                    ? []
                    : chosen.Where(c => c.UserId == first.Id).Select(c => c.ClaimValue!).Order().ToArray();
                var session = sessions.FirstOrDefault(x => x.UserId == first.Id);
                return new StaffMember(
                    first.Id, first.FullName, first.PhoneNumber, role, sections,
                    session?.Active ?? 0, session?.Last);
            })
            .OrderBy(m => m.Role == RoleNames.SuperAdmin ? 0 : 1)
            .ThenBy(m => m.FullName)
            .ToArray();
    }

    public async Task<StaffMember> GrantAdminAsync(
        string phoneNumber,
        CancellationToken cancellationToken = default)
    {
        Guid actor = RequireActor();

        string phone = (phoneNumber ?? string.Empty).Trim();
        if (phone.Length == 0)
        {
            throw new DomainException("Phone number is required.");
        }

        AppUser? user = await userManager.Users
            .SingleOrDefaultAsync(u => u.PhoneNumber == phone, cancellationToken);

        UserProfile? profile = user is null
            ? null
            : await dbContext.Set<UserProfile>().SingleOrDefaultAsync(p => p.Id == user.Id, cancellationToken);

        if (user is null || profile is null || profile.Status != UserStatus.Active)
        {
            throw new DomainException("User was not found.");
        }

        if (await userManager.IsInRoleAsync(user, RoleNames.SuperAdmin) ||
            await userManager.IsInRoleAsync(user, RoleNames.Admin))
        {
            throw new DomainException("This person already has admin access.");
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        IdentityResult result = await userManager.AddToRoleAsync(user, RoleNames.Admin);
        if (!result.Succeeded)
        {
            throw new DomainException("The admin role could not be given.");
        }

        dbContext.Set<AuditLogEntry>().Add(
            AuditLogEntry.ForStaffRoleChange(Guid.NewGuid(), actor, user.Id, true, clock.UtcNow));
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return new StaffMember(user.Id, profile.FullName, user.PhoneNumber, RoleNames.Admin, []);
    }

    public async Task SetSectionsAsync(
        Guid userId,
        IReadOnlyList<string> sections,
        CancellationToken cancellationToken = default)
    {
        Guid actor = RequireActor();
        ArgumentNullException.ThrowIfNull(sections);

        string[] wanted = sections.Distinct().ToArray();
        if (wanted.Any(section => !AdminSections.All.Contains(section)))
        {
            throw new DomainException("A section is not valid.");
        }

        // Every ordinary section chosen is the same as nothing chosen: all of them. (The staff and branding sections
        // are different: they are only ever given on purpose, so they are not part of "all".)
        if (wanted.Length == AdminSections.Ordinary.Count && wanted.All(AdminSections.Ordinary.Contains))
        {
            wanted = [];
        }

        AppUser user = await userManager.FindByIdAsync(userId.ToString())
            ?? throw new DomainException("User was not found.");

        if (await userManager.IsInRoleAsync(user, RoleNames.SuperAdmin))
        {
            throw new DomainException("A SuperAdmin cannot be changed here.");
        }

        if (!await userManager.IsInRoleAsync(user, RoleNames.Admin))
        {
            throw new DomainException("This person is not an admin.");
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        List<System.Security.Claims.Claim> current = (await userManager.GetClaimsAsync(user))
            .Where(c => c.Type == AdminSections.ClaimType)
            .ToList();

        int before = AdminSections.ToMask(current.Select(c => c.Value));
        int after = AdminSections.ToMask(wanted);
        if (before == after)
        {
            return;
        }

        if (current.Count > 0)
        {
            await userManager.RemoveClaimsAsync(user, current);
        }

        if (wanted.Length > 0)
        {
            await userManager.AddClaimsAsync(
                user,
                wanted.Select(w => new System.Security.Claims.Claim(AdminSections.ClaimType, w)));
        }

        // A narrower set must not wait for the old token to run out: the
        // person's sessions end and the next sign-in carries the new limits.
        DateTimeOffset now = clock.UtcNow;
        List<RefreshToken> tokens = await dbContext.RefreshTokens
            .Where(t => t.UserId == userId && t.RevokedAtUtc == null)
            .ToListAsync(cancellationToken);
        foreach (RefreshToken token in tokens)
        {
            token.Revoke(now);
        }

        dbContext.Set<AuditLogEntry>().Add(
            AuditLogEntry.ForStaffSectionsChange(Guid.NewGuid(), actor, userId, before, after, now));
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task<int> EndSessionsAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        Guid actor = RequireActor();

        if (userId == actor)
        {
            throw new DomainException("Use Sign out to end your own sessions.");
        }

        AppUser user = await userManager.FindByIdAsync(userId.ToString())
            ?? throw new DomainException("User was not found.");

        if (!await userManager.IsInRoleAsync(user, RoleNames.SuperAdmin) &&
            !await userManager.IsInRoleAsync(user, RoleNames.Admin))
        {
            throw new DomainException("This person is not staff.");
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        DateTimeOffset now = clock.UtcNow;
        List<RefreshToken> tokens = await dbContext.RefreshTokens
            .Where(t => t.UserId == userId && t.RevokedAtUtc == null && t.ExpiresAtUtc > now)
            .ToListAsync(cancellationToken);
        foreach (RefreshToken token in tokens)
        {
            token.Revoke(now);
        }

        dbContext.Set<AuditLogEntry>().Add(
            AuditLogEntry.ForStaffSessionsEnded(Guid.NewGuid(), actor, userId, tokens.Count, now));
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return tokens.Count;
    }

    public async Task RevokeAdminAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        Guid actor = RequireActor();

        if (userId == actor)
        {
            throw new DomainException("You cannot take admin access away from yourself.");
        }

        AppUser user = await userManager.FindByIdAsync(userId.ToString())
            ?? throw new DomainException("User was not found.");

        if (await userManager.IsInRoleAsync(user, RoleNames.SuperAdmin))
        {
            throw new DomainException("A SuperAdmin cannot be changed here.");
        }

        if (!await userManager.IsInRoleAsync(user, RoleNames.Admin))
        {
            throw new DomainException("This person is not an admin.");
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        IdentityResult result = await userManager.RemoveFromRoleAsync(user, RoleNames.Admin);
        if (!result.Succeeded)
        {
            throw new DomainException("The admin role could not be taken away.");
        }

        // The access token lives a few minutes; ending the refresh tokens means
        // it cannot be renewed, so the access ends with it.
        DateTimeOffset now = clock.UtcNow;
        List<RefreshToken> tokens = await dbContext.RefreshTokens
            .Where(t => t.UserId == userId && t.RevokedAtUtc == null)
            .ToListAsync(cancellationToken);
        foreach (RefreshToken token in tokens)
        {
            token.Revoke(now);
        }

        dbContext.Set<AuditLogEntry>().Add(
            AuditLogEntry.ForStaffRoleChange(Guid.NewGuid(), actor, userId, false, now));
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    private Guid RequireActor()
    {
        Guid? id = currentUserContext.UserId;
        if (!currentUserContext.IsAuthenticated || !id.HasValue || id.Value == Guid.Empty)
        {
            throw new DomainException("An authenticated audit actor is required.");
        }

        return id.Value;
    }
}
