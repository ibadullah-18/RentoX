using Microsoft.EntityFrameworkCore;
using RentoX.Application.Abstractions.Time;
using RentoX.Domain.Catalog.Categories;
using RentoX.Domain.Listings;
using RentoX.Domain.Listings.Enums;
using RentoX.Domain.Notifications;
using RentoX.Domain.Users;
using RentoX.Domain.Users.Enums;
using RentoX.Infrastructure.Identity;
using RentoX.Infrastructure.Listings;
using RentoX.Infrastructure.Persistence;
using RentoX.IntegrationTests.Auditing;

namespace RentoX.IntegrationTests.Moderation;

/// <summary>The reminder that a listing is about to end: once, only for listings on show.</summary>
public sealed class ListingExpiryReminderTests(AuditPostgresFixture database)
    : IClassFixture<AuditPostgresFixture>
{
    private static readonly DateTimeOffset Now =
        new(2026, 10, 15, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task OnlyListingsOnShowThatEndSoonGetOneReminderEach()
    {
        Guid owner = await AddUserAsync();
        Guid category = await AddCategoryAsync();

        Guid soon = await AddListingAsync(owner, category, "Tezliklə", Now.AddDays(-27)); // ends in 3 days minus: set below
        // Publish sets the end 30 days after the publish time: 28 days ago -> ends in 2 days.
        Guid ends = await AddListingAsync(owner, category, "İki günə", Now.AddDays(-28));
        Guid far = await AddListingAsync(owner, category, "Uzaqda", Now.AddDays(-1));
        Guid deactivated = await AddListingAsync(owner, category, "Dayandırılıb", Now.AddDays(-28), deactivate: true);

        int first = await Service().RemindAsync(TimeSpan.FromDays(3), 100);

        await using (RentoXDbContext db = database.CreateContext())
        {
            List<Notification> got = await db.Set<Notification>()
                .Where(n => n.UserId == owner && n.Type == NotificationType.ListingExpiring)
                .ToListAsync();

            // 27 days ago -> ends in 3 days exactly (inside the window), 28 -> in 2 days.
            Assert.Contains(got, n => n.RelatedEntityId == ends);
            Assert.Contains(got, n => n.RelatedEntityId == soon);
            Assert.DoesNotContain(got, n => n.RelatedEntityId == far);
            Assert.DoesNotContain(got, n => n.RelatedEntityId == deactivated);
            Assert.Equal(2, first);
            Assert.All(got, n => Assert.Equal($"/listings/{n.RelatedEntityId:D}", n.ActionUrl));
        }

        // A second run in the same stretch writes nothing new.
        Assert.Equal(0, await Service().RemindAsync(TimeSpan.FromDays(3), 100));
    }

    [Fact]
    public async Task TheBatchSizeLimitsAndTheRestFollowsNextRun()
    {
        Guid owner = await AddUserAsync();
        Guid category = await AddCategoryAsync();
        await AddListingAsync(owner, category, "Bir", Now.AddDays(-28));
        await AddListingAsync(owner, category, "İki", Now.AddDays(-28));
        await AddListingAsync(owner, category, "Üç", Now.AddDays(-28));

        int run1 = await Service().RemindAsync(TimeSpan.FromDays(3), 2);
        int run2 = await Service().RemindAsync(TimeSpan.FromDays(3), 2);
        int run3 = await Service().RemindAsync(TimeSpan.FromDays(3), 2);

        Assert.True(run1 >= 1 && run1 <= 2);
        Assert.True(run1 + run2 + run3 >= 3);
        await using RentoXDbContext db = database.CreateContext();
        Assert.Equal(3, await db.Set<Notification>()
            .CountAsync(n => n.UserId == owner && n.Type == NotificationType.ListingExpiring));
    }

    private ListingExpiryReminderService Service() => new(database.CreateContext(), new TestClock());

    private async Task<Guid> AddCategoryAsync()
    {
        await using RentoXDbContext db = database.CreateContext();
        Category category = Category.Create(null, $"exp-{Guid.NewGuid():N}", null, 0);
        category.AddTranslation(CategoryTranslation.Create(category.Id, PreferredLanguage.Azerbaijani, "Test"));
        db.Categories.Add(category);
        await db.SaveChangesAsync();
        return category.Id;
    }

    private async Task<Guid> AddListingAsync(
        Guid ownerId, Guid categoryId, string title, DateTimeOffset publishedAt, bool deactivate = false)
    {
        await using RentoXDbContext db = database.CreateContext();
        Listing listing = Listing.Create(
            ownerId, categoryId, title + " elan", "Təsvir mətni burada yazılıb.", 50m, "AZN", RentalPeriodUnit.Day);
        listing.MarkAsCreated(publishedAt, ownerId);
        listing.AddImage($"tests/exp-{Guid.NewGuid():N}.jpg", "image/jpeg", 100);
        listing.SubmitForReview();
        listing.Publish(publishedAt, TimeSpan.FromDays(30));
        if (deactivate)
        {
            listing.Deactivate();
        }

        db.Listings.Add(listing);
        await db.SaveChangesAsync();
        return listing.Id;
    }

    private async Task<Guid> AddUserAsync()
    {
        await using RentoXDbContext db = database.CreateContext();
        Guid id = Guid.NewGuid();
        string userName = $"exp-{id:N}";
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
        db.Set<UserProfile>().Add(UserProfile.Create(id, "Test exp", PreferredLanguage.Azerbaijani));
        await db.SaveChangesAsync();
        return id;
    }

    private sealed class TestClock : IClock
    {
        public DateTimeOffset UtcNow => Now;
    }
}
