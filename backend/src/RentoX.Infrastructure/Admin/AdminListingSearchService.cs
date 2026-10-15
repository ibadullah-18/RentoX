using Microsoft.EntityFrameworkCore;
using RentoX.Application.Admin;
using RentoX.Application.Common;
using RentoX.Domain.Listings.Enums;
using RentoX.Domain.Users;
using RentoX.Infrastructure.Persistence;

namespace RentoX.Infrastructure.Admin;

public sealed class AdminListingSearchService(RentoXDbContext dbContext) : IAdminListingSearchService
{
    private const int MaximumPageSize = 50;

    public async Task<PagedResult<AdminListingSearchItem>> SearchAsync(
        int? status,
        string? text,
        Guid? categoryId,
        Guid? ownerId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, MaximumPageSize);

        // Owner name and phone live in two tables; join them once.
        var owners =
            from profile in dbContext.Set<UserProfile>().AsNoTracking()
            join user in dbContext.Users.AsNoTracking() on profile.Id equals user.Id
            select new { profile.Id, profile.FullName, user.PhoneNumber };

        var query =
            from listing in dbContext.Listings.AsNoTracking()
            join owner in owners on listing.OwnerId equals owner.Id
            select new { listing, owner };

        if (status is not null && Enum.IsDefined(typeof(ListingStatus), status.Value))
        {
            ListingStatus wanted = (ListingStatus)status.Value;
            query = query.Where(x => x.listing.Status == wanted);
        }

        if (ownerId is not null)
        {
            query = query.Where(x => x.listing.OwnerId == ownerId);
        }

        if (categoryId is not null)
        {
            query = query.Where(x => x.listing.CategoryId == categoryId);
        }

        string? needle = string.IsNullOrWhiteSpace(text) ? null : text.Trim();
        if (needle is not null)
        {
            // ILIKE with the wildcard characters of the text itself escaped.
            string pattern = "%" + needle.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_") + "%";
            query = query.Where(x =>
                EF.Functions.ILike(x.listing.Title, pattern, "\\") ||
                EF.Functions.ILike(x.owner.FullName, pattern, "\\") ||
                (x.owner.PhoneNumber != null && EF.Functions.ILike(x.owner.PhoneNumber, pattern, "\\")));
        }

        int total = await query.CountAsync(cancellationToken);

        var rows = await query
            .OrderByDescending(x => x.listing.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new
            {
                x.listing.Id,
                x.listing.OwnerId,
                x.owner.FullName,
                x.owner.PhoneNumber,
                x.listing.CategoryId,
                x.listing.Title,
                x.listing.Price,
                x.listing.Currency,
                Unit = (int)x.listing.RentalPeriodUnit,
                Status = (int)x.listing.Status,
                CoverId = x.listing.Images.Where(i => i.IsCover).Select(i => (Guid?)i.Id).FirstOrDefault(),
                ImageCount = x.listing.Images.Count,
                x.listing.ViewCount,
                x.listing.CreatedAtUtc,
                x.listing.ExpiresAtUtc
            })
            .ToListAsync(cancellationToken);

        AdminListingSearchItem[] items = rows
            .Select(r => new AdminListingSearchItem(
                r.Id, r.OwnerId, r.FullName, r.PhoneNumber, r.CategoryId, r.Title, r.Price,
                r.Currency, r.Unit, r.Status, r.CoverId, r.ImageCount, r.ViewCount,
                r.CreatedAtUtc, r.ExpiresAtUtc))
            .ToArray();

        int totalPages = total == 0 ? 0 : (int)Math.Ceiling(total / (double)pageSize);
        return new PagedResult<AdminListingSearchItem>(items, page, pageSize, total, totalPages);
    }
}
