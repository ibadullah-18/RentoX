using Microsoft.EntityFrameworkCore;
using RentoX.Application.Listings.Billing;
using RentoX.Domain.Common.Exceptions;
using RentoX.Domain.Listings.Enums;
using RentoX.Infrastructure.Persistence;

namespace RentoX.Infrastructure.Listings.Billing;

public sealed class ListingActivationPricingService(
    RentoXDbContext dbContext)
    : IListingActivationPricingService
{
    public const int FreeActiveListingLimit = 5;

    public const decimal PaidActivationFee = 1m;

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

        int otherActiveListingCount =
            await dbContext.Listings
                .AsNoTracking()
                .CountAsync(
                    listing =>
                        listing.OwnerId == ownerId &&
                        listing.Id != listingId &&
                        listing.Status ==
                        ListingStatus.Active,
                    cancellationToken);

        bool requiresPayment =
            otherActiveListingCount >=
            FreeActiveListingLimit;

        decimal activationFee =
            requiresPayment
                ? PaidActivationFee
                : 0;

        return new ListingActivationPricingResult(
            listingId,
            ownerId,
            otherActiveListingCount,
            FreeActiveListingLimit,
            requiresPayment,
            activationFee,
            "AZN");
    }
}
