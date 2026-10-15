using Microsoft.EntityFrameworkCore;
using RentoX.Application.Abstractions.Time;
using RentoX.Domain.Catalog.Categories;
using RentoX.Domain.Listings;
using RentoX.Domain.Listings.Enums;
using RentoX.Domain.Users.Enums;
using RentoX.Infrastructure.Analytics;
using RentoX.Infrastructure.Identity;
using RentoX.Infrastructure.Persistence;
using RentoX.IntegrationTests.Auditing;

namespace RentoX.IntegrationTests.Moderation;

/// <summary>The day-by-day numbers on the admin front page.</summary>
public sealed class DailyAnalyticsTests(AuditPostgresFixture database)
    : IClassFixture<AuditPostgresFixture>
{
    private static readonly DateTimeOffset Now =
        new(2026, 10, 15, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task EveryDayIsThereOldestFirstAndNewPeopleAreCountedOnTheirDay()
    {
        await AddUserAsync(Now.AddHours(-1));
        await AddUserAsync(Now.AddHours(-2));
        await AddUserAsync(Now.AddDays(-3));
        await AddUserAsync(Now.AddDays(-40)); // outside a 30 day window

        await using RentoXDbContext db = database.CreateContext();
        var points = await new AdminAnalyticsService(db, new TestClock()).GetDailyAsync(30);

        Assert.Equal(30, points.Count);
        Assert.Equal(new DateOnly(2026, 10, 15), points[^1].Date);
        Assert.Equal(new DateOnly(2026, 9, 16), points[0].Date);
        Assert.Equal(2, points[^1].NewUsers);
        Assert.Equal(1, points.Single(p => p.Date == new DateOnly(2026, 10, 12)).NewUsers);
        Assert.Equal(3, points.Sum(p => p.NewUsers));
    }

    [Fact]
    public async Task TheNumberOfDaysIsKeptBetweenSevenAndNinety()
    {
        await using RentoXDbContext db = database.CreateContext();
        var service = new AdminAnalyticsService(db, new TestClock());

        Assert.Equal(7, (await service.GetDailyAsync(1)).Count);
        Assert.Equal(90, (await service.GetDailyAsync(1000)).Count);
    }

    [Fact]
    public async Task CategoryCountsTakeOnlyActiveListingsAndPutTheBiggestFirst()
    {
        Guid owner = await AddUserAsync(Now.AddDays(-60)); // outside the daily window
        Guid big = await AddCategoryAsync();
        Guid small = await AddCategoryAsync();
        Guid empty = await AddCategoryAsync();

        await AddListingAsync(owner, big, publish: true);
        await AddListingAsync(owner, big, publish: true);
        await AddListingAsync(owner, big, publish: false); // waits for review: not on show
        await AddListingAsync(owner, small, publish: true);

        await using RentoXDbContext db = database.CreateContext();
        var counts = await new AdminAnalyticsService(db, new TestClock()).GetCategoryCountsAsync();

        Assert.Equal(2, counts.Single(c => c.CategoryId == big).ActiveListings);
        Assert.Equal(1, counts.Single(c => c.CategoryId == small).ActiveListings);
        Assert.DoesNotContain(counts, c => c.CategoryId == empty);
        Assert.True(counts.ToList().FindIndex(c => c.CategoryId == big) < counts.ToList().FindIndex(c => c.CategoryId == small));
    }

    private async Task<Guid> AddCategoryAsync()
    {
        await using RentoXDbContext db = database.CreateContext();
        Category category = Category.Create(null, $"cnt-{Guid.NewGuid():N}", null, 0);
        category.AddTranslation(CategoryTranslation.Create(category.Id, PreferredLanguage.Azerbaijani, "Test"));
        db.Categories.Add(category);
        await db.SaveChangesAsync();
        return category.Id;
    }

    private async Task AddListingAsync(Guid ownerId, Guid categoryId, bool publish)
    {
        await using RentoXDbContext db = database.CreateContext();
        Listing listing = Listing.Create(
            ownerId, categoryId, "Say testi", "Təsvir mətni burada yazılıb.", 50m, "AZN", RentalPeriodUnit.Day);
        listing.MarkAsCreated(Now, ownerId);
        listing.AddImage($"tests/cnt-{Guid.NewGuid():N}.jpg", "image/jpeg", 100);
        listing.SubmitForReview();
        if (publish)
        {
            listing.Publish(Now, TimeSpan.FromDays(30));
        }

        db.Listings.Add(listing);
        await db.SaveChangesAsync();
    }

    private async Task<Guid> AddUserAsync(DateTimeOffset registeredAt)
    {
        await using RentoXDbContext db = database.CreateContext();
        Guid id = Guid.NewGuid();
        string userName = $"daily-{id:N}";
        db.Users.Add(new AppUser
        {
            Id = id,
            UserName = userName,
            NormalizedUserName = userName.ToUpperInvariant(),
            PhoneNumber = "+99450" + Random.Shared.Next(1000000, 9999999),
            RegisteredAtUtc = registeredAt,
            SecurityStamp = Guid.NewGuid().ToString("N"),
            ConcurrencyStamp = Guid.NewGuid().ToString("N")
        });
        await db.SaveChangesAsync();
        return id;
    }

    private sealed class TestClock : IClock
    {
        public DateTimeOffset UtcNow => Now;
    }
}
