namespace RentoX.Domain.Hr;

/// <summary>Working days are counted in Baku time (UTC+4, no summer time).</summary>
public static class HrTime
{
    public static readonly TimeSpan BakuOffset = TimeSpan.FromHours(4);

    /// <summary>How long after the start of the day a person may still start without being late.</summary>
    public const int GraceMinutes = 15;

    public static DateTimeOffset ToBaku(DateTimeOffset utc) => utc.ToOffset(BakuOffset);

    public static DateOnly BakuDay(DateTimeOffset utc) => DateOnly.FromDateTime(ToBaku(utc).DateTime);

    /// <summary>Minutes after midnight in Baku.</summary>
    public static int BakuMinute(DateTimeOffset utc)
    {
        DateTime local = ToBaku(utc).DateTime;
        return local.Hour * 60 + local.Minute;
    }

    /// <summary>The moment a Baku day and minute is, as UTC.</summary>
    public static DateTimeOffset FromBaku(DateOnly day, int minute) =>
        new DateTimeOffset(day.ToDateTime(TimeOnly.MinValue).AddMinutes(minute), BakuOffset).ToUniversalTime();

    /// <summary>How many of the days from [from] to [to] (both included) the person works.</summary>
    public static int WorkingDaysBetween(DateOnly from, DateOnly to, StaffProfile profile)
    {
        int count = 0;
        for (DateOnly d = from; d <= to; d = d.AddDays(1))
        {
            if (profile.Works(d.DayOfWeek))
            {
                count++;
            }
        }

        return count;
    }
}
