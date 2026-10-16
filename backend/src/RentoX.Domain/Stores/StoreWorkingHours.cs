using System.Text.Json;
using RentoX.Domain.Common.Exceptions;

namespace RentoX.Domain.Stores;

/// <summary>One open day of a store: the day of the week (1 Monday … 7 Sunday) and the hours as minutes after midnight.</summary>
public sealed record StoreOpenDay(int Day, int OpenMinute, int CloseMinute);

/// <summary>
/// The hours a store is open, one entry per open day (a day that is not listed is closed). Kept as a small JSON text
/// in the store row; this class checks it and reads it.
/// </summary>
public static class StoreWorkingHours
{
    private sealed record Stored(int D, int O, int C);

    /// <summary>Checks the days and returns the text to keep (null when the store has no hours).</summary>
    public static string? Serialize(IReadOnlyCollection<StoreOpenDay>? days)
    {
        if (days is null || days.Count == 0)
        {
            return null;
        }

        if (days.Count > 7 || days.Select(d => d.Day).Distinct().Count() != days.Count)
        {
            throw new DomainException("A store can have one set of hours for each day of the week.");
        }

        foreach (StoreOpenDay d in days)
        {
            if (d.Day is < 1 or > 7)
            {
                throw new DomainException("The day of the week is not valid.");
            }

            if (d.OpenMinute is < 0 or > 1439 || d.CloseMinute is < 1 or > 1440 || d.CloseMinute <= d.OpenMinute)
            {
                throw new DomainException("The opening hours are not valid: the store must close after it opens.");
            }
        }

        return JsonSerializer.Serialize(
            days.OrderBy(d => d.Day).Select(d => new Stored(d.Day, d.OpenMinute, d.CloseMinute)));
    }

    /// <summary>The open days kept in [text] (an empty list when there are none or the text is not readable).</summary>
    public static IReadOnlyList<StoreOpenDay> Read(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return [];
        }

        try
        {
            return (JsonSerializer.Deserialize<List<Stored>>(text) ?? [])
                .Select(s => new StoreOpenDay(s.D, s.O, s.C))
                .ToArray();
        }
        catch (JsonException)
        {
            return [];
        }
    }
}
