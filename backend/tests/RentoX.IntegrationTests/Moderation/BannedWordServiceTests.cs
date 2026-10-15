using Microsoft.EntityFrameworkCore;
using RentoX.Application.Abstractions.Authentication;
using RentoX.Application.Abstractions.Time;
using RentoX.Application.Admin;
using RentoX.Domain.Catalog.Categories;
using RentoX.Domain.Common.Exceptions;
using RentoX.Domain.Listings;
using RentoX.Domain.Listings.Enums;
using RentoX.Domain.Moderation;
using RentoX.Domain.Users;
using RentoX.Domain.Users.Enums;
using RentoX.Infrastructure.Admin;
using RentoX.Infrastructure.Identity;
using RentoX.Infrastructure.Persistence;
using RentoX.IntegrationTests.Auditing;

namespace RentoX.IntegrationTests.Moderation;

/// <summary>The list of banned words and which listings contain them.</summary>
public sealed class BannedWordServiceTests(AuditPostgresFixture database)
    : IClassFixture<AuditPostgresFixture>
{
    private static readonly DateTimeOffset Now =
        new(2026, 10, 15, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task WordsAreAddedListedAndRemovedWithoutDuplicates()
    {
        await ResetAsync();
        Guid admin = Guid.NewGuid();

        BannedWordItem first = await Service(admin).AddAsync("  DOLANDIRICI ");
        Assert.Equal("dolandırıcı", first.Word);
        await Service(admin).AddAsync("spam");

        await Assert.ThrowsAsync<DomainException>(() => Service(admin).AddAsync("Spam"));
        await Assert.ThrowsAsync<DomainException>(() => Service(admin).AddAsync("a"));

        Assert.Equal(["dolandırıcı", "spam"], (await Service(admin).ListAsync()).Select(w => w.Word).ToArray());

        Assert.True(await Service(admin).RemoveAsync(first.Id));
        Assert.False(await Service(admin).RemoveAsync(first.Id));
        Assert.Equal(["spam"], (await Service(admin).ListAsync()).Select(w => w.Word).ToArray());
    }

    [Fact]
    public async Task ListingsAreFlaggedByTheirTitleOrDescriptionAndCleanOnesAreLeftOut()
    {
        await ResetAsync();
        Guid admin = Guid.NewGuid();
        await Service(admin).AddAsync("spam");
        await Service(admin).AddAsync("dolandır*");

        Guid owner = await AddUserAsync();
        Guid category = await AddCategoryAsync();
        Guid inTitle = await AddListingAsync(owner, category, "SPAM velosiped", "Yaxşı vəziyyətdə, təmiz mətn.");
        Guid inText = await AddListingAsync(owner, category, "Normal başlıq", "Diqqət: dolandırıcılıq halları var.");
        Guid clean = await AddListingAsync(owner, category, "Musiqi alətləri", "Heç bir problem yoxdur burada.");

        var flags = await Service(admin).FindInListingsAsync([inTitle, inText, clean]);

        Assert.Equal(["spam"], flags[inTitle]);
        Assert.Equal(["dolandır*"], flags[inText]);
        Assert.False(flags.ContainsKey(clean));
    }

    [Fact]
    public async Task WithoutWordsOrIdsNothingIsFlagged()
    {
        await ResetAsync();
        Guid admin = Guid.NewGuid();

        Assert.Empty(await Service(admin).FindInListingsAsync([]));
        Assert.Empty(await Service(admin).FindInListingsAsync([Guid.NewGuid()]));
    }

    [Fact]
    public async Task OnlyASignedInAdminCanAddAWord()
    {
        await ResetAsync();
        var anonymous = new BannedWordService(database.CreateContext(), new TestClock(), new TestActor(null));

        await Assert.ThrowsAsync<DomainException>(() => anonymous.AddAsync("spam"));
    }

    private async Task ResetAsync()
    {
        await using RentoXDbContext db = database.CreateContext();
        await db.Set<BannedWord>().ExecuteDeleteAsync();
    }

    private BannedWordService Service(Guid actor) =>
        new(database.CreateContext(), new TestClock(), new TestActor(actor));

    private async Task<Guid> AddCategoryAsync()
    {
        await using RentoXDbContext db = database.CreateContext();
        Category category = Category.Create(null, $"bw-{Guid.NewGuid():N}", null, 0);
        category.AddTranslation(CategoryTranslation.Create(category.Id, PreferredLanguage.Azerbaijani, "Test"));
        db.Categories.Add(category);
        await db.SaveChangesAsync();
        return category.Id;
    }

    private async Task<Guid> AddListingAsync(Guid ownerId, Guid categoryId, string title, string description)
    {
        await using RentoXDbContext db = database.CreateContext();
        Listing listing = Listing.Create(ownerId, categoryId, title, description, 50m, "AZN", RentalPeriodUnit.Day);
        listing.MarkAsCreated(Now, ownerId);
        listing.AddImage($"tests/bw-{Guid.NewGuid():N}.jpg", "image/jpeg", 100);
        listing.SubmitForReview();
        db.Listings.Add(listing);
        await db.SaveChangesAsync();
        return listing.Id;
    }

    private async Task<Guid> AddUserAsync()
    {
        await using RentoXDbContext db = database.CreateContext();
        Guid id = Guid.NewGuid();
        string userName = $"bw-{id:N}";
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
        db.Set<UserProfile>().Add(UserProfile.Create(id, "Test bw", PreferredLanguage.Azerbaijani));
        await db.SaveChangesAsync();
        return id;
    }

    private sealed class TestClock : IClock
    {
        public DateTimeOffset UtcNow => Now;
    }

    private sealed class TestActor(Guid? userId) : ICurrentUserContext
    {
        public Guid? UserId => userId;

        public bool IsAuthenticated => userId is not null;
    }
}
