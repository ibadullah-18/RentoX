using Microsoft.EntityFrameworkCore;
using RentoX.Application.Abstractions.Time;
using RentoX.Application.Gifts;
using RentoX.Application.Listings.Billing;
using RentoX.Application.Notifications;
using RentoX.Application.Stores;
using RentoX.Domain.Auditing;
using RentoX.Domain.Common.Exceptions;
using RentoX.Domain.Listings.Enums;
using RentoX.Domain.Listings.Promotions;
using RentoX.Domain.Listings.Promotions.Enums;
using RentoX.Domain.Notifications;
using RentoX.Domain.Stores;
using RentoX.Domain.Stores.Enums;
using RentoX.Domain.Users.Enums;
using RentoX.Infrastructure.Persistence;

namespace RentoX.Infrastructure.Gifts;

public sealed class GiftService(
    RentoXDbContext dbContext,
    IClock clock,
    ITariffService tariffService,
    IStoreVipService storeVipService,
    INotificationService notifications)
    : IGiftService
{
    private const int MaximumDays = 365;

    public async Task<GiftResult> GiftListingPromotionAsync(
        Guid actorUserId, Guid listingId, int type, int? days, CancellationToken cancellationToken = default)
    {
        RequireActor(actorUserId);

        if (!Enum.IsDefined(typeof(ListingPromotionType), type))
        {
            throw new DomainException("Listing promotion type is invalid.");
        }

        ListingPromotionType kind = (ListingPromotionType)type;
        DateTimeOffset now = clock.UtcNow;

        var listing = await dbContext.Listings
            .AsNoTracking()
            .Where(l => l.Id == listingId)
            .Select(l => new { l.Id, l.OwnerId, l.Title, l.Status, l.ExpiresAtUtc })
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new DomainException("Listing was not found.");

        if (listing.Status != ListingStatus.Active || !listing.ExpiresAtUtc.HasValue || listing.ExpiresAtUtc.Value <= now)
        {
            throw new DomainException("Only an unexpired active listing can be promoted.");
        }

        ListingPromotion promotion;
        string title;

        if (kind == ListingPromotionType.Bump)
        {
            promotion = ListingPromotion.CreateGiftBump(listing.Id, actorUserId, now);
            title = $"Elanınız “{listing.Title}” RentoX tərəfindən pulsuz irəli çəkildi.";
        }
        else
        {
            int length = days ?? (await tariffService.GetCurrentAsync(cancellationToken)).VipDurationDays;
            if (length is < 1 or > MaximumDays)
            {
                throw new DomainException($"The number of days must be between 1 and {MaximumDays}.");
            }

            bool alreadyVip = await dbContext.ListingPromotions
                .AsNoTracking()
                .AnyAsync(
                    p => p.ListingId == listing.Id && p.Type == ListingPromotionType.Vip
                        && p.StartsAtUtc <= now && p.EndsAtUtc > now,
                    cancellationToken);
            if (alreadyVip)
            {
                throw new DomainException("An active VIP promotion already exists for this listing.");
            }

            // A VIP longer than the listing itself would mean nothing: it ends with the listing.
            DateTimeOffset end = now.AddDays(length);
            if (end > listing.ExpiresAtUtc.Value)
            {
                end = listing.ExpiresAtUtc.Value;
            }

            if (end - now < TimeSpan.FromHours(1))
            {
                throw new DomainException("The listing ends too soon to be made VIP.");
            }

            promotion = ListingPromotion.CreateGiftVip(listing.Id, actorUserId, now, end, now);
            int shown = Math.Max(1, (int)Math.Round((end - now).TotalDays));
            title = $"Elanınız “{listing.Title}” RentoX tərəfindən {shown} gün pulsuz VIP edildi.";
        }

        dbContext.ListingPromotions.Add(promotion);
        dbContext.Set<AuditLogEntry>().Add(AuditLogEntry.ForGift(
            Guid.NewGuid(), actorUserId, listing.OwnerId, kind == ListingPromotionType.Bump ? 1 : 2, promotion.Id, now));
        await dbContext.SaveChangesAsync(cancellationToken);

        await NotifyAsync(listing.OwnerId, title, $"/listings/{listing.Id:D}", listing.Id, cancellationToken);
        return new GiftResult(listing.OwnerId, title, promotion.StartsAtUtc, promotion.EndsAtUtc);
    }

    public async Task<GiftResult> GiftStoreVipAsync(
        Guid actorUserId, Guid storeId, int? days, CancellationToken cancellationToken = default)
    {
        RequireActor(actorUserId);
        DateTimeOffset now = clock.UtcNow;

        var store = await dbContext.Set<StoreProfile>()
            .AsNoTracking()
            .Where(s => s.Id == storeId)
            .Select(s => new { s.Id, s.OwnerId, s.Name, s.Slug, s.Status })
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new DomainException("The store was not found.");

        if (store.Status != StoreStatus.Active)
        {
            throw new DomainException("Only an active store can become VIP.");
        }

        int length = days ?? (await storeVipService.GetPlanAsync(cancellationToken)).DurationDays;
        if (length is < 1 or > MaximumDays)
        {
            throw new DomainException($"The number of days must be between 1 and {MaximumDays}.");
        }

        // A gift is added after the period already running (paid or gift), as a second payment would be.
        DateTimeOffset? current = await dbContext.Set<StoreVipPurchase>()
            .AsNoTracking()
            .Where(p => p.StoreId == store.Id && p.EndsAtUtc > now)
            .Select(p => (DateTimeOffset?)p.EndsAtUtc)
            .MaxAsync(cancellationToken);
        DateTimeOffset start = current ?? now;
        DateTimeOffset end = start.AddDays(length);

        StoreVipPurchase purchase = StoreVipPurchase.CreateGift(store.Id, "RentoX hədiyyəsi", actorUserId, start, end, now);
        dbContext.Set<StoreVipPurchase>().Add(purchase);
        dbContext.Set<AuditLogEntry>().Add(AuditLogEntry.ForGift(Guid.NewGuid(), actorUserId, store.OwnerId, 3, purchase.Id, now));
        await dbContext.SaveChangesAsync(cancellationToken);

        string title = $"Mağazanız “{store.Name}” RentoX tərəfindən {length} gün pulsuz VIP edildi.";
        await NotifyAsync(store.OwnerId, title, $"/stores/{store.Slug}", store.Id, cancellationToken);
        return new GiftResult(store.OwnerId, title, start, end);
    }

    public async Task<int> GetFreeServicesAsync(Guid userId, CancellationToken cancellationToken = default) =>
        (int)await dbContext.UserProfiles
            .AsNoTracking()
            .Where(p => p.Id == userId)
            .Select(p => p.FreeServices)
            .SingleOrDefaultAsync(cancellationToken);

    public async Task SetFreeServicesAsync(
        Guid actorUserId, Guid userId, int services, CancellationToken cancellationToken = default)
    {
        RequireActor(actorUserId);
        DateTimeOffset now = clock.UtcNow;

        if (services < 0 || (services & ~(int)FreeServiceKinds.All) != 0)
        {
            throw new DomainException("The free services are not valid.");
        }

        FreeServiceKinds kinds = (FreeServiceKinds)services;

        RentoX.Domain.Users.UserProfile profile = await dbContext.UserProfiles
            .SingleOrDefaultAsync(p => p.Id == userId, cancellationToken)
            ?? throw new DomainException("The person was not found.");

        if (profile.FreeServices == kinds)
        {
            return;
        }

        profile.SetFreeServices(kinds, actorUserId);
        dbContext.Set<AuditLogEntry>().Add(AuditLogEntry.ForGift(
            Guid.NewGuid(), actorUserId, userId, kinds == FreeServiceKinds.None ? 5 : 4, userId, now));
        await dbContext.SaveChangesAsync(cancellationToken);

        if (kinds != FreeServiceKinds.None)
        {
            List<string> names = [];
            if (kinds.HasFlag(FreeServiceKinds.ListingFee)) names.Add("elan haqqı");
            if (kinds.HasFlag(FreeServiceKinds.Vip)) names.Add("elan VIP");
            if (kinds.HasFlag(FreeServiceKinds.Bump)) names.Add("irəli çəkmə");
            if (kinds.HasFlag(FreeServiceKinds.StoreVip)) names.Add("VIP mağaza");

            await NotifyAsync(
                userId,
                $"RentoX tərəfindən sizə pulsuz imkan verildi: {string.Join(", ", names)}.",
                "/listings", userId, cancellationToken);
        }
    }

    private static void RequireActor(Guid actorUserId)
    {
        if (actorUserId == Guid.Empty)
        {
            throw new DomainException("The person who gives the gift is required.");
        }
    }

    private async Task NotifyAsync(
        Guid userId, string body, string actionUrl, Guid relatedId, CancellationToken cancellationToken)
    {
        await notifications.CreateAsync(
            new CreateNotificationCommand(
                userId, (int)NotificationType.GiftReceived, "RentoX-dan hədiyyə", body, relatedId, actionUrl),
            cancellationToken);
    }
}
