using RentoX.Domain.Common;
using RentoX.Domain.Common.Exceptions;

namespace RentoX.Domain.Hr;

public enum PayrollKind
{
    /// <summary>Money kept back from the pay, with the reason.</summary>
    Deduction = 1,

    /// <summary>Money given before the pay day (a part of the pay in advance).</summary>
    Advance = 2,

    /// <summary>Money paid out of the month's pay.</summary>
    Payment = 3,

    /// <summary>Money added to the pay.</summary>
    Bonus = 4
}

/// <summary>
/// One line in a staff member's pay for a month. Lines are never edited; a mistake is voided (and stays visible).
/// </summary>
public sealed class PayrollEntry : Entity
{
    public const int MaximumNoteLength = 300;

    private PayrollEntry()
    {
    }

    private PayrollEntry(
        Guid id,
        Guid userId,
        DateOnly month,
        PayrollKind kind,
        int amountInQepik,
        string? note,
        Guid createdByUserId,
        DateTimeOffset now)
        : base(id)
    {
        if (userId == Guid.Empty || createdByUserId == Guid.Empty)
        {
            throw new DomainException("The staff member and the person who wrote the line are required.");
        }

        if (!Enum.IsDefined(kind))
        {
            throw new DomainException("The kind of line is not valid.");
        }

        if (amountInQepik is < 1 or > StaffProfile.MaximumSalaryInQepik)
        {
            throw new DomainException("The amount must be more than zero.");
        }

        string? cleaned = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
        if (kind == PayrollKind.Deduction && cleaned is null)
        {
            throw new DomainException("The reason of a deduction is required.");
        }

        if (cleaned is { Length: > MaximumNoteLength })
        {
            throw new DomainException($"The note cannot exceed {MaximumNoteLength} characters.");
        }

        StaffProfile.RequireUtc(now);
        UserId = userId;
        Month = new DateOnly(month.Year, month.Month, 1);
        Kind = kind;
        AmountInQepik = amountInQepik;
        Note = cleaned;
        CreatedByUserId = createdByUserId;
        CreatedAtUtc = now;
    }

    public Guid UserId { get; private set; }

    /// <summary>The first day of the month the line belongs to.</summary>
    public DateOnly Month { get; private set; }

    public PayrollKind Kind { get; private set; }

    public int AmountInQepik { get; private set; }

    public string? Note { get; private set; }

    public Guid CreatedByUserId { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public Guid? VoidedByUserId { get; private set; }

    public DateTimeOffset? VoidedAtUtc { get; private set; }

    public bool IsVoid => VoidedAtUtc.HasValue;

    public static PayrollEntry Create(
        Guid userId,
        DateOnly month,
        PayrollKind kind,
        int amountInQepik,
        string? note,
        Guid createdByUserId,
        DateTimeOffset now) =>
        new(Guid.NewGuid(), userId, month, kind, amountInQepik, note, createdByUserId, now);

    public void Void(Guid byUserId, DateTimeOffset now)
    {
        StaffProfile.RequireUtc(now);

        if (byUserId == Guid.Empty)
        {
            throw new DomainException("The person who voided the line is required.");
        }

        if (IsVoid)
        {
            throw new DomainException("This line is already voided.");
        }

        VoidedByUserId = byUserId;
        VoidedAtUtc = now;
    }
}
