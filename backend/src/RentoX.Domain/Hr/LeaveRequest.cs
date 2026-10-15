using RentoX.Domain.Common;
using RentoX.Domain.Common.Exceptions;

namespace RentoX.Domain.Hr;

public enum LeaveStatus
{
    Pending = 1,
    Approved = 2,
    Rejected = 3,
    Cancelled = 4
}

/// <summary>
/// A staff member asks, in advance, to be away on some days and says why. When the owner approves, those days count
/// as a day off (and not as an absence).
/// </summary>
public sealed class LeaveRequest : Entity
{
    public const int MaximumReasonLength = 500;

    private LeaveRequest()
    {
    }

    private LeaveRequest(
        Guid id,
        Guid userId,
        DateOnly fromDay,
        DateOnly toDay,
        int workingDays,
        bool overLimit,
        string reason,
        DateTimeOffset now)
        : base(id)
    {
        if (userId == Guid.Empty)
        {
            throw new DomainException("The staff member is required.");
        }

        if (toDay < fromDay)
        {
            throw new DomainException("The last day cannot be before the first day.");
        }

        if (toDay.DayNumber - fromDay.DayNumber > 60)
        {
            throw new DomainException("A day off request can cover at most 61 days.");
        }

        if (workingDays < 1)
        {
            throw new DomainException("The chosen days contain no working day.");
        }

        string cleaned = (reason ?? string.Empty).Trim();
        if (cleaned.Length == 0)
        {
            throw new DomainException("The reason is required.");
        }

        if (cleaned.Length > MaximumReasonLength)
        {
            throw new DomainException($"The reason cannot exceed {MaximumReasonLength} characters.");
        }

        StaffProfile.RequireUtc(now);
        UserId = userId;
        FromDay = fromDay;
        ToDay = toDay;
        WorkingDays = workingDays;
        OverLimit = overLimit;
        Reason = cleaned;
        Status = LeaveStatus.Pending;
        CreatedAtUtc = now;
    }

    public Guid UserId { get; private set; }

    public DateOnly FromDay { get; private set; }

    public DateOnly ToDay { get; private set; }

    /// <summary>How many of the chosen days are working days of the person.</summary>
    public int WorkingDays { get; private set; }

    /// <summary>True when this request takes the person past their monthly days off.</summary>
    public bool OverLimit { get; private set; }

    public string Reason { get; private set; } = string.Empty;

    public LeaveStatus Status { get; private set; }

    public string? DecisionNote { get; private set; }

    public Guid? DecidedByUserId { get; private set; }

    public DateTimeOffset? DecidedAtUtc { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public static LeaveRequest Create(
        Guid userId,
        DateOnly fromDay,
        DateOnly toDay,
        int workingDays,
        bool overLimit,
        string reason,
        DateTimeOffset now) =>
        new(Guid.NewGuid(), userId, fromDay, toDay, workingDays, overLimit, reason, now);

    public bool Covers(DateOnly day) => Status == LeaveStatus.Approved && day >= FromDay && day <= ToDay;

    public void Decide(bool approve, string? note, Guid decidedByUserId, DateTimeOffset now)
    {
        StaffProfile.RequireUtc(now);

        if (Status != LeaveStatus.Pending)
        {
            throw new DomainException("This request has already been decided.");
        }

        if (decidedByUserId == Guid.Empty)
        {
            throw new DomainException("The person who decided is required.");
        }

        string? cleaned = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
        if (cleaned is { Length: > MaximumReasonLength })
        {
            throw new DomainException($"The note cannot exceed {MaximumReasonLength} characters.");
        }

        Status = approve ? LeaveStatus.Approved : LeaveStatus.Rejected;
        DecisionNote = cleaned;
        DecidedByUserId = decidedByUserId;
        DecidedAtUtc = now;
    }

    /// <summary>The person takes the request back (while it waits, or after approval if the days are still ahead).</summary>
    public void Cancel(DateOnly today, DateTimeOffset now)
    {
        StaffProfile.RequireUtc(now);

        if (Status is LeaveStatus.Rejected or LeaveStatus.Cancelled)
        {
            throw new DomainException("This request is already closed.");
        }

        if (Status == LeaveStatus.Approved && FromDay <= today)
        {
            throw new DomainException("Days off that have started cannot be cancelled.");
        }

        Status = LeaveStatus.Cancelled;
        DecidedAtUtc ??= now;
    }
}
