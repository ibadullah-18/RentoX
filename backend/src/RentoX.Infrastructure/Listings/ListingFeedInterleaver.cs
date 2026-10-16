namespace RentoX.Infrastructure.Listings;

/// <summary>
/// Mixes the three kinds of listings in a feed so that paid VIP listings do
/// not push everything else out of sight: every round shows up to
/// <see cref="VipPerRound"/> VIP listings, then up to
/// <see cref="BumpedPerRound"/> recently bumped ones, then up to
/// <see cref="PlainPerRound"/> plain ones, and the round repeats. A kind
/// that has run out is skipped, so nothing is ever left a gap.
/// </summary>
public static class ListingFeedInterleaver
{
    public const int VipPerRound = 4;
    public const int BumpedPerRound = 4;
    public const int PlainPerRound = 4;

    /// <summary>The wide (computer) feed: rows of five, so 2 rows of VIP, 3 of moved-up, 3 of ordinary.</summary>
    public static readonly int[] WideRound = [10, 15, 15];

    public const int Vip = 0;
    public const int Bumped = 1;
    public const int Plain = 2;

    /// <summary>
    /// Which kind sits at each position of the page, and how many of each
    /// kind come before the page (so each kind can be fetched with its own
    /// skip/take).
    /// </summary>
    public sealed record Plan(
        IReadOnlyList<int> Page,
        int[] SkipPerKind,
        int[] TakePerKind);

    public static Plan Build(
        int vipCount,
        int bumpedCount,
        int plainCount,
        int skip,
        int take,
        int[]? round = null)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(skip);
        ArgumentOutOfRangeException.ThrowIfNegative(take);

        int[] available = [vipCount, bumpedCount, plainCount];
        int[] perRound = round ?? [VipPerRound, BumpedPerRound, PlainPerRound];
        int[] skipped = [0, 0, 0];
        int[] taken = [0, 0, 0];
        List<int> page = [];

        int end = skip + take;
        int position = 0;

        while (position < end &&
               (available[Vip] > 0 ||
                available[Bumped] > 0 ||
                available[Plain] > 0))
        {
            for (int kind = 0; kind < 3 && position < end; kind++)
            {
                int quota = Math.Min(perRound[kind], available[kind]);

                for (int i = 0; i < quota && position < end; i++)
                {
                    if (position < skip)
                    {
                        skipped[kind]++;
                    }
                    else
                    {
                        page.Add(kind);
                        taken[kind]++;
                    }

                    available[kind]--;
                    position++;
                }
            }
        }

        return new Plan(page, skipped, taken);
    }
}
