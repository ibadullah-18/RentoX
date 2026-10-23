namespace RentoX.Infrastructure.Listings;

/// <summary>
/// Mixes the three kinds of listings in a feed so that paid VIP listings do
/// not push everything else out of sight: every round shows up to
/// <see cref="VipPerRound"/> VIP listings, then up to
/// <see cref="BumpedPerRound"/> recently bumped ones, then up to
/// <see cref="PlainPerRound"/> plain ones, and the round repeats.
///
/// VIP listings come round again: while there are still bumped or plain listings to show, the VIP slots of every
/// round keep being filled, and when the last VIP listing has been shown the next VIP slot shows the first one
/// again. So the paid places never go empty as the visitor scrolls down. A round never shows the same VIP listing
/// twice (with 2 VIP listings, a round has 2 VIP slots, not 4). When nothing else is left, each VIP listing that has
/// not been shown yet is shown once, and the feed ends. Bumped and plain listings are never repeated.
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
    /// For each position of the page: which kind of listing sits there, and which one of that kind (its place in the
    /// list of that kind, starting at 0; a VIP place that came round again points to the first ones). [Total] is how
    /// many positions the whole feed has, repeats included (this is what the number of pages is made from).
    /// </summary>
    public sealed record Plan(
        IReadOnlyList<int> Page,
        IReadOnlyList<int> Indices,
        int Total);

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

        int[] perRound = round ?? [VipPerRound, BumpedPerRound, PlainPerRound];

        int bumpedLeft = bumpedCount;
        int plainLeft = plainCount;
        int bumpedUsed = 0;
        int plainUsed = 0;
        long vipPlace = 0;
        int position = 0;
        int end = skip + take;

        List<int> kinds = [];
        List<int> indices = [];

        void Add(int kind, int index)
        {
            if (position >= skip && position < end)
            {
                kinds.Add(kind);
                indices.Add(index);
            }

            position++;
        }

        while (true)
        {
            bool othersLeft = bumpedLeft > 0 || plainLeft > 0;

            int vipTake = vipCount == 0
                ? 0
                : othersLeft
                    ? Math.Min(perRound[Vip], vipCount)
                    : (int)Math.Min(perRound[Vip], Math.Max(0, vipCount - vipPlace));
            int bumpedTake = Math.Min(perRound[Bumped], bumpedLeft);
            int plainTake = Math.Min(perRound[Plain], plainLeft);

            if (vipTake == 0 && bumpedTake == 0 && plainTake == 0)
            {
                break;
            }

            for (int i = 0; i < vipTake; i++)
            {
                Add(Vip, (int)((vipPlace + i) % vipCount));
            }

            vipPlace += vipTake;

            for (int i = 0; i < bumpedTake; i++)
            {
                Add(Bumped, bumpedUsed + i);
            }

            bumpedUsed += bumpedTake;
            bumpedLeft -= bumpedTake;

            for (int i = 0; i < plainTake; i++)
            {
                Add(Plain, plainUsed + i);
            }

            plainUsed += plainTake;
            plainLeft -= plainTake;
        }

        return new Plan(kinds, indices, position);
    }
}
