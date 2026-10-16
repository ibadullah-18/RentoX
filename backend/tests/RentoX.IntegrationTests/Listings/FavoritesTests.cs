using RentoX.Application.Abstractions.Time;
using RentoX.Domain.Catalog.Categories;
using RentoX.Domain.Listings;
using RentoX.Domain.Listings.Enums;
using RentoX.Domain.Users.Enums;
using RentoX.Infrastructure.Favorites;
using RentoX.Infrastructure.Identity;
using RentoX.Infrastructure.Persistence;
using RentoX.IntegrationTests.Auditing;

namespace RentoX.IntegrationTests.Listings;

/// <summary>Favourites: a person may favourite any live listing (their own too), and one query tells which of many are.</summary>
public sealed class FavoritesTests(AuditPostgresFixture database)
    : IClassFixture<AuditPostgresFixture>
{
    private static readonly DateTimeOffset Now = new(2026, 10, 14, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task OwnListingsCanBeFavouritedAndOneQueryTellsWhichOfManyAre()
    {
        await using RentoXDbContext db = database.CreateContext();
        (Guid owner, Guid viewer, Guid[] listings) = await SeedAsync(db);
        var service = new FavoriteService(db, new Clock());

        await service.AddAsync(viewer, listings[0]);
        await service.AddAsync(owner, listings[1]); // the owner's own listing

        IReadOnlySet<Guid> viewerSees = await service.GetFavoriteIdsAsync(viewer, listings);
        Assert.Equal([listings[0]], viewerSees);

        IReadOnlySet<Guid> ownerSees = await service.GetFavoriteIdsAsync(owner, listings);
        Assert.Equal([listings[1]], ownerSees);

        Assert.Empty(await service.GetFavoriteIdsAsync(viewer, []));
        Assert.Empty(await service.GetFavoriteIdsAsync(Guid.NewGuid(), listings));
    }

    private static async Task<(Guid Owner, Guid Viewer, Guid[] Listings)> SeedAsync(RentoXDbContext db)
    {
        AppUser User(string name) => new()
        {
            Id = Guid.NewGuid(),
            UserName = $"{name}-{Guid.NewGuid():N}",
            NormalizedUserName = $"{name}-{Guid.NewGuid():N}".ToUpperInvariant(),
            RegisteredAtUtc = Now,
            SecurityStamp = Guid.NewGuid().ToString("N"),
            ConcurrencyStamp = Guid.NewGuid().ToString("N")
        };

        AppUser owner = User("fav-owner");
        AppUser viewer = User("fav-viewer");
        db.Users.AddRange(owner, viewer);

        Category category = Category.Create(null, $"fav-{Guid.NewGuid():N}", null, 0);
        category.AddTranslation(CategoryTranslation.Create(category.Id, PreferredLanguage.Azerbaijani, "Fav"));
        db.Categories.Add(category);

        Guid[] ids = new Guid[3];
        for (int i = 0; i < ids.Length; i++)
        {
            Listing listing = Listing.Create(owner.Id, category.Id, $"Fav {i}", "Təsvir mətni burada yazılıb.", 10m, "AZN", (RentalPeriodUnit)2);
            listing.MarkAsCreated(Now.AddDays(-5), owner.Id);
            db.Listings.Add(listing);
            db.Entry(listing).Property(item => item.Status).CurrentValue = ListingStatus.Active;
            db.Entry(listing).Property(item => item.PublishedAtUtc).CurrentValue = Now.AddDays(-4);
            db.Entry(listing).Property(item => item.ExpiresAtUtc).CurrentValue = Now.AddDays(20);
            ids[i] = listing.Id;
        }

        await db.SaveChangesAsync();
        return (owner.Id, viewer.Id, ids);
    }

    private sealed class Clock : IClock
    {
        public DateTimeOffset UtcNow => Now;
    }
}
