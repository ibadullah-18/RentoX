using Microsoft.EntityFrameworkCore;
using RentoX.Application.Abstractions.Authentication;
using RentoX.Application.Abstractions.Time;
using RentoX.Application.Admin;
using RentoX.Domain.Common.Exceptions;
using RentoX.Domain.Moderation;
using RentoX.Infrastructure.Persistence;

namespace RentoX.Infrastructure.Admin;

public sealed class BannedWordService(
    RentoXDbContext dbContext,
    IClock clock,
    ICurrentUserContext currentUserContext)
    : IBannedWordService
{
    private const int MaximumWords = 500;

    private const int MaximumListings = 50;

    public async Task<IReadOnlyList<BannedWordItem>> ListAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.Set<BannedWord>().AsNoTracking()
            .OrderBy(w => w.Word)
            .Select(w => new BannedWordItem(w.Id, w.Word, w.CreatedByUserId, w.CreatedAtUtc))
            .ToListAsync(cancellationToken);
    }

    public async Task<BannedWordItem> AddAsync(string word, CancellationToken cancellationToken = default)
    {
        Guid? actor = currentUserContext.UserId;
        if (!currentUserContext.IsAuthenticated || !actor.HasValue || actor.Value == Guid.Empty)
        {
            throw new DomainException("An authenticated admin is required.");
        }

        BannedWord entry = BannedWord.Create(word, actor.Value, clock.UtcNow);

        if (await dbContext.Set<BannedWord>().AnyAsync(w => w.Word == entry.Word, cancellationToken))
        {
            throw new DomainException("This word is already in the list.");
        }

        if (await dbContext.Set<BannedWord>().CountAsync(cancellationToken) >= MaximumWords)
        {
            throw new DomainException($"The list cannot hold more than {MaximumWords} words.");
        }

        dbContext.Set<BannedWord>().Add(entry);
        await dbContext.SaveChangesAsync(cancellationToken);
        return new BannedWordItem(entry.Id, entry.Word, entry.CreatedByUserId, entry.CreatedAtUtc);
    }

    public async Task<bool> RemoveAsync(Guid id, CancellationToken cancellationToken = default)
    {
        int removed = await dbContext.Set<BannedWord>().Where(w => w.Id == id).ExecuteDeleteAsync(cancellationToken);
        return removed > 0;
    }

    public async Task<IReadOnlyDictionary<Guid, IReadOnlyList<string>>> FindInListingsAsync(
        IReadOnlyCollection<Guid> listingIds,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(listingIds);

        Guid[] ids = listingIds.Distinct().Take(MaximumListings).ToArray();
        if (ids.Length == 0)
        {
            return new Dictionary<Guid, IReadOnlyList<string>>();
        }

        string[] words = await dbContext.Set<BannedWord>().AsNoTracking()
            .Select(w => w.Word)
            .ToArrayAsync(cancellationToken);
        if (words.Length == 0)
        {
            return new Dictionary<Guid, IReadOnlyList<string>>();
        }

        var listings = await dbContext.Listings.IgnoreQueryFilters().AsNoTracking()
            .Where(l => ids.Contains(l.Id))
            .Select(l => new { l.Id, l.Title, l.Description })
            .ToListAsync(cancellationToken);

        Dictionary<Guid, IReadOnlyList<string>> result = [];
        foreach (var listing in listings)
        {
            IReadOnlyList<string> found = BannedWordMatcher.Find(words, listing.Title, listing.Description);
            if (found.Count > 0)
            {
                result[listing.Id] = found;
            }
        }

        return result;
    }
}
