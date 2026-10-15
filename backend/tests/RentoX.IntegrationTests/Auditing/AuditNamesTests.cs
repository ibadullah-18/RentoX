using Microsoft.EntityFrameworkCore;
using RentoX.Application.Abstractions.Time;
using RentoX.Application.Auditing;
using RentoX.Domain.Auditing;
using RentoX.Domain.Catalog.Categories;
using RentoX.Domain.Listings;
using RentoX.Domain.Listings.Enums;
using RentoX.Domain.Users;
using RentoX.Domain.Users.Enums;
using RentoX.Infrastructure.Auditing;
using RentoX.Infrastructure.Identity;
using RentoX.Infrastructure.Persistence;

namespace RentoX.IntegrationTests.Auditing;

/// <summary>The history shows names next to the ids: who did it and what it was done to.</summary>
public sealed class AuditNamesTests(AuditPostgresFixture database)
    : IClassFixture<AuditPostgresFixture>
{
    private static readonly DateTimeOffset Now =
        new(2026, 10, 15, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task EntriesCarryTheNameOfThePersonAndOfTheThingTheyActedOn()
    {
        Guid admin = await AddUserAsync("Aysel Admin");
        Guid owner = await AddUserAsync("Murad Sahib");
        Guid listingId = await AddListingAsync(owner, "Mənim velosipedim");
        Guid vanished = Guid.NewGuid();

        await using (RentoXDbContext db = database.CreateContext())
        {
            Guid operation = Guid.NewGuid();
            db.Set<AuditLogEntry>().AddRange(
                AuditLogEntry.ForListingStatusChange(
                    operation, admin, listingId, ListingStatus.PendingReview, ListingStatus.Active, Now.AddHours(-1)),
                AuditLogEntry.ForUserStatusChange(
                    Guid.NewGuid(), admin, owner, UserStatus.Active, UserStatus.Blocked, Now.AddHours(-2)),
                AuditLogEntry.ForStoreStatusChange(
                    Guid.NewGuid(), admin, vanished, RentoX.Domain.Stores.Enums.StoreStatus.PendingReview,
                    RentoX.Domain.Stores.Enums.StoreStatus.Active, Now.AddHours(-3)));
            await db.SaveChangesAsync();
        }

        await using RentoXDbContext read = database.CreateContext();
        var page = await new AuditLogQueryService(read, new TestClock()).SearchAsync(
            new AuditLogSearchQuery(admin, null, null, null, null, null, 1, 20));

        AuditLogItemResult listing = page.Items.Single(i => i.Action == (int)AuditAction.ListingStatusChanged);
        Assert.Equal("Aysel Admin", listing.ActorName);
        Assert.Equal("Mənim velosipedim", listing.TargetName);

        AuditLogItemResult user = page.Items.Single(i => i.Action == (int)AuditAction.UserStatusChanged);
        Assert.Equal("Murad Sahib", user.TargetName);

        // A thing that no longer exists simply has no name: the page falls back to the id.
        AuditLogItemResult store = page.Items.Single(i => i.Action == (int)AuditAction.StoreStatusChanged);
        Assert.Null(store.TargetName);
        Assert.Equal("Aysel Admin", store.ActorName);
    }

    private async Task<Guid> AddUserAsync(string name)
    {
        await using RentoXDbContext db = database.CreateContext();
        Guid id = Guid.NewGuid();
        string userName = $"aud-{id:N}";
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
        db.Set<UserProfile>().Add(UserProfile.Create(id, name, PreferredLanguage.Azerbaijani));
        await db.SaveChangesAsync();
        return id;
    }

    private async Task<Guid> AddListingAsync(Guid ownerId, string title)
    {
        await using RentoXDbContext db = database.CreateContext();
        Category category = Category.Create(null, $"aud-{Guid.NewGuid():N}", null, 0);
        category.AddTranslation(CategoryTranslation.Create(category.Id, PreferredLanguage.Azerbaijani, "Test"));
        db.Categories.Add(category);
        Listing listing = Listing.Create(
            ownerId, category.Id, title, "Təsvir mətni burada yazılıb.", 50m, "AZN", RentalPeriodUnit.Day);
        listing.MarkAsCreated(Now, ownerId);
        listing.AddImage($"tests/aud-{Guid.NewGuid():N}.jpg", "image/jpeg", 100);
        listing.SubmitForReview();
        db.Listings.Add(listing);
        await db.SaveChangesAsync();
        return listing.Id;
    }

    private sealed class TestClock : IClock
    {
        public DateTimeOffset UtcNow => Now;
    }
}
