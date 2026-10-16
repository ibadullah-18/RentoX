using RentoX.Domain.Common;
using RentoX.Domain.Common.Exceptions;

namespace RentoX.Domain.Hr;

/// <summary>
/// A change of one person's monthly pay, from a month on. The pay of a month is the one in force in that month, so
/// raising a salary does not change the pay (or the cost) of the months already gone. Lines are never edited.
/// </summary>
public sealed class StaffSalaryChange : Entity
{
    private StaffSalaryChange()
    {
    }

    private StaffSalaryChange(
        Guid id, Guid userId, DateOnly effectiveFrom, int amountInQepik, Guid createdByUserId, DateTimeOffset now)
        : base(id)
    {
        if (userId == Guid.Empty || createdByUserId == Guid.Empty)
        {
            throw new DomainException("The staff member and the person who made the change are required.");
        }

        if (amountInQepik is < 0 or > StaffProfile.MaximumSalaryInQepik)
        {
            throw new DomainException("The salary is not valid.");
        }

        StaffProfile.RequireUtc(now);
        UserId = userId;
        EffectiveFrom = new DateOnly(effectiveFrom.Year, effectiveFrom.Month, 1);
        AmountInQepik = amountInQepik;
        CreatedByUserId = createdByUserId;
        CreatedAtUtc = now;
    }

    public Guid UserId { get; private set; }

    /// <summary>The first day of the month the pay is in force from.</summary>
    public DateOnly EffectiveFrom { get; private set; }

    public int AmountInQepik { get; private set; }

    public Guid CreatedByUserId { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public static StaffSalaryChange Create(
        Guid userId, DateOnly effectiveFrom, int amountInQepik, Guid createdByUserId, DateTimeOffset now) =>
        new(Guid.NewGuid(), userId, effectiveFrom, amountInQepik, createdByUserId, now);

    /// <summary>
    /// The pay in force in [month] among [changes] of one person: the latest change that starts in that month or
    /// before it (the one written last wins when two start together). Nothing before the first change: 0.
    /// </summary>
    public static int For(IEnumerable<StaffSalaryChange> changes, DateOnly month)
    {
        DateOnly first = new(month.Year, month.Month, 1);
        StaffSalaryChange? chosen = changes
            .Where(c => c.EffectiveFrom <= first)
            .OrderByDescending(c => c.EffectiveFrom)
            .ThenByDescending(c => c.CreatedAtUtc)
            .FirstOrDefault();

        return chosen?.AmountInQepik ?? 0;
    }
}
