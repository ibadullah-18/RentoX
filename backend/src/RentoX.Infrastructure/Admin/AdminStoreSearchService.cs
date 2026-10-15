using Microsoft.EntityFrameworkCore;
using RentoX.Application.Admin;
using RentoX.Application.Common;
using RentoX.Domain.Stores.Enums;
using RentoX.Domain.Users;
using RentoX.Infrastructure.Persistence;

namespace RentoX.Infrastructure.Admin;

public sealed class AdminStoreSearchService(RentoXDbContext dbContext) : IAdminStoreSearchService
{
    private const int MaximumPageSize = 50;

    public async Task<PagedResult<AdminStoreItem>> SearchAsync(
        int? status,
        string? text,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, MaximumPageSize);

        var query =
            from store in dbContext.StoreProfiles.AsNoTracking()
            join profile in dbContext.Set<UserProfile>().AsNoTracking() on store.OwnerId equals profile.Id
            join user in dbContext.Users.AsNoTracking() on store.OwnerId equals user.Id
            select new { store, profile.FullName, user.PhoneNumber };

        if (status is not null && Enum.IsDefined(typeof(StoreStatus), status.Value))
        {
            StoreStatus wanted = (StoreStatus)status.Value;
            query = query.Where(x => x.store.Status == wanted);
        }

        string? needle = string.IsNullOrWhiteSpace(text) ? null : text.Trim();
        if (needle is not null)
        {
            string pattern = "%" + needle.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_") + "%";
            query = query.Where(x =>
                EF.Functions.ILike(x.store.Name, pattern, "\\") ||
                EF.Functions.ILike(x.FullName, pattern, "\\") ||
                (x.PhoneNumber != null && EF.Functions.ILike(x.PhoneNumber, pattern, "\\")));
        }

        int total = await query.CountAsync(cancellationToken);

        var rows = await query
            .OrderByDescending(x => x.store.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new
            {
                x.store.Id,
                x.store.OwnerId,
                x.FullName,
                x.PhoneNumber,
                x.store.Name,
                x.store.Slug,
                Status = (int)x.store.Status,
                x.store.RejectionReason,
                x.store.CreatedAtUtc
            })
            .ToListAsync(cancellationToken);

        AdminStoreItem[] items = rows
            .Select(r => new AdminStoreItem(
                r.Id, r.OwnerId, r.FullName, r.PhoneNumber, r.Name, r.Slug, r.Status, r.RejectionReason, r.CreatedAtUtc))
            .ToArray();

        int totalPages = total == 0 ? 0 : (int)Math.Ceiling(total / (double)pageSize);
        return new PagedResult<AdminStoreItem>(items, page, pageSize, total, totalPages);
    }
}
