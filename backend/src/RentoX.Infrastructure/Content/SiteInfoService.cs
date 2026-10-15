using Microsoft.EntityFrameworkCore;
using RentoX.Application.Abstractions.Authentication;
using RentoX.Application.Abstractions.Time;
using RentoX.Application.Content;
using RentoX.Domain.Common.Exceptions;
using RentoX.Domain.Content;
using RentoX.Infrastructure.Persistence;

namespace RentoX.Infrastructure.Content;

public sealed class SiteInfoService(
    RentoXDbContext dbContext,
    IClock clock,
    ICurrentUserContext currentUserContext)
    : ISiteInfoService
{
    public async Task<SiteInfoValues> GetAsync(CancellationToken cancellationToken = default)
    {
        SiteInfo? info = await dbContext.Set<SiteInfo>().AsNoTracking().FirstOrDefaultAsync(cancellationToken);
        return info is null ? new SiteInfoValues(null, null, null, null, null, null, null, null, null, null) : Map(info);
    }

    public async Task<SiteInfoValues> UpdateAsync(SiteInfoValues values, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(values);

        Guid? actor = currentUserContext.UserId;
        if (!currentUserContext.IsAuthenticated || !actor.HasValue || actor.Value == Guid.Empty)
        {
            throw new DomainException("An authenticated admin is required.");
        }

        SiteInfo? info = await dbContext.Set<SiteInfo>().FirstOrDefaultAsync(cancellationToken);
        if (info is null)
        {
            info = SiteInfo.CreateEmpty();
            dbContext.Set<SiteInfo>().Add(info);
        }

        info.Update(
            values.OperatorName, values.Voen, values.Address,
            values.Email, values.Phone, values.Hotline, values.WhatsApp,
            values.Instagram, values.TikTok, values.Facebook,
            clock.UtcNow, actor.Value);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Map(info);
    }

    private static SiteInfoValues Map(SiteInfo info) => new(
        info.OperatorName, info.Voen, info.Address, info.Email, info.Phone, info.Hotline,
        info.WhatsApp, info.Instagram, info.TikTok, info.Facebook);
}
