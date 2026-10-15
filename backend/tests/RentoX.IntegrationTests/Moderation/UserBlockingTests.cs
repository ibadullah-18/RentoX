using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using RentoX.Application.Abstractions.Authentication;
using RentoX.Application.Abstractions.Time;
using RentoX.Application.Authorization;
using RentoX.Domain.Auditing;
using RentoX.Domain.Authentication;
using RentoX.Domain.Catalog.Categories;
using RentoX.Domain.Common.Exceptions;
using RentoX.Domain.Listings;
using RentoX.Domain.Listings.Enums;
using RentoX.Domain.Stores;
using RentoX.Domain.Stores.Enums;
using RentoX.Domain.Users;
using RentoX.Domain.Users.Enums;
using RentoX.Infrastructure.Admin;
using RentoX.Infrastructure.Identity;
using RentoX.Infrastructure.Persistence;
using RentoX.IntegrationTests.Auditing;

namespace RentoX.IntegrationTests.Moderation;

/// <summary>Blocking an account: what stops, what comes down, and who cannot be blocked.</summary>
public sealed class UserBlockingTests(AuditPostgresFixture database)
    : IClassFixture<AuditPostgresFixture>
{
    private static readonly DateTimeOffset Now =
        new(2026, 10, 14, 12, 0, 0, TimeSpan.Zero);

    private sealed record Seed(Guid AdminId, Guid UserId, Guid ListingId, Guid StoreId, Guid TokenId);

    [Fact]
    public async Task BlockingEndsSessionsTakesDownListingsAndSuspendsTheStore()
    {
        Seed seed = await SeedAsync(makeAdminStaff: true);

        await Service(seed.AdminId).BlockAsync(seed.UserId, "Fraud reports");

        await using RentoXDbContext db = database.CreateContext();
        UserProfile profile = await db.Set<UserProfile>().SingleAsync(p => p.Id == seed.UserId);
        Assert.Equal(UserStatus.Blocked, profile.Status);
        Assert.Equal("Fraud reports", profile.StatusReason);

        Assert.NotNull((await db.RefreshTokens.SingleAsync(t => t.Id == seed.TokenId)).RevokedAtUtc);

        Listing listing = await db.Listings.SingleAsync(l => l.Id == seed.ListingId);
        Assert.Equal(ListingStatus.Rejected, listing.Status);
        Assert.Contains("Fraud reports", listing.RejectionReason);

        Assert.Equal(StoreStatus.Suspended, (await db.StoreProfiles.SingleAsync(s => s.Id == seed.StoreId)).Status);

        List<AuditLogEntry> audit = await db.Set<AuditLogEntry>()
            .Where(a => a.ActorUserId == seed.AdminId &&
                (a.TargetId == seed.UserId || a.TargetId == seed.ListingId || a.TargetId == seed.StoreId))
            .ToListAsync();
        Assert.Contains(audit, a => a.Action == AuditAction.UserStatusChanged && a.CurrentValue == (int)UserStatus.Blocked);
        Assert.Contains(audit, a => a.Action == AuditAction.ListingStatusChanged);
        Assert.Contains(audit, a => a.Action == AuditAction.StoreStatusChanged);
    }

    [Fact]
    public async Task TheStatusGuardStopsABlockedAccountAndLetsItBackInAfterUnblocking()
    {
        Seed seed = await SeedAsync(makeAdminStaff: true);
        MemoryCache cache = new(new MemoryCacheOptions());

        await using (RentoXDbContext before = database.CreateContext())
        {
            Assert.True(await new UserStatusGuard(before, cache).IsAllowedAsync(seed.UserId));
        }

        await Service(seed.AdminId, cache).BlockAsync(seed.UserId, "Spam");

        await using (RentoXDbContext blocked = database.CreateContext())
        {
            Assert.False(await new UserStatusGuard(blocked, cache).IsAllowedAsync(seed.UserId));
        }

        await Service(seed.AdminId, cache).UnblockAsync(seed.UserId);

        await using (RentoXDbContext after = database.CreateContext())
        {
            Assert.True(await new UserStatusGuard(after, cache).IsAllowedAsync(seed.UserId));
            // Unblocking brings the person back, not the listings that came down.
            Assert.Equal(
                ListingStatus.Rejected,
                (await after.Listings.SingleAsync(l => l.Id == seed.ListingId)).Status);
        }
    }

    [Fact]
    public async Task StaffAndTheCallerThemselvesCannotBeBlocked()
    {
        Seed seed = await SeedAsync(makeAdminStaff: true);

        await Assert.ThrowsAsync<DomainException>(() => Service(seed.AdminId).BlockAsync(seed.AdminId, "x"));

        // The person being blocked is staff too.
        await using (RentoXDbContext db = database.CreateContext())
        {
            await MakeStaffAsync(db, seed.UserId);
        }

        await Assert.ThrowsAsync<DomainException>(() => Service(seed.AdminId).BlockAsync(seed.UserId, "x"));

        await using RentoXDbContext check = database.CreateContext();
        Assert.Equal(
            UserStatus.Active,
            (await check.Set<UserProfile>().SingleAsync(p => p.Id == seed.UserId)).Status);
    }

    [Fact]
    public async Task ABlockWithoutAReasonChangesNothing()
    {
        Seed seed = await SeedAsync(makeAdminStaff: true);

        await Assert.ThrowsAsync<DomainException>(() => Service(seed.AdminId).BlockAsync(seed.UserId, "  "));

        await using RentoXDbContext db = database.CreateContext();
        Assert.Equal(ListingStatus.Active, (await db.Listings.SingleAsync(l => l.Id == seed.ListingId)).Status);
        Assert.Null((await db.RefreshTokens.SingleAsync(t => t.Id == seed.TokenId)).RevokedAtUtc);
    }

    private UserModerationService Service(Guid actor, IMemoryCache? cache = null)
    {
        RentoXDbContext db = database.CreateContext();
        return new UserModerationService(
            db,
            new TestClock(),
            new TestActor(actor),
            new UserStatusGuard(db, cache ?? new MemoryCache(new MemoryCacheOptions())));
    }

    private async Task<Seed> SeedAsync(bool makeAdminStaff)
    {
        await using RentoXDbContext db = database.CreateContext();

        Guid adminId = await AddUserAsync(db, "admin");
        Guid userId = await AddUserAsync(db, "person");

        Category category = Category.Create(null, $"blk-{Guid.NewGuid():N}", null, 0);
        category.AddTranslation(CategoryTranslation.Create(category.Id, PreferredLanguage.Azerbaijani, "Test"));
        db.Categories.Add(category);

        Listing listing = Listing.Create(
            userId, category.Id, "Bloklama testi", "Təsvir mətni burada yazılıb.", 50m, "AZN", RentalPeriodUnit.Day);
        listing.MarkAsCreated(Now, userId);
        listing.AddImage($"tests/block-{Guid.NewGuid():N}.jpg", "image/jpeg", 100);
        listing.SubmitForReview();
        listing.Publish(Now, TimeSpan.FromDays(30));
        db.Listings.Add(listing);

        StoreProfile store = StoreProfile.Create(
            userId, "Block test store", "block-test-" + Guid.NewGuid().ToString("N"),
            "A store created for the blocking tests.", "+994500000000", null, null, null, null, null, null);
        store.SetLogo("stores/logo.webp");
        store.SubmitForReview();
        store.Approve();
        db.StoreProfiles.Add(store);

        RefreshToken token = RefreshToken.Create(userId, Guid.NewGuid().ToString("N"), Now, TimeSpan.FromDays(30));
        db.RefreshTokens.Add(token);

        await db.SaveChangesAsync();

        if (makeAdminStaff)
        {
            await MakeStaffAsync(db, adminId);
        }

        return new Seed(adminId, userId, listing.Id, store.Id, token.Id);
    }

    private static async Task<Guid> AddUserAsync(RentoXDbContext db, string kind)
    {
        Guid id = Guid.NewGuid();
        string userName = $"block-{kind}-{id:N}";
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
        db.Set<UserProfile>().Add(UserProfile.Create(id, "Test " + kind, PreferredLanguage.Azerbaijani));
        await db.SaveChangesAsync();
        return id;
    }

    private static async Task MakeStaffAsync(RentoXDbContext db, Guid userId)
    {
        AppRole? role = await db.Roles.SingleOrDefaultAsync(r => r.Name == RoleNames.Admin);
        if (role is null)
        {
            role = new AppRole
            {
                Id = Guid.NewGuid(),
                Name = RoleNames.Admin,
                NormalizedName = RoleNames.Admin.ToUpperInvariant(),
                ConcurrencyStamp = Guid.NewGuid().ToString("N")
            };
            db.Roles.Add(role);
        }

        db.Set<IdentityUserRole<Guid>>().Add(new IdentityUserRole<Guid> { UserId = userId, RoleId = role.Id });
        await db.SaveChangesAsync();
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
