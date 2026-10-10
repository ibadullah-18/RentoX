using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using RentoX.Application.Abstractions.Time;
using RentoX.Application.Common;
using RentoX.Application.Files;
using RentoX.Application.Moderation;
using RentoX.Domain.Authentication;
using RentoX.Domain.Catalog.Categories;
using RentoX.Domain.Common.Exceptions;
using RentoX.Domain.Favorites;
using RentoX.Domain.Listings;
using RentoX.Domain.Listings.Enums;
using RentoX.Domain.Moderation;
using RentoX.Domain.Notifications;
using RentoX.Domain.Stores;
using RentoX.Domain.Users;
using RentoX.Domain.Users.Enums;
using RentoX.Infrastructure.Accounts;
using RentoX.Infrastructure.Identity;
using RentoX.Infrastructure.Moderation;
using RentoX.Infrastructure.Persistence;
using RentoX.IntegrationTests.Auditing;

namespace RentoX.IntegrationTests.Moderation;

public sealed class ContentReportAndDeletionTests(
    AuditPostgresFixture database)
    : IClassFixture<AuditPostgresFixture>
{
    private static readonly DateTimeOffset Now =
        new(2026, 10, 10, 12, 0, 0, TimeSpan.Zero);

    // ---- reports ---------------------------------------------------------

    [Fact]
    public async Task AListingCanBeReportedOnceWhileTheReportIsOpen()
    {
        World world = await NewWorldAsync();

        ContentReportResult? first = await ReportAsync(
            world.Reporter, 1, world.ListingId, 2, "Scam price");

        Assert.NotNull(first);
        Assert.Equal(1, first.Status);

        DomainException duplicate =
            await Assert.ThrowsAsync<DomainException>(() =>
                ReportAsync(world.Reporter, 1, world.ListingId, 1, null));
        Assert.Contains("already reported", duplicate.Message);
    }

    [Fact]
    public async Task AStoreCanBeReported()
    {
        World world = await NewWorldAsync();

        ContentReportResult? report = await ReportAsync(
            world.Reporter, 2, world.StoreId, 3, null);

        Assert.NotNull(report);
        Assert.Equal(2, report.TargetType);
    }

    [Fact]
    public async Task YouCannotReportYourOwnContentOrAMissingTarget()
    {
        World world = await NewWorldAsync();

        await Assert.ThrowsAsync<DomainException>(() =>
            ReportAsync(world.Owner, 1, world.ListingId, 1, null));
        await Assert.ThrowsAsync<DomainException>(() =>
            ReportAsync(world.Owner, 2, world.StoreId, 1, null));

        Assert.Null(await ReportAsync(
            world.Reporter, 1, Guid.NewGuid(), 1, null));
        Assert.Null(await ReportAsync(
            world.Reporter, 2, Guid.NewGuid(), 1, null));
    }

    [Fact]
    public async Task ReasonsAreChecked()
    {
        World world = await NewWorldAsync();

        await Assert.ThrowsAsync<DomainException>(() =>
            ReportAsync(world.Reporter, 1, world.ListingId, 99, null));
        // "Other" needs an explanation.
        await Assert.ThrowsAsync<DomainException>(() =>
            ReportAsync(world.Reporter, 1, world.ListingId, 6, "  "));
        await Assert.ThrowsAsync<DomainException>(() =>
            ReportAsync(world.Reporter, 1, world.ListingId, 2,
                new string('x', 1001)));
        await Assert.ThrowsAsync<DomainException>(() =>
            ReportAsync(world.Reporter, 7, world.ListingId, 2, null));
    }

    [Fact]
    public async Task AModeratorSeesReportsWithTitlesAndClosesThem()
    {
        World world = await NewWorldAsync();
        ContentReportResult? created = await ReportAsync(
            world.Reporter, 1, world.ListingId, 4, "Wrong photos");
        Assert.NotNull(created);

        await using RentoXDbContext db = database.CreateContext();
        ContentReportService service = new(db, new FixedClock());

        PagedResult<ContentReportSummaryResult> list =
            await service.GetForAdminAsync(1, 1, 1, 50);
        ContentReportSummaryResult row =
            Assert.Single(list.Items, item => item.Id == created.Id);
        Assert.Equal("Reported listing", row.TargetTitle);

        ContentReportDetailsResult? details =
            await service.GetDetailsAsync(created.Id);
        Assert.NotNull(details);
        Assert.Equal(world.Owner, details.TargetOwnerId);
        Assert.Equal("Wrong photos", details.Details);
        Assert.Equal(1, details.OpenReportsOnTarget);

        // Completing needs a note.
        await Assert.ThrowsAsync<DomainException>(() =>
            service.ReviewAsync(world.Reporter, created.Id, 3, null));

        ContentReportResult? done = await service.ReviewAsync(
            world.Reporter, created.Id, 3, "Removed the photos.");
        Assert.NotNull(done);
        Assert.Equal(3, done.Status);

        // A finished report is final, and a new one may be filed.
        await Assert.ThrowsAsync<DomainException>(() =>
            service.ReviewAsync(world.Reporter, created.Id, 4, "No"));
        Assert.NotNull(await ReportAsync(
            world.Reporter, 1, world.ListingId, 1, null));

        Assert.Null(await service.ReviewAsync(
            world.Reporter, Guid.NewGuid(), 3, "x"));
    }

    // ---- account deletion -----------------------------------------------

    [Fact]
    public async Task DeletingTheAccountClosesEverythingTheUserHad()
    {
        World world = await NewWorldAsync();
        FakeStorage storage = new();

        await using (RentoXDbContext db = database.CreateContext())
        {
            // The owner follows and likes things, has a phone and a session.
            db.StoreFollowers.Add(
                StoreFollower.Create(world.Owner, world.OtherStoreId, Now));
            db.Favorites.Add(
                Favorite.Create(world.Owner, world.OtherListingId, Now));
            db.Favorites.Add(
                Favorite.Create(world.Reporter, world.ListingId, Now));
            db.StoreFollowers.Add(
                StoreFollower.Create(world.Reporter, world.StoreId, Now));
            db.Set<PushDevice>().Add(PushDevice.Register(
                world.Owner, PushPlatform.Android, "dev", "tok", Now));
            db.RefreshTokens.Add(RefreshToken.Create(
                world.Owner, "hash-" + Guid.NewGuid(), Now,
                TimeSpan.FromDays(30)));
            await db.SaveChangesAsync();
        }

        await using (RentoXDbContext db = database.CreateContext())
        {
            AccountDeletionService service = new(
                db, storage, new FixedClock(),
                NullLogger<AccountDeletionService>.Instance);

            Assert.True(await service.DeleteAsync(world.Owner));
            Assert.False(await service.DeleteAsync(world.Owner)); // already
        }

        await using RentoXDbContext check = database.CreateContext();

        // Sign-in closed, phone number released.
        AppUser user = await check.Users.SingleAsync(u => u.Id == world.Owner);
        Assert.Null(user.PhoneNumber);
        Assert.StartsWith("deleted-", user.UserName, StringComparison.Ordinal);
        Assert.True(user.LockoutEnd > DateTimeOffset.UtcNow.AddYears(100));

        // Personal details erased, conversations can show a neutral name.
        UserProfile profile =
            await check.UserProfiles.SingleAsync(p => p.Id == world.Owner);
        Assert.Equal(UserProfile.DeletedUserName, profile.FullName);
        Assert.Equal(UserStatus.Deleted, profile.Status);
        Assert.Null(profile.ProfileImageKey);

        // Listings and store are gone from view.
        Assert.All(
            await check.Listings
                .Where(l => l.OwnerId == world.Owner)
                .ToListAsync(),
            l => Assert.Equal(ListingStatus.Deleted, l.Status));
        Assert.Empty(await check.Set<StoreProfile>()
            .Where(s => s.OwnerId == world.Owner)
            .ToListAsync()); // hidden by the soft-delete filter
        StoreProfile hidden = await check.Set<StoreProfile>()
            .IgnoreQueryFilters()
            .SingleAsync(s => s.OwnerId == world.Owner);
        Assert.Equal(
            RentoX.Domain.Stores.Enums.StoreStatus.Deleted,
            hidden.Status);

        // Followers, likes, devices and sessions are dropped.
        Assert.Empty(await check.StoreFollowers
            .Where(f => f.UserId == world.Owner ||
                        f.StoreId == world.StoreId)
            .ToListAsync());
        Assert.Empty(await check.Favorites
            .Where(f => f.UserId == world.Owner)
            .ToListAsync());
        Assert.DoesNotContain(
            await check.Set<PushDevice>()
                .Where(d => d.UserId == world.Owner)
                .ToListAsync(),
            d => d.IsActive);
        Assert.DoesNotContain(
            await check.RefreshTokens
                .Where(t => t.UserId == world.Owner)
                .ToListAsync(),
            t => t.RevokedAtUtc == null);

        // Store logo, store cover and profile photo were removed from storage.
        Assert.Equal(3, storage.Deleted.Count);

        // Other people's data is untouched.
        Assert.Single(await check.Favorites
            .Where(f => f.UserId == world.Reporter)
            .ToListAsync());
    }

    [Fact]
    public async Task StaffAccountsCannotBeDeletedFromTheApp()
    {
        World world = await NewWorldAsync();

        await using (RentoXDbContext db = database.CreateContext())
        {
            AppRole role = await db.Roles
                .SingleOrDefaultAsync(r => r.Name == "Admin")
                ?? new AppRole { Name = "Admin", NormalizedName = "ADMIN" };

            if (db.Entry(role).State == EntityState.Detached)
            {
                db.Roles.Add(role);
            }

            db.Set<IdentityUserRole<Guid>>().Add(new IdentityUserRole<Guid>
            {
                UserId = world.Reporter,
                RoleId = role.Id
            });
            await db.SaveChangesAsync();
        }

        await using RentoXDbContext check = database.CreateContext();

        await Assert.ThrowsAsync<DomainException>(() =>
            new AccountDeletionService(
                check, new FakeStorage(), new FixedClock(),
                NullLogger<AccountDeletionService>.Instance)
            .DeleteAsync(world.Reporter));
    }

    // ---- helpers ---------------------------------------------------------

    private sealed record World(
        Guid Owner,
        Guid Reporter,
        Guid ListingId,
        Guid OtherListingId,
        Guid StoreId,
        Guid OtherStoreId);

    private async Task<ContentReportResult?> ReportAsync(
        Guid reporter,
        int targetType,
        Guid targetId,
        int reason,
        string? details)
    {
        await using RentoXDbContext db = database.CreateContext();

        return await new ContentReportService(db, new FixedClock())
            .CreateAsync(reporter, targetType, targetId, reason, details);
    }

    private async Task<World> NewWorldAsync()
    {
        await using RentoXDbContext db = database.CreateContext();

        Guid owner = NewUser(db, "owner", "+9945" + Random.Shared.Next(10000000, 99999999));
        Guid reporter = NewUser(db, "reporter", "+9945" + Random.Shared.Next(10000000, 99999999));
        Guid otherOwner = NewUser(db, "other", "+9945" + Random.Shared.Next(10000000, 99999999));

        Category category = Category.Create(
            null, $"cat-{Guid.NewGuid():N}", null, 0);
        category.AddTranslation(
            CategoryTranslation.Create(
                category.Id, PreferredLanguage.Azerbaijani, "Kateqoriya"));
        db.Categories.Add(category);

        Guid listing = NewListing(db, owner, category.Id, "Reported listing");
        NewListing(db, owner, category.Id, "Second listing");
        Guid otherListing = NewListing(
            db, otherOwner, category.Id, "Someone else's listing");

        Guid store = NewStore(db, owner, "Owner store");
        Guid otherStore = NewStore(db, otherOwner, "Other store");

        await db.SaveChangesAsync();

        return new World(
            owner, reporter, listing, otherListing, store, otherStore);
    }

    private static Guid NewUser(
        RentoXDbContext db,
        string prefix,
        string phone)
    {
        Guid id = Guid.NewGuid();
        string userName = $"{prefix}-{id:N}";

        db.Users.Add(new AppUser
        {
            Id = id,
            UserName = userName,
            NormalizedUserName = userName.ToUpperInvariant(),
            PhoneNumber = phone,
            RegisteredAtUtc = Now,
            SecurityStamp = Guid.NewGuid().ToString("N"),
            ConcurrencyStamp = Guid.NewGuid().ToString("N")
        });

        UserProfile profile = UserProfile.Create(
            id, "Real Name", PreferredLanguage.Azerbaijani);
        profile.MarkAsCreated(Now, id);
        profile.SetProfileImage("profile-images/" + id.ToString("N") + ".png");
        db.UserProfiles.Add(profile);

        return id;
    }

    private static Guid NewListing(
        RentoXDbContext db,
        Guid ownerId,
        Guid categoryId,
        string title)
    {
        Listing listing = Listing.Create(
            ownerId, categoryId, title,
            "Təsvir mətni burada yazılıb.", 50m, "AZN",
            (RentalPeriodUnit)2);

        listing.MarkAsCreated(Now.AddDays(-5), ownerId);
        db.Listings.Add(listing);

        db.Entry(listing).Property(item => item.Status)
            .CurrentValue = ListingStatus.Active;
        db.Entry(listing).Property(item => item.PublishedAtUtc)
            .CurrentValue = Now.AddDays(-4);
        db.Entry(listing).Property(item => item.ExpiresAtUtc)
            .CurrentValue = Now.AddDays(20);

        return listing.Id;
    }

    private static Guid NewStore(
        RentoXDbContext db,
        Guid ownerId,
        string name)
    {
        StoreProfile store = StoreProfile.Create(
            ownerId, name, "slug-" + Guid.NewGuid().ToString("N"),
            "Mağaza haqqında kifayət qədər uzun təsvir.",
            "+994500000000", null, null, null, null, null, null);

        store.MarkAsCreated(Now.AddDays(-10), ownerId);
        store.SetLogo("store-logos/" + Guid.NewGuid().ToString("N") + ".png");
        store.SetCover("store-covers/" + Guid.NewGuid().ToString("N") + ".png");
        db.StoreProfiles.Add(store);

        db.Entry(store).Property(item => item.Status).CurrentValue =
            RentoX.Domain.Stores.Enums.StoreStatus.Active;

        return store.Id;
    }

    private sealed class FixedClock : IClock
    {
        public DateTimeOffset UtcNow => Now;
    }

    private sealed class FakeStorage : IFileStorage
    {
        public List<string> Deleted { get; } = [];

        public Task<StoredFileResult> SaveAsync(
            FileStorageArea area,
            Stream content,
            string contentType,
            string extension,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<Stream?> OpenReadAsync(
            string storageKey,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<Stream?>(null);

        public Task DeleteAsync(
            string storageKey,
            CancellationToken cancellationToken = default)
        {
            Deleted.Add(storageKey);
            return Task.CompletedTask;
        }
    }
}
