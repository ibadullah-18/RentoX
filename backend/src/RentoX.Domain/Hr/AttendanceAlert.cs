using RentoX.Domain.Common;
using RentoX.Domain.Common.Exceptions;

namespace RentoX.Domain.Hr;

/// <summary>
/// "This person did not start work on this day" was already told to the owner. One per person and day,
/// so the same absence is never reported twice.
/// </summary>
public sealed class AttendanceAlert : Entity
{
    private AttendanceAlert()
    {
    }

    private AttendanceAlert(Guid id, Guid userId, DateOnly day, DateTimeOffset now)
        : base(id)
    {
        if (userId == Guid.Empty)
        {
            throw new DomainException("The staff member is required.");
        }

        StaffProfile.RequireUtc(now);
        UserId = userId;
        Day = day;
        CreatedAtUtc = now;
    }

    public Guid UserId { get; private set; }

    public DateOnly Day { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public static AttendanceAlert Create(Guid userId, DateOnly day, DateTimeOffset now) =>
        new(Guid.NewGuid(), userId, day, now);
}
