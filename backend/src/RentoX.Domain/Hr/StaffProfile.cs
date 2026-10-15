using RentoX.Domain.Common;
using RentoX.Domain.Common.Exceptions;

namespace RentoX.Domain.Hr;

/// <summary>
/// What the owner sets for one staff member (an Admin): the monthly pay, whether they may sign in to the panel at all,
/// and their working schedule (the hours, the days of the week, how many days off a month).
/// </summary>
public sealed class StaffProfile : Entity
{
    public const int DefaultStartMinute = 9 * 60;

    public const int DefaultEndMinute = 18 * 60;

    /// <summary>Monday to Friday (Monday is bit 1 … Sunday is bit 64).</summary>
    public const int DefaultWorkDays = 0b0011111;

    public const int DefaultMonthlyDayOffLimit = 3;

    public const int MaximumSalaryInQepik = 100_000_00;

    private StaffProfile()
    {
    }

    private StaffProfile(Guid id, Guid userId, DateTimeOffset now)
        : base(id)
    {
        if (userId == Guid.Empty)
        {
            throw new DomainException("The staff member is required.");
        }

        RequireUtc(now);
        UserId = userId;
        LoginAllowed = true;
        WorkStartMinute = DefaultStartMinute;
        WorkEndMinute = DefaultEndMinute;
        WorkDaysMask = DefaultWorkDays;
        MonthlyDayOffLimit = DefaultMonthlyDayOffLimit;
        CreatedAtUtc = now;
        UpdatedAtUtc = now;
    }

    public Guid UserId { get; private set; }

    /// <summary>The monthly pay in qəpik (1/100 AZN).</summary>
    public int MonthlySalaryInQepik { get; private set; }

    /// <summary>False: the person cannot use the admin panel even though they hold the Admin role.</summary>
    public bool LoginAllowed { get; private set; }

    /// <summary>When the working day starts and ends, as minutes after midnight (Baku time).</summary>
    public int WorkStartMinute { get; private set; }

    public int WorkEndMinute { get; private set; }

    public int WorkDaysMask { get; private set; }

    /// <summary>How many working days a month the person may take off without it being a problem.</summary>
    public int MonthlyDayOffLimit { get; private set; }

    public string? Note { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public static StaffProfile Create(Guid userId, DateTimeOffset now) => new(Guid.NewGuid(), userId, now);

    /// <summary>True when [day] is one of the person's working days.</summary>
    public bool Works(DayOfWeek day) => (WorkDaysMask & Bit(day)) != 0;

    public static int Bit(DayOfWeek day) => 1 << (((int)day + 6) % 7);

    /// <summary>Everything is checked first, then set (nothing changes when something is wrong).</summary>
    public void Update(
        int monthlySalaryInQepik,
        bool loginAllowed,
        int workStartMinute,
        int workEndMinute,
        int workDaysMask,
        int monthlyDayOffLimit,
        string? note,
        DateTimeOffset now)
    {
        RequireUtc(now);

        if (monthlySalaryInQepik is < 0 or > MaximumSalaryInQepik)
        {
            throw new DomainException("The salary is not valid.");
        }

        if (workStartMinute is < 0 or > 1439 || workEndMinute is < 1 or > 1440 || workEndMinute <= workStartMinute)
        {
            throw new DomainException("The working hours are not valid: the day must end after it starts.");
        }

        if (workDaysMask is < 0 or > 127)
        {
            throw new DomainException("The working days are not valid.");
        }

        if (monthlyDayOffLimit is < 0 or > 31)
        {
            throw new DomainException("The monthly days off must be between 0 and 31.");
        }

        string? cleaned = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
        if (cleaned is { Length: > 500 })
        {
            throw new DomainException("The note cannot exceed 500 characters.");
        }

        MonthlySalaryInQepik = monthlySalaryInQepik;
        LoginAllowed = loginAllowed;
        WorkStartMinute = workStartMinute;
        WorkEndMinute = workEndMinute;
        WorkDaysMask = workDaysMask;
        MonthlyDayOffLimit = monthlyDayOffLimit;
        Note = cleaned;
        UpdatedAtUtc = now;
    }

    internal static void RequireUtc(DateTimeOffset value)
    {
        if (value == default || value.Offset != TimeSpan.Zero)
        {
            throw new DomainException("The time must be a UTC time.");
        }
    }
}
