using Microsoft.EntityFrameworkCore;
using RentoX.Application.Abstractions.Time;
using RentoX.Application.Listings.Billing;
using RentoX.Domain.Common.Exceptions;
using RentoX.Domain.Listings.Enums;
using RentoX.Domain.Users.Enums;
using RentoX.Infrastructure.Persistence;

namespace RentoX.Infrastructure.Listings.Billing;

public sealed class ListingActivationPricingService(
    RentoXDbContext dbContext,
    IClock clock,
    ITariffService tariffService)
    : IListingActivationPricingService
{
    public async Task<ListingActivationPricingResult>
        GetAsync(
            Guid ownerId,
            Guid listingId,
            CancellationToken cancellationToken = default)
    {
        if (ownerId == Guid.Empty)
        {
            throw new DomainException(
                "Listing owner id is required.");
        }

        if (listingId == Guid.Empty)
        {
            throw new DomainException(
                "Listing id is required.");
        }

        bool listingExists =
            await dbContext.Listings
                .AsNoTracking()
                .AnyAsync(
                    listing =>
                        listing.Id == listingId &&
                        listing.OwnerId == ownerId &&
                        listing.Status !=
                        ListingStatus.Deleted,
                    cancellationToken);

        if (!listingExists)
        {
            throw new DomainException(
                "Listing was not found.");
        }

        DateTimeOffset now = clock.UtcNow;
        TariffValues tariff = await tariffService.GetCurrentAsync(cancellationToken);

        int otherActiveListingCount =
            await dbContext.Listings
                .AsNoTracking()
                .CountAsync(
                    listing =>
                        listing.OwnerId == ownerId &&
                        listing.Id != listingId &&
                        listing.Status ==
                        ListingStatus.Active &&
                        listing.ExpiresAtUtc.HasValue &&
                        listing.ExpiresAtUtc.Value > now,
                    cancellationToken);

        // A person RentoX pays for never meets the limit fee, however many listings they post.
        bool freeServices =
            await dbContext.UserProfiles
                .AsNoTracking()
                .AnyAsync(
                    profile => profile.Id == ownerId && (profile.FreeServices & FreeServiceKinds.ListingFee) != 0,
                    cancellationToken);

        bool requiresPayment =
            !freeServices &&
            otherActiveListingCount >=
            tariff.FreeActiveListingLimit;

        decimal activationFee =
            requiresPayment
                ? tariff.ActivationFee
                : 0;

        return new ListingActivationPricingResult(
            listingId,
            ownerId,
            otherActiveListingCount,
            tariff.FreeActiveListingLimit,
            requiresPayment,
            activationFee,
            "AZN");
    }
}
