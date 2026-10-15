using RentoX.Domain.Common;
using RentoX.Domain.Common.Exceptions;

namespace RentoX.Domain.Hr;

/// <summary>One working day of one person: when they started and when they finished (Baku day).</summary>
public sealed class AttendanceRecord : Entity
{
    private AttendanceRecord()
    {
    }

    private AttendanceRecord(Guid id, Guid userId, DateOnly day, DateTimeOffset checkInAtUtc)
        : base(id)
    {
        if (userId == Guid.Empty)
        {
            throw new DomainException("The staff member is required.");
        }

        StaffProfile.RequireUtc(checkInAtUtc);
        UserId = userId;
        Day = day;
        CheckInAtUtc = checkInAtUtc;
    }

    public Guid UserId { get; private set; }

    public DateOnly Day { get; private set; }

    public DateTimeOffset CheckInAtUtc { get; private set; }

    public DateTimeOffset? CheckOutAtUtc { get; private set; }

    public static AttendanceRecord Start(Guid userId, DateOnly day, DateTimeOffset now) =>
        new(Guid.NewGuid(), userId, day, now);

    public void Finish(DateTimeOffset now)
    {
        StaffProfile.RequireUtc(now);

        if (CheckOutAtUtc.HasValue)
        {
            throw new DomainException("The day is already finished.");
        }

        if (now < CheckInAtUtc)
        {
            throw new DomainException("The day cannot finish before it started.");
        }

        CheckOutAtUtc = now;
    }

    /// <summary>Minutes between start and finish (until [now] while the day is still open).</summary>
    public int WorkedMinutes(DateTimeOffset now) =>
        (int)Math.Max(0, ((CheckOutAtUtc ?? now) - CheckInAtUtc).TotalMinutes);
}
