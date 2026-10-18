using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using RentoX.Application.Abstractions.Authentication;
using RentoX.Application.Abstractions.Time;
using RentoX.Application.Gifts;
using RentoX.Application.Listings.Billing;
using RentoX.Application.Listings.Promotions;
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
using RentoX.Infrastructure.Gifts;
using RentoX.Infrastructure.Identity;
using RentoX.Infrastructure.Listings.Billing;
using RentoX.Infrastructure.Listings.Promotions;
using RentoX.Infrastructure.Notifications;
using RentoX.Infrastructure.Persistence;
using RentoX.Infrastructure.Stores;
using RentoX.Infrastructure.Wallets;
using RentoX.IntegrationTests.Auditing;

namespace RentoX.IntegrationTests.Moderation;

/// <summary>
/// RentoX pays for one person: the listing fee, VIP, bump and VIP store are free for them, with no wallet money moved
/// and nothing counted as income. The owner who switched it on is on every free promotion.
/// </summary>
public sealed class FreeServicesTests(AuditPostgresFixture database)
    : IClassFixture<AuditPostgresFixture>
{
    private static readonly DateTimeOffset Now = new(2026, 10, 14, 8, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task SwitchingItOnTellsThePersonIsRecordedAndOffMakesPricesNormalAgain()
    {
        Guid owner = await AddUserAsync();
        Guid giver = Guid.NewGuid();

        await using RentoXDbContext db = database.CreateContext();
        GiftService gifts = Gifts(db);

        Assert.Equal(0, await gifts.GetFreeServicesAsync(owner));
        await gifts.SetFreeServicesAsync(giver, owner, 15);
        Assert.Equal(15, await gifts.GetFreeServicesAsync(owner));

        // Switching it on again changes nothing and tells nobody twice.
        await gifts.SetFreeServicesAsync(giver, owner, 15);

        await using RentoXDbContext read = database.CreateContext();
        Assert.Equal(1, await read.Set<Notification>().CountAsync(n => n.UserId == owner && n.Type == NotificationType.GiftReceived));
        AuditLogEntry audit = await read.Set<AuditLogEntry>().AsNoTracking()
            .SingleAsync(a => a.Action == AuditAction.GiftGranted && a.TargetId == owner);
        Assert.Equal(giver, audit.ActorUserId);
        Assert.Equal(4, audit.CurrentValue);

        await gifts.SetFreeServicesAsync(giver, owner, 0);
        Assert.Equal(0, await gifts.GetFreeServicesAsync(owner));
        Assert.Equal(2, await read.Set<AuditLogEntry>().CountAsync(a => a.Action == AuditAction.GiftGranted && a.TargetId == owner));

        await Assert.ThrowsAsync<DomainException>(() => gifts.SetFreeServicesAsync(giver, Guid.NewGuid(), 15));
        await Assert.ThrowsAsync<DomainException>(() => gifts.SetFreeServicesAsync(Guid.Empty, owner, 15));
        await Assert.ThrowsAsync<DomainException>(() => gifts.SetFreeServicesAsync(giver, owner, 16));
    }

    [Fact]
    public async Task APersonWhoIsFreeNeverMeetsTheListingFeeLimit()
    {
        Guid free = await AddUserAsync();
        Guid normal = await AddUserAsync();
        Guid[] freeListings = new Guid[6];
        Guid[] normalListings = new Guid[6];
        for (int i = 0; i < 6; i++)
        {
            freeListings[i] = await AddListingAsync(free, Now.AddDays(20));
            normalListings[i] = await AddListingAsync(normal, Now.AddDays(20));
        }

        await using RentoXDbContext db = database.CreateContext();
        await Gifts(db).SetFreeServicesAsync(Guid.NewGuid(), free, 1);

        ListingActivationPricingService pricing = Pricing(db);
        ListingActivationPricingResult normalResult = await pricing.GetAsync(normal, normalListings[0]);
        ListingActivationPricingResult freeResult = await pricing.GetAsync(free, freeListings[0]);

        // The same number of listings: the one who pays is over the free limit, the other is not asked for money.
        Assert.True(normalResult.RequiresPayment);
        Assert.False(freeResult.RequiresPayment);
        Assert.Equal(0m, freeResult.ActivationFee);
    }

    [Fact]
    public async Task APromotionIsGivenWithoutAWalletTransactionAndCarriesTheGiver()
    {
        Guid owner = await AddUserAsync();
        Guid listingId = await AddListingAsync(owner, Now.AddDays(30));
        Guid giver = Guid.NewGuid();

        await using RentoXDbContext db = database.CreateContext();
        await Gifts(db).SetFreeServicesAsync(giver, owner, 15);

        ListingPromotionService promotions = Promotions(db);
        PurchaseListingPromotionResult vip = await promotions.PurchaseAsync(
            new PurchaseListingPromotionCommand(owner, listingId, (int)ListingPromotionType.Vip, "key-vip-" + Guid.NewGuid().ToString("N")));
        PurchaseListingPromotionResult bump = await promotions.PurchaseAsync(
            new PurchaseListingPromotionCommand(owner, listingId, (int)ListingPromotionType.Bump, "key-bump-" + Guid.NewGuid().ToString("N")));

        Assert.True(vip.IsGift);
        Assert.True(bump.IsGift);
        Assert.Equal(0m, vip.ChargedAmount);
        Assert.Null(vip.WalletTransactionId);
        Assert.Null(bump.WalletTransactionId);

        await using RentoXDbContext read = database.CreateContext();
        Assert.All(
            await read.ListingPromotions.AsNoTracking().Where(p => p.ListingId == listingId).ToListAsync(),
            p =>
            {
                Assert.True(p.IsGift);
                Assert.Equal(giver, p.GrantedByUserId);
            });

        // No money moved at all: not even a wallet needed to exist.
        Assert.False(await read.WalletTransactions.AnyAsync(t => t.RelatedEntityId == listingId));

        // A second VIP while one runs is still refused.
        await Assert.ThrowsAsync<DomainException>(() => promotions.PurchaseAsync(
            new PurchaseListingPromotionCommand(owner, listingId, (int)ListingPromotionType.Vip, "key-vip2-" + Guid.NewGuid().ToString("N"))));
    }

    [Fact]
    public async Task TheVipStoreIsFreeForThePersonAndShownAsAGift()
    {
        (Guid owner, _) = await AddStoreAsync();
        Guid giver = Guid.NewGuid();

        await using RentoXDbContext db = database.CreateContext();
        await Gifts(db).SetFreeServicesAsync(giver, owner, 15);
        StoreVipService vip = VipService(db);
        await vip.SetPlanAsync(new StoreVipPlanValues("VIP mağaza", 20m, 30, string.Empty, 72, true));

        StoreVipStatusResult before = (await vip.GetStatusAsync(owner))!;
        Assert.True(before.FreeServices);

        StoreVipPurchaseResult result = await vip.PurchaseAsync(owner, "key-store-" + Guid.NewGuid().ToString("N"));
        Assert.True(result.IsGift);
        Assert.Equal(0m, result.ChargedAmount);
        Assert.Equal(Now.AddDays(30), result.EndsAtUtc);

        StoreVipStatusResult after = (await vip.GetStatusAsync(owner))!;
        Assert.True(after.IsVip);
        Assert.True(after.IsGift);

        await using RentoXDbContext read = database.CreateContext();
        Assert.All(
            await read.Set<StoreVipPurchase>().AsNoTracking().Where(p => p.GrantedByUserId == giver).ToListAsync(),
            p => Assert.Null(p.WalletTransactionId));
    }

    [Fact]
    public async Task OnlyTheChosenServicesAreFree()
    {
        Guid owner = await AddUserAsync();
        Guid listingId = await AddListingAsync(owner, Now.AddDays(30));

        await using RentoXDbContext db = database.CreateContext();
        // Only the VIP is free (2): the bump still has to be paid, and this person has no money.
        await Gifts(db).SetFreeServicesAsync(Guid.NewGuid(), owner, 2);

        ListingPromotionService promotions = Promotions(db);
        PurchaseListingPromotionResult vip = await promotions.PurchaseAsync(
            new PurchaseListingPromotionCommand(owner, listingId, (int)ListingPromotionType.Vip, "key-only-vip-" + Guid.NewGuid().ToString("N")));
        Assert.True(vip.IsGift);

        await Assert.ThrowsAnyAsync<Exception>(() => promotions.PurchaseAsync(
            new PurchaseListingPromotionCommand(owner, listingId, (int)ListingPromotionType.Bump, "key-only-bump-" + Guid.NewGuid().ToString("N"))));
    }

    private static GiftService Gifts(RentoXDbContext db)
    {
        Clock clock = new();
        return new GiftService(
            db,
            clock,
            Tariffs(db, clock),
            VipService(db),
            new NotificationService(db, clock, new NoPublisher()));
    }

    private static TariffService Tariffs(RentoXDbContext db, Clock clock) =>
        new(db, new MemoryCache(new MemoryCacheOptions()), clock, new Actor(Guid.NewGuid()));

    private static ListingActivationPricingService Pricing(RentoXDbContext db)
    {
        Clock clock = new();
        return new ListingActivationPricingService(db, clock, Tariffs(db, clock));
    }

    private static ListingPromotionService Promotions(RentoXDbContext db)
    {
        Clock clock = new();
        return new ListingPromotionService(
            db, clock, new ListingPromotionPricingService(Tariffs(db, clock)), new WalletService(db, clock));
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
        string userName = $"free-{id:N}";
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
        db.Set<UserProfile>().Add(UserProfile.Create(id, "Pulsuz istifadəçi", PreferredLanguage.Azerbaijani));
        await db.SaveChangesAsync();
        return id;
    }

    private async Task<Guid> AddListingAsync(Guid owner, DateTimeOffset expiresAtUtc)
    {
        await using RentoXDbContext db = database.CreateContext();

        Category category = Category.Create(null, $"free-{Guid.NewGuid():N}", null, 0);
        category.AddTranslation(CategoryTranslation.Create(category.Id, PreferredLanguage.Azerbaijani, "Free"));
        db.Categories.Add(category);

        Listing listing = Listing.Create(owner, category.Id, "Pulsuz elan", "Təsvir mətni burada yazılıb.", 10m, "AZN", (RentalPeriodUnit)2);
        listing.MarkAsCreated(Now.AddDays(-5), owner);
        db.Listings.Add(listing);
        db.Entry(listing).Property(item => item.Status).CurrentValue = ListingStatus.Active;
        db.Entry(listing).Property(item => item.PublishedAtUtc).CurrentValue = Now.AddDays(-4);
        db.Entry(listing).Property(item => item.ExpiresAtUtc).CurrentValue = expiresAtUtc;

        await db.SaveChangesAsync();
        return listing.Id;
    }

    private async Task<(Guid Owner, Guid StoreId)> AddStoreAsync()
    {
        Guid owner = await AddUserAsync();
        await using RentoXDbContext db = database.CreateContext();
        StoreProfile store = StoreProfile.Create(
            owner, "Pulsuz mağaza", "free-" + owner.ToString("N")[..10], "A store made for the free services tests.",
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
