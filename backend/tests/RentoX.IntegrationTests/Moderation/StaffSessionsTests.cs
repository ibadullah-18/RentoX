using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RentoX.Application.Abstractions.Authentication;
using RentoX.Application.Abstractions.Time;
using RentoX.Application.Admin;
using RentoX.Application.Authorization;
using RentoX.Domain.Auditing;
using RentoX.Domain.Authentication;
using RentoX.Domain.Common.Exceptions;
using RentoX.Domain.Users;
using RentoX.Domain.Users.Enums;
using RentoX.Infrastructure.Admin;
using RentoX.Infrastructure.Identity;
using RentoX.Infrastructure.Persistence;
using RentoX.IntegrationTests.Auditing;

namespace RentoX.IntegrationTests.Moderation;

/// <summary>A SuperAdmin ends the sessions of a staff member: counted, ended, recorded.</summary>
public sealed class StaffSessionsTests(AuditPostgresFixture database)
    : IClassFixture<AuditPostgresFixture>
{
    private static readonly DateTimeOffset Now =
        new(2026, 10, 15, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task EndingSessionsRevokesEveryActiveTokenAndWritesTheAudit()
    {
        Guid boss = await AddUserAsync(RoleNames.SuperAdmin);
        Guid staff = await AddUserAsync(RoleNames.Admin);
        await AddTokenAsync(staff, expired: false);
        await AddTokenAsync(staff, expired: false);
        await AddTokenAsync(staff, expired: true); // already over: not a session

        // The list shows them first.
        using (ServiceProvider list = Provider())
        {
            StaffMember before = (await Service(list, boss).ListAsync()).Single(m => m.UserId == staff);
            Assert.Equal(2, before.ActiveSessions);
            Assert.NotNull(before.LastActiveAtUtc);
        }

        int ended;
        using (ServiceProvider provider = Provider())
        {
            ended = await Service(provider, boss).EndSessionsAsync(staff);
        }

        Assert.Equal(2, ended);
        await using RentoXDbContext db = database.CreateContext();
        Assert.Empty(await db.RefreshTokens.Where(t => t.UserId == staff && t.RevokedAtUtc == null && t.ExpiresAtUtc > Now).ToListAsync());
        AuditLogEntry entry = await db.Set<AuditLogEntry>()
            .SingleAsync(a => a.Action == AuditAction.StaffSessionsEnded && a.TargetId == staff);
        Assert.Equal(boss, entry.ActorUserId);
        Assert.Equal(2, entry.CurrentValue);

        // A second time there is nothing left to end, and it is still recorded (with 0).
        using ServiceProvider again = Provider();
        Assert.Equal(0, await Service(again, boss).EndSessionsAsync(staff));
    }

    [Fact]
    public async Task YourOwnSessionsAndNonStaffAreRefused()
    {
        Guid boss = await AddUserAsync(RoleNames.SuperAdmin);
        Guid plain = await AddUserAsync(null);
        await AddTokenAsync(plain, expired: false);

        using ServiceProvider provider = Provider();
        await Assert.ThrowsAsync<DomainException>(() => Service(provider, boss).EndSessionsAsync(boss));
        await Assert.ThrowsAsync<DomainException>(() => Service(provider, boss).EndSessionsAsync(plain));
        await Assert.ThrowsAsync<DomainException>(() => Service(provider, boss).EndSessionsAsync(Guid.NewGuid()));

        await using RentoXDbContext db = database.CreateContext();
        Assert.Null((await db.RefreshTokens.SingleAsync(t => t.UserId == plain)).RevokedAtUtc);
    }

    private ServiceProvider Provider()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddScoped(_ => database.CreateContext());
        services.AddIdentityCore<AppUser>().AddRoles<AppRole>().AddEntityFrameworkStores<RentoXDbContext>();
        return services.BuildServiceProvider();
    }

    private static StaffManagementService Service(ServiceProvider provider, Guid actor)
    {
        IServiceScope scope = provider.CreateScope();
        return new StaffManagementService(
            scope.ServiceProvider.GetRequiredService<RentoXDbContext>(),
            scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>(),
            new TestClock(),
            new TestActor(actor));
    }

    private async Task AddTokenAsync(Guid userId, bool expired)
    {
        await using RentoXDbContext db = database.CreateContext();
        DateTimeOffset created = expired ? Now.AddDays(-60) : Now.AddMinutes(-5);
        db.RefreshTokens.Add(RefreshToken.Create(userId, Guid.NewGuid().ToString("N"), created, TimeSpan.FromDays(30)));
        await db.SaveChangesAsync();
    }

    private async Task<Guid> AddUserAsync(string? role)
    {
        await using RentoXDbContext db = database.CreateContext();
        Guid id = Guid.NewGuid();
        string userName = $"sess-{id:N}";
        db.Users.Add(new AppUser
        {
            Id = id,
            UserName = userName,
            NormalizedUserName = userName.ToUpperInvariant(),
            PhoneNumber = "+99450" + Random.Shared.Next(1000000, 9999999),
            RegisteredAtUtc = Now,
            SecurityStamp = Guid.NewGuid().ToString("N"),
            ConcurrencyStamp = Guid.NewGuid().ToString("N")
        });
        db.Set<UserProfile>().Add(UserProfile.Create(id, "Test " + id.ToString("N")[..6], PreferredLanguage.Azerbaijani));
        await db.SaveChangesAsync();

        if (role is not null)
        {
            AppRole? existing = await db.Roles.SingleOrDefaultAsync(r => r.Name == role);
            if (existing is null)
            {
                existing = new AppRole
                {
                    Id = Guid.NewGuid(),
                    Name = role,
                    NormalizedName = role.ToUpperInvariant(),
                    ConcurrencyStamp = Guid.NewGuid().ToString("N")
                };
                db.Roles.Add(existing);
            }

            db.Set<IdentityUserRole<Guid>>().Add(new IdentityUserRole<Guid> { UserId = id, RoleId = existing.Id });
            await db.SaveChangesAsync();
        }

        return id;
    }

    private sealed class TestClock : IClock
    {
        public DateTimeOffset UtcNow => Now;
    }

    private sealed class TestActor(Guid userId) : ICurrentUserContext
    {
        public Guid? UserId => userId;

        public bool IsAuthenticated => true;
    }
}
