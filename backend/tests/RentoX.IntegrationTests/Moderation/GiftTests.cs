using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using RentoX.Application.Abstractions.Authentication;
using RentoX.Application.Abstractions.Time;
using RentoX.Application.Finance;
using RentoX.Application.Gifts;
using RentoX.Application.Notifications;
using RentoX.Application.Stores;
using RentoX.Domain.Auditing;
using RentoX.Domain.Catalog.Categories;
using RentoX.Domain.Common.Exceptions;
using RentoX.Domain.Listings;
using RentoX.Domain.Listings.Enums;
using RentoX.Domain.Listings.Promotions;
using RentoX.Domain.Listings.Promotions.Enums;
using RentoX.Domain.Notifications;
using RentoX.Domain.Stores;
using RentoX.Domain.Users;
using RentoX.Domain.Users.Enums;
using RentoX.Infrastructure.Finance;
using RentoX.Infrastructure.Gifts;
using RentoX.Infrastructure.Identity;
using RentoX.Infrastructure.Listings.Billing;
using RentoX.Infrastructure.Notifications;
using RentoX.Infrastructure.Persistence;
using RentoX.Infrastructure.Stores;
using RentoX.Infrastructure.Wallets;
using RentoX.IntegrationTests.Auditing;

namespace RentoX.IntegrationTests.Moderation;

/// <summary>
/// Free VIP and bump from RentoX: nothing is taken from the wallet, nothing becomes income, the person is told, and
/// the owner who gave it is in the audit log.
/// </summary>
public sealed class GiftTests(AuditPostgresFixture database)
    : IClassFixture<AuditPostgresFixture>
{
    private static readonly DateTimeOffset Now = new(2026, 10, 14, 8, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task AFreeVipTakesNoMoneyAddsNoIncomeAndTheOwnerIsToldAndTheGiverIsRecorded()
    {
        (Guid owner, Guid listingId) = await AddListingAsync(Now.AddDays(30));
        Guid giver = Guid.NewGuid();
        decimal incomeBefore = (await Summary()).Periods.Single(p => p.Name == "all").Income;

        await using RentoXDbContext db = database.CreateContext();
        GiftResult result = await Service(db).GiftListingPromotionAsync(giver, listingId, (int)ListingPromotionType.Vip, null);

        // The default length is the one of the paid VIP (7 days when the prices were never changed).
        Assert.Equal(owner, result.RecipientUserId);
        Assert.Equal(Now.AddDays(7), result.EndsAtUtc);

        await using RentoXDbContext read = database.CreateContext();
        ListingPromotion promotion = await read.ListingPromotions.AsNoTracking().SingleAsync(p => p.ListingId == listingId);
        Assert.True(promotion.IsGift);
        Assert.Equal(giver, promotion.GrantedByUserId);
        Assert.Equal(0m, promotion.ChargedAmount);
        Assert.Null(promotion.WalletTransactionId);

        // No wallet was even made for the person: nothing was charged.
        Assert.False(await read.Wallets.AnyAsync(w => w.UserId == owner));
        Assert.Equal(incomeBefore, (await Summary()).Periods.Single(p => p.Name == "all").Income);

        Notification note = await read.Set<Notification>().AsNoTracking()
            .SingleAsync(n => n.UserId == owner && n.Type == NotificationType.GiftReceived);
        Assert.Contains("pulsuz VIP", note.Body, StringComparison.Ordinal);

        AuditLogEntry audit = await read.Set<AuditLogEntry>().AsNoTracking()
            .SingleAsync(a => a.Action == AuditAction.GiftGranted && a.TargetId == owner);
        Assert.Equal(giver, audit.ActorUserId);
        Assert.Equal(2, audit.CurrentValue);
        Assert.Equal(promotion.Id, audit.RelatedEntityId);
    }

    [Fact]
    public async Task AGiftVipIsRefusedWhenAlreadyVipAndEndsWithTheListing()
    {
        (_, Guid listingId) = await AddListingAsync(Now.AddDays(3));
        Guid giver = Guid.NewGuid();

        await using RentoXDbContext db = database.CreateContext();
        GiftResult first = await Service(db).GiftListingPromotionAsync(giver, listingId, (int)ListingPromotionType.Vip, 30);

        // 30 days asked, the listing has 3 left: the VIP ends with the listing.
        Assert.Equal(Now.AddDays(3), first.EndsAtUtc);

        await Assert.ThrowsAsync<DomainException>(() =>
            Service(db).GiftListingPromotionAsync(giver, listingId, (int)ListingPromotionType.Vip, 5));
    }

    [Fact]
    public async Task AGiftBumpHasNoEndAndWrongRequestsAreRefused()
    {
        (_, Guid listingId) = await AddListingAsync(Now.AddDays(30));
        (_, Guid endedId) = await AddListingAsync(Now.AddDays(-1));
        Guid giver = Guid.NewGuid();

        await using RentoXDbContext db = database.CreateContext();
        GiftResult bump = await Service(db).GiftListingPromotionAsync(giver, listingId, (int)ListingPromotionType.Bump, null);
        Assert.Null(bump.EndsAtUtc);

        await Assert.ThrowsAsync<DomainException>(() => Service(db).GiftListingPromotionAsync(giver, endedId, (int)ListingPromotionType.Bump, null));
        await Assert.ThrowsAsync<DomainException>(() => Service(db).GiftListingPromotionAsync(giver, Guid.NewGuid(), (int)ListingPromotionType.Bump, null));
        await Assert.ThrowsAsync<DomainException>(() => Service(db).GiftListingPromotionAsync(giver, listingId, 9, null));
        await Assert.ThrowsAsync<DomainException>(() => Service(db).GiftListingPromotionAsync(giver, listingId, (int)ListingPromotionType.Vip, 0));
        await Assert.ThrowsAsync<DomainException>(() => Service(db).GiftListingPromotionAsync(Guid.Empty, listingId, (int)ListingPromotionType.Bump, null));
    }

    [Fact]
    public async Task AFreeVipStoreIsShownToTheOwnerAsAGiftAndAddsAfterThePeriodRunning()
    {
        (Guid owner, Guid storeId) = await AddStoreAsync();
        Guid giver = Guid.NewGuid();

        await using RentoXDbContext db = database.CreateContext();
        GiftResult first = await Service(db).GiftStoreVipAsync(giver, storeId, 10);
        Assert.Equal(Now, first.StartsAtUtc);
        Assert.Equal(Now.AddDays(10), first.EndsAtUtc);

        GiftResult second = await Service(db).GiftStoreVipAsync(giver, storeId, 5);
        Assert.Equal(Now.AddDays(10), second.StartsAtUtc);
        Assert.Equal(Now.AddDays(15), second.EndsAtUtc);

        StoreVipStatusResult status = (await VipService(db).GetStatusAsync(owner))!;
        Assert.True(status.IsVip);
        Assert.True(status.IsGift);
        Assert.Equal(Now.AddDays(15), status.VipUntilUtc);

        IReadOnlyList<PublicStoreSummaryResult> home = await VipService(db).GetVipStoresAsync(50);
        Assert.Contains(home, s => s.StoreId == storeId);

        await using RentoXDbContext read = database.CreateContext();
        Assert.All(
            await read.Set<StoreVipPurchase>().AsNoTracking().Where(p => p.StoreId == storeId).ToListAsync(),
            p =>
            {
                Assert.True(p.IsGift);
                Assert.Null(p.WalletTransactionId);
                Assert.Equal(0m, p.ChargedAmount);
            });
        Assert.False(await read.Wallets.AnyAsync(w => w.UserId == owner));

        // A store that is not live cannot get it.
        await Assert.ThrowsAsync<DomainException>(() => Service(db).GiftStoreVipAsync(giver, Guid.NewGuid(), 5));
    }

    [Fact]
    public async Task TheMonthlyReportCountsTheGiftsButNotAsIncome()
    {
        (_, Guid listingId) = await AddListingAsync(Now.AddDays(30));
        (_, Guid storeId) = await AddStoreAsync();
        Guid giver = Guid.NewGuid();

        await using RentoXDbContext db = database.CreateContext();
        await Service(db).GiftListingPromotionAsync(giver, listingId, (int)ListingPromotionType.Bump, null);
        await Service(db).GiftStoreVipAsync(giver, storeId, 3);

        await using RentoXDbContext read = database.CreateContext();
        MonthlyReport report = await new FinanceService(read, new Clock()).GetMonthlyReportAsync(new DateOnly(2026, 10, 1));
        Assert.True(report.Gifts.Bumps >= 1);
        Assert.True(report.Gifts.StoreVip >= 1);
    }

    private async Task<FinanceSummary> Summary()
    {
        await using RentoXDbContext db = database.CreateContext();
        return await new FinanceService(db, new Clock()).GetSummaryAsync();
    }

    private static GiftService Service(RentoXDbContext db)
    {
        Clock clock = new();
        return new GiftService(
            db,
            clock,
            new TariffService(db, new MemoryCache(new MemoryCacheOptions()), clock, new Actor(Guid.NewGuid())),
            VipService(db),
            new NotificationService(db, clock, new NoPublisher()));
    }

    private static StoreVipService VipService(RentoXDbContext db)
    {
        Clock clock = new();
        return new StoreVipService(db, clock, new WalletService(db, clock), new Actor(Guid.NewGuid()));
    }

    private async Task<Guid> AddUserAsync()
    {
        await using RentoXDbContext db = database.CreateContext();
        Guid id = Guid.NewGuid();
        string userName = $"gift-{id:N}";
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
        db.Set<UserProfile>().Add(UserProfile.Create(id, "Hədiyyə alan", PreferredLanguage.Azerbaijani));
        await db.SaveChangesAsync();
        return id;
    }

    private async Task<(Guid Owner, Guid ListingId)> AddListingAsync(DateTimeOffset expiresAtUtc)
    {
        Guid owner = await AddUserAsync();
        await using RentoXDbContext db = database.CreateContext();

        Category category = Category.Create(null, $"gift-{Guid.NewGuid():N}", null, 0);
        category.AddTranslation(CategoryTranslation.Create(category.Id, PreferredLanguage.Azerbaijani, "Gift"));
        db.Categories.Add(category);

        Listing listing = Listing.Create(owner, category.Id, "Hədiyyə elanı", "Təsvir mətni burada yazılıb.", 10m, "AZN", (RentalPeriodUnit)2);
        listing.MarkAsCreated(Now.AddDays(-5), owner);
        db.Listings.Add(listing);
        db.Entry(listing).Property(item => item.Status).CurrentValue = ListingStatus.Active;
        db.Entry(listing).Property(item => item.PublishedAtUtc).CurrentValue = Now.AddDays(-4);
        db.Entry(listing).Property(item => item.ExpiresAtUtc).CurrentValue = expiresAtUtc;

        await db.SaveChangesAsync();
        return (owner, listing.Id);
    }

    private async Task<(Guid Owner, Guid StoreId)> AddStoreAsync()
    {
        Guid owner = await AddUserAsync();
        await using RentoXDbContext db = database.CreateContext();
        StoreProfile store = StoreProfile.Create(
            owner, "Hədiyyə mağazası", "gift-" + owner.ToString("N")[..10], "A store made for the gift tests.",
            "+994500000000", null, null, null, null, null, null);
        store.SetLogo("stores/logo.webp");
        store.SubmitForReview();
        store.Approve();
        db.StoreProfiles.Add(store);
        await db.SaveChangesAsync();
        return (owner, store.Id);
    }

    private sealed class Clock : IClock
    {
        public DateTimeOffset UtcNow => Now;
    }

    private sealed class Actor(Guid userId) : ICurrentUserContext
    {
        public Guid? UserId => userId;

        public bool IsAuthenticated => true;
    }

    private sealed class NoPublisher : INotificationEventPublisher
    {
        public Task NotificationCreatedAsync(NotificationResult notification, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }
}
