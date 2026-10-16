using System.Collections.Concurrent;
using Microsoft.Extensions.Caching.Memory;

namespace RentoX.Api.Caching;

/// <summary>
/// A short memory of answers that are the same for every visitor (the first pages of the listing feed, a popular
/// search). Many people open the home page at the same moment: the first one makes the database do the work, the
/// others (even those who arrive while it is still working) get that same answer, for a few seconds. It is only
/// used for signed-out visitors, whose answers carry nothing personal.
/// </summary>
public sealed class AnonymousResponseCache : IDisposable
{
    /// <summary>How long an answer is kept: short enough that a new listing shows up quickly.</summary>
    public static readonly TimeSpan Lifetime = TimeSpan.FromSeconds(20);

    /// <summary>The most answers kept at once (memory stays small whatever the visitors ask).</summary>
    private const int MaximumEntries = 400;

    private readonly MemoryCache cache = new(new MemoryCacheOptions { SizeLimit = MaximumEntries });
    private readonly ConcurrentDictionary<string, SemaphoreSlim> gates = new();

    public async Task<T> GetOrCreateAsync<T>(string key, Func<Task<T>> create)
        where T : class
    {
        if (cache.TryGetValue(key, out T? known) && known is not null)
        {
            return known;
        }

        SemaphoreSlim gate = gates.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync();
        try
        {
            // Someone else may have made it while this one waited.
            if (cache.TryGetValue(key, out known) && known is not null)
            {
                return known;
            }

            T value = await create();
            cache.Set(key, value, new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = Lifetime,
                Size = 1
            });
            return value;
        }
        finally
        {
            gate.Release();
            // The answer is in the cache by now, so the next visitor does not need this gate any more.
            gates.TryRemove(key, out _);
        }
    }

    public void Dispose() => cache.Dispose();
}
