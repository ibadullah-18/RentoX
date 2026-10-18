using System.Data;
using Microsoft.EntityFrameworkCore;
using RentoX.Application.Abstractions.Authentication;
using RentoX.Application.Abstractions.Time;
using RentoX.Application.Notifications;
using RentoX.Application.Stores;
using RentoX.Application.Wallets;
using RentoX.Domain.Common.Exceptions;
using RentoX.Domain.Listings.Enums;
using RentoX.Domain.Notifications;
using RentoX.Domain.Stores;
using RentoX.Domain.Stores.Enums;
using RentoX.Domain.Users.Enums;
using RentoX.Domain.Wallets;
using RentoX.Domain.Wallets.Enums;
using RentoX.Infrastructure.Persistence;

namespace RentoX.Infrastructure.Stores;

public sealed class StoreVipService(
    RentoXDbContext dbContext,
    IClock clock,
    IWalletService walletService,
    ICurrentUserContext currentUserContext,
    INotificationService? notifications = null)
    : IStoreVipService
{
    /// <summary>The plan the site starts with: off, so nothing is sold until an admin sets it up.</summary>
    public static readonly StoreVipPlanValues Starting = new("VIP mağaza", 20m, 30, string.Empty, 72, false);

    public async Task<StoreVipPlanValues> GetPlanAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.Set<StoreVipPlan>()
            .AsNoTracking()
            .OrderByDescending(p => p.CreatedAtUtc)
            .Select(p => new StoreVipPlanValues(
                p.Name, p.Price, p.DurationDays, p.OfferMessage, p.OfferIntervalHours, p.IsEnabled))
            .FirstOrDefaultAsync(cancellationToken)
            ?? Starting;
    }

    public async Task<StoreVipPlanValues> SetPlanAsync(
        StoreVipPlanValues values, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(values);

        Guid? actor = currentUserContext.UserId;
        if (!currentUserContext.IsAuthenticated || !actor.HasValue || actor.Value == Guid.Empty)
        {
            throw new DomainException("An authenticated admin is required.");
        }

        StoreVipPlan plan = StoreVipPlan.Create(
            values.Name, values.Price, values.DurationDays, values.OfferMessage, values.OfferIntervalHours,
            values.IsEnabled, actor.Value, clock.UtcNow);

        dbContext.Set<StoreVipPlan>().Add(plan);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new StoreVipPlanValues(
            plan.Name, plan.Price, plan.DurationDays, plan.OfferMessage, plan.OfferIntervalHours, plan.IsEnabled);
    }

    public async Task<StoreVipStatusResult?> GetStatusAsync(Guid ownerId, CancellationToken cancellationToken = default)
    {
        var store = await dbContext.Set<StoreProfile>()
            .AsNoTracking()
            .Where(s => s.OwnerId == ownerId && s.Status == StoreStatus.Active)
            .Select(s => new { s.Id })
            .SingleOrDefaultAsync(cancellationToken);
        if (store is null)
        {
            return null;
        }

        DateTimeOffset now = clock.UtcNow;
        DateTimeOffset? until = await VipUntilAsync(store.Id, now, cancellationToken);

        // Was the period that ends last a gift from RentoX? (The owner is told so on the panel.)
        bool gift = until.HasValue && await dbContext.Set<StoreVipPurchase>()
            .AsNoTracking()
            .AnyAsync(p => p.StoreId == store.Id && p.EndsAtUtc == until.Value && p.GrantedByUserId != null, cancellationToken);

        bool free = await dbContext.UserProfiles
            .AsNoTracking()
            .AnyAsync(p => p.Id == ownerId && (p.FreeServices & FreeServiceKinds.StoreVip) != 0, cancellationToken);

        return new StoreVipStatusResult(
            await GetPlanAsync(cancellationToken), until.HasValue, until, "AZN", gift, free);
    }

    public async Task<StoreVipPurchaseResult> PurchaseAsync(
        Guid ownerId, string idempotencyKey, CancellationToken cancellationToken = default)
    {
        if (ownerId == Guid.Empty)
        {
            throw new DomainException("The owner id is required.");
        }

        string key = (idempotencyKey ?? string.Empty).Trim();
        if (key.Length == 0 || key.Length > WalletTransaction.MaximumIdempotencyKeyLength)
        {
            throw new DomainException("The idempotency key is required.");
        }

        StoreVipPlanValues plan = await GetPlanAsync(cancellationToken);
        if (!plan.IsEnabled)
        {
            throw new DomainException("The VIP store place is not on sale now.");
        }

        await using var databaseTransaction = await dbContext.Database.BeginTransactionAsync(
            IsolationLevel.Serializable, cancellationToken);

        StoreProfile store = await dbContext.Set<StoreProfile>()
            .AsNoTracking()
            .SingleOrDefaultAsync(s => s.OwnerId == ownerId && s.Status == StoreStatus.Active, cancellationToken)
            ?? throw new DomainException("Only an active store can become VIP.");

        // The same key again answers with the first purchase instead of charging twice.
        WalletTransaction? existing = await dbContext.WalletTransactions
            .AsNoTracking()
            .SingleOrDefaultAsync(t => t.IdempotencyKey == key, cancellationToken);
        if (existing is not null)
        {
            StoreVipPurchase? done = await dbContext.Set<StoreVipPurchase>()
                .AsNoTracking()
                .SingleOrDefaultAsync(p => p.WalletTransactionId == existing.Id && p.StoreId == store.Id, cancellationToken)
                ?? throw new DomainException("Idempotency key was already used for a different operation.");

            WalletBalanceResult wallet = await walletService.GetAsync(ownerId, cancellationToken);
            await databaseTransaction.CommitAsync(cancellationToken);
            return new StoreVipPurchaseResult(done.StartsAtUtc, done.EndsAtUtc, done.ChargedAmount, wallet.Balance);
        }

        DateTimeOffset now = clock.UtcNow;
        DateTimeOffset? current = await VipUntilAsync(store.Id, now, cancellationToken);
        DateTimeOffset start = current ?? now;
        DateTimeOffset end = start.AddDays(plan.DurationDays);

        // A person RentoX pays for gets the VIP store free: no wallet transaction, so it is never income.
        Guid? grantedBy = await dbContext.UserProfiles
            .AsNoTracking()
            .Where(p => p.Id == ownerId && (p.FreeServices & FreeServiceKinds.StoreVip) != 0)
            .Select(p => p.FreeServicesGrantedByUserId)
            .SingleOrDefaultAsync(cancellationToken);

        if (grantedBy.HasValue)
        {
            dbContext.Set<StoreVipPurchase>().Add(StoreVipPurchase.CreateGift(
                store.Id, plan.Name, grantedBy.Value, start, end, now));
            await dbContext.SaveChangesAsync(cancellationToken);
            await databaseTransaction.CommitAsync(cancellationToken);

            WalletBalanceResult own = await walletService.GetAsync(ownerId, cancellationToken);
            return new StoreVipPurchaseResult(start, end, 0m, own.Balance, true);
        }

        WalletOperationResult debit = await walletService.DebitAsync(
            new DebitWalletCommand(
                ownerId, plan.Price, WalletTransactionType.PromotionFee, "VIP store place", store.Id, key),
            cancellationToken);

        if (debit.WasAlreadyProcessed)
        {
            throw new DomainException("This payment was already processed. Retry the request.");
        }

        dbContext.Set<StoreVipPurchase>().Add(StoreVipPurchase.Create(
            store.Id, plan.Name, plan.Price, debit.Transaction.Id, start, end, now));
        await dbContext.SaveChangesAsync(cancellationToken);
        await databaseTransaction.CommitAsync(cancellationToken);

        return new StoreVipPurchaseResult(start, end, plan.Price, debit.Wallet.Balance);
    }

    public async Task<StoreVipOffersResult> SendOffersAsync(CancellationToken cancellationToken = default)
    {
        StoreVipPlanValues plan = await GetPlanAsync(cancellationToken);
        if (!plan.IsEnabled)
        {
            throw new DomainException("Turn the VIP store product on before sending offers.");
        }

        DateTimeOffset now = clock.UtcNow;
        DateTimeOffset notBefore = now.AddHours(-plan.OfferIntervalHours);

        Guid[] owners = await dbContext.Set<StoreProfile>()
            .AsNoTracking()
            .Where(s => s.Status == StoreStatus.Active
                && !dbContext.Set<StoreVipPurchase>().Any(p => p.StoreId == s.Id && p.EndsAtUtc > now))
            .Select(s => s.OwnerId)
            .ToArrayAsync(cancellationToken);

        HashSet<Guid> recent = (await dbContext.Set<Notification>()
            .AsNoTracking()
            .Where(n => n.Type == NotificationType.StoreVipOffer && n.CreatedAtUtc > notBefore && owners.Contains(n.UserId))
            .Select(n => n.UserId)
            .Distinct()
            .ToListAsync(cancellationToken)).ToHashSet();

        string body = string.IsNullOrWhiteSpace(plan.OfferMessage)
            ? $"Mağazanızı ana səhifədə göstərin: {plan.Price:0.##} AZN, {plan.DurationDays} gün."
            : plan.OfferMessage;

        int sent = 0;
        foreach (Guid owner in owners.Where(o => !recent.Contains(o)))
        {
            if (notifications is null)
            {
                break;
            }

            await notifications.CreateAsync(
                new CreateNotificationCommand(owner, (int)NotificationType.StoreVipOffer, plan.Name, body, null, "/magaza"),
                cancellationToken);
            sent++;
        }

        return new StoreVipOffersResult(sent, owners.Length - sent);
    }

    public async Task<IReadOnlyList<PublicStoreSummaryResult>> GetVipStoresAsync(
        int take, CancellationToken cancellationToken = default)
    {
        take = Math.Clamp(take, 1, 50);
        DateTimeOffset now = clock.UtcNow;

        var rows = await dbContext.Set<StoreProfile>()
            .AsNoTracking()
            .Where(s => s.Status == StoreStatus.Active
                && dbContext.Set<StoreVipPurchase>().Any(p => p.StoreId == s.Id && p.StartsAtUtc <= now && p.EndsAtUtc > now))
            .Select(s => new
            {
                s.Id,
                s.Name,
                s.Slug,
                s.Description,
                s.LogoImageKey,
                s.CreatedAtUtc,
                s.UpdatedAtUtc,
                ListingCount = dbContext.Listings.Count(l =>
                    l.OwnerId == s.OwnerId && l.Status == ListingStatus.Active
                    && l.ExpiresAtUtc.HasValue && l.ExpiresAtUtc.Value > now),
                FollowerCount = dbContext.StoreFollowers.Count(f => f.StoreId == s.Id)
            })
            .OrderByDescending(r => r.ListingCount)
            .ThenByDescending(r => r.FollowerCount)
            .ThenBy(r => r.Name)
            .Take(take)
            .ToListAsync(cancellationToken);

        return rows
            .Select(r => new PublicStoreSummaryResult(
                r.Id, r.Name, r.Slug, r.Description,
                !string.IsNullOrWhiteSpace(r.LogoImageKey),
                (r.UpdatedAtUtc ?? r.CreatedAtUtc).ToUnixTimeMilliseconds(),
                r.ListingCount, r.FollowerCount))
            .ToArray();
    }

    /// <summary>The end of the last paid period when it is still running (periods are laid one after another).</summary>
    private async Task<DateTimeOffset?> VipUntilAsync(Guid storeId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        return await dbContext.Set<StoreVipPurchase>()
            .AsNoTracking()
            .Where(p => p.StoreId == storeId && p.EndsAtUtc > now)
            .Select(p => (DateTimeOffset?)p.EndsAtUtc)
            .MaxAsync(cancellationToken);
    }
}
