using Microsoft.EntityFrameworkCore;
using RentoX.Application.Listings;
using RentoX.Domain.Common.Exceptions;
using RentoX.Domain.Listings;
using RentoX.Domain.Listings.Enums;
using RentoX.Infrastructure.Persistence;

namespace RentoX.Infrastructure.Listings;

public sealed class ListingUpdateService(
    RentoXDbContext dbContext)
    : IListingUpdateService
{
    public async Task<UpdateListingResult> UpdateAsync(
        UpdateListingCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (!Enum.IsDefined(
                typeof(RentalPeriodUnit),
                command.RentalPeriodUnit))
        {
            throw new DomainException(
                "Rental period unit is invalid.");
        }

        Listing listing =
            await dbContext.Listings
                .SingleOrDefaultAsync(
                    item =>
                        item.Id == command.ListingId &&
                        item.OwnerId == command.OwnerId,
                    cancellationToken)
            ?? throw new DomainException(
                "Listing was not found.");

        listing.UpdateDetails(
            command.Title,
            command.Description,
            command.Price,
            command.Currency,
            (RentalPeriodUnit)
                command.RentalPeriodUnit);

        await dbContext.SaveChangesAsync(
            cancellationToken);

        return new UpdateListingResult(
            listing.Id,
            listing.OwnerId,
            listing.CategoryId,
            listing.Title,
            listing.Description,
            listing.Price,
            listing.Currency,
            (int)listing.RentalPeriodUnit,
            (int)listing.Status,
            listing.UpdatedAtUtc);
    }
}