using Microsoft.EntityFrameworkCore;
using RentoX.Application.Abstractions.Authentication;
using RentoX.Application.Abstractions.Time;
using RentoX.Application.Notifications;
using RentoX.Application.Stores;
using RentoX.Application.Wallets;
using RentoX.Domain.Common.Exceptions;
using RentoX.Domain.Notifications;
using RentoX.Domain.Stores;
using RentoX.Domain.Users;
using RentoX.Domain.Users.Enums;
using RentoX.Domain.Wallets.Enums;
using RentoX.Infrastructure.Identity;
using RentoX.Infrastructure.Notifications;
using RentoX.Infrastructure.Persistence;
using RentoX.Infrastructure.Stores;
using RentoX.Infrastructure.Wallets;
using RentoX.IntegrationTests.Auditing;

namespace RentoX.IntegrationTests.Moderation;

/// <summary>The VIP store product: the plan an admin sets, paying from the wallet, the home page list, the offers.</summary>
public sealed class StoreVipTests(AuditPostgresFixture database)
    : IClassFixture<AuditPostgresFixture>
{
    private static readonly DateTimeOffset Now = new(2026, 10, 14, 8, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task ThePlanIsOffUntilAnAdminSetsItAndEveryChangeIsAddedAsAVersion()
    {
        await ResetPlansAsync();
        Guid admin = Guid.NewGuid();

        await using (RentoXDbContext db = database.CreateContext())
        {
            Assert.False((await Service(db, admin).GetPlanAsync()).IsEnabled);
        }

        StoreVipPlanValues plan = new("Qızıl mağaza", 25m, 14, "Ana səhifədə görünün", 24, true);
        await using (RentoXDbContext db = database.CreateContext())
        {
            await Service(db, admin).SetPlanAsync(plan);
        }

        await using RentoXDbContext read = database.CreateContext();
        Assert.Equal(plan, await Service(read, admin).GetPlanAsync());
        Assert.Equal(1, await read.Set<StoreVipPlan>().CountAsync());

        await Assert.ThrowsAsync<DomainException>(() =>
            Service(read, admin).SetPlanAsync(plan with { DurationDays = 0 }));
        await Assert.ThrowsAsync<DomainException>(() =>
            Service(read, admin).SetPlanAsync(plan with { Price = -1m }));
    }

    [Fact]
    public async Task PayingFromTheWalletMakesTheStoreVipForTheSetTimeAndBuyingAgainAddsAfterIt()
    {
        await ResetPlansAsync();
        (Guid owner, Guid storeId) = await AddActiveStoreAsync();
        await SetPlanAsync(new StoreVipPlanValues("VIP mağaza", 10m, 30, "", 48, true));
        await TopUpAsync(owner, 35m);

        await using RentoXDbContext db = database.CreateContext();
        StoreVipService service = Service(db, owner);

        StoreVipPurchaseResult first = await service.PurchaseAsync(owner, "vip-1-" + owner);
        Assert.Equal(Now, first.StartsAtUtc);
        Assert.Equal(Now.AddDays(30), first.EndsAtUtc);
        Assert.Equal(25m, first.RemainingBalance);

        // The same key answers with the same purchase and takes nothing more.
        StoreVipPurchaseResult repeated = await service.PurchaseAsync(owner, "vip-1-" + owner);
        Assert.Equal(first.EndsAtUtc, repeated.EndsAtUtc);
        Assert.Equal(25m, repeated.RemainingBalance);

        StoreVipStatusResult status = (await service.GetStatusAsync(owner))!;
        Assert.True(status.IsVip);
        Assert.Equal(first.EndsAtUtc, status.VipUntilUtc);

        // A second purchase starts where the first ends.
        StoreVipPurchaseResult second = await service.PurchaseAsync(owner, "vip-2-" + owner);
        Assert.Equal(first.EndsAtUtc, second.StartsAtUtc);
        Assert.Equal(first.EndsAtUtc.AddDays(30), second.EndsAtUtc);
        Assert.Equal(15m, second.RemainingBalance);

        IReadOnlyList<PublicStoreSummaryResult> home = await service.GetVipStoresAsync(50);
        Assert.Contains(home, s => s.StoreId == storeId);
    }

    [Fact]
    public async Task WithoutMoneyOrWhileNotOnSaleNothingIsBoughtAndNoStoreMeansNoOffer()
    {
        await ResetPlansAsync();
        (Guid owner, Guid storeId) = await AddActiveStoreAsync();
        await SetPlanAsync(new StoreVipPlanValues("VIP mağaza", 10m, 30, "", 48, false));
        await TopUpAsync(owner, 50m);

        await using RentoXDbContext db = database.CreateContext();
        StoreVipService service = Service(db, owner);

        // Switched off: cannot be bought.
        await Assert.ThrowsAsync<DomainException>(() => service.PurchaseAsync(owner, "off-" + owner));

        await SetPlanAsync(new StoreVipPlanValues("VIP mağaza", 90m, 30, "", 48, true));
        await using RentoXDbContext poor = database.CreateContext();
        // 90 is more than the 50 in the wallet.
        await Assert.ThrowsAsync<DomainException>(() => Service(poor, owner).PurchaseAsync(owner, "poor-" + owner));
        Assert.DoesNotContain(await Service(poor, owner).GetVipStoresAsync(50), s => s.StoreId == storeId);

        Assert.Null(await Service(poor, owner).GetStatusAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task OffersGoToTheOwnersWhoAreNotVipAndNotAgainWithinTheInterval()
    {
        await ResetPlansAsync();
        (Guid plain, _) = await AddActiveStoreAsync();
        (Guid vip, _) = await AddActiveStoreAsync();
        await SetPlanAsync(new StoreVipPlanValues("VIP mağaza", 5m, 30, "Təklif mətni", 24, true));
        await TopUpAsync(vip, 5m);

        await using (RentoXDbContext db = database.CreateContext())
        {
            await Service(db, vip).PurchaseAsync(vip, "offer-vip-" + vip);
        }

        await using RentoXDbContext sender = database.CreateContext();
        StoreVipOffersResult first = await Service(sender, plain).SendOffersAsync();
        Assert.True(first.Sent >= 1);

        Notification note = await sender.Set<Notification>()
            .AsNoTracking()
            .SingleAsync(n => n.UserId == plain && n.Type == NotificationType.StoreVipOffer);
        Assert.Equal("Təklif mətni", note.Body);
        Assert.False(await sender.Set<Notification>().AnyAsync(n => n.UserId == vip && n.Type == NotificationType.StoreVipOffer));

        // Straight away again: the owner was just offered, so nothing is sent to them.
        await using RentoXDbContext again = database.CreateContext();
        await Service(again, plain).SendOffersAsync();
        Assert.Equal(1, await again.Set<Notification>().CountAsync(n => n.UserId == plain && n.Type == NotificationType.StoreVipOffer));
    }

    private static StoreVipService Service(RentoXDbContext db, Guid actor)
    {
        Clock clock = new();
        return new StoreVipService(
            db, clock, new WalletService(db, clock), new Actor(actor),
            new NotificationService(db, clock, new NoPublisher()));
    }

    private async Task SetPlanAsync(StoreVipPlanValues plan)
    {
        await using RentoXDbContext db = database.CreateContext();
        await Service(db, Guid.NewGuid()).SetPlanAsync(plan);
    }

    private async Task TopUpAsync(Guid userId, decimal amount)
    {
        await using RentoXDbContext db = database.CreateContext();
        await new WalletService(db, new Clock()).CreditAsync(new CreditWalletCommand(
            userId, amount, WalletTransactionType.Bonus, "test", null, "topup-" + userId + "-" + Guid.NewGuid()));
    }

    private async Task ResetPlansAsync()
    {
        await using RentoXDbContext db = database.CreateContext();
        await db.Database.ExecuteSqlRawAsync("DELETE FROM stores.store_vip_plans");
    }

    private async Task<(Guid Owner, Guid StoreId)> AddActiveStoreAsync()
    {
        await using RentoXDbContext db = database.CreateContext();
        Guid id = Guid.NewGuid();
        string userName = $"vip-{id:N}";
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
        db.Set<UserProfile>().Add(UserProfile.Create(id, "Sahib", PreferredLanguage.Azerbaijani));
        StoreProfile store = StoreProfile.Create(
            id, "VIP sınaq mağazası", "vip-" + id.ToString("N")[..10], "A store made for the VIP tests.",
            "+994500000000", null, null, null, null, null, null);
        store.SetLogo("stores/logo.webp");
        store.SubmitForReview();
        store.Approve();
        db.StoreProfiles.Add(store);
        await db.SaveChangesAsync();
        return (id, store.Id);
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
