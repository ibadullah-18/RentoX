using RentoX.Domain.Common;
using RentoX.Domain.Common.Exceptions;

namespace RentoX.Domain.Finance;

public enum ExpenseCategory
{
    /// <summary>Money set aside for something (advertising, a tool, an office...).</summary>
    Budget = 1,

    /// <summary>Tax paid to the state.</summary>
    Tax = 2,

    /// <summary>Anything else the business spent.</summary>
    Other = 3,

    /// <summary>The owner's own spending: written down, but not a business cost.</summary>
    Personal = 4
}

/// <summary>
/// One amount of money spent, written down by the owner. A line is never edited or deleted; a mistake is voided
/// (and stays visible with who did it and why).
/// </summary>
public sealed class FinanceExpense : Entity
{
    public const int MaximumTitleLength = 120;
    public const int MaximumNoteLength = 400;
    public const decimal MaximumAmount = 10_000_000m;

    private FinanceExpense()
    {
    }

    private FinanceExpense(
        Guid id, ExpenseCategory category, string title, decimal amount, DateOnly day, string? note,
        Guid createdByUserId, DateTimeOffset now)
        : base(id)
    {
        if (!Enum.IsDefined(category))
        {
            throw new DomainException("The kind of expense is not valid.");
        }

        if (string.IsNullOrWhiteSpace(title) || title.Trim().Length > MaximumTitleLength)
        {
            throw new DomainException($"The title is required and at most {MaximumTitleLength} characters.");
        }

        if (amount <= 0 || amount > MaximumAmount || decimal.Round(amount, 2) != amount)
        {
            throw new DomainException("The amount must be more than zero, with at most two decimals.");
        }

        string? cleaned = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
        if (cleaned is { Length: > MaximumNoteLength })
        {
            throw new DomainException($"The note cannot exceed {MaximumNoteLength} characters.");
        }

        if (createdByUserId == Guid.Empty)
        {
            throw new DomainException("The person who wrote the expense is required.");
        }

        if (now == default || now.Offset != TimeSpan.Zero)
        {
            throw new DomainException("The time must be a UTC time.");
        }

        Category = category;
        Title = title.Trim();
        Amount = amount;
        Day = day;
        Note = cleaned;
        CreatedByUserId = createdByUserId;
        CreatedAtUtc = now;
    }

    public ExpenseCategory Category { get; private set; }

    public string Title { get; private set; } = string.Empty;

    public decimal Amount { get; private set; }

    /// <summary>The day the money was spent (Baku day).</summary>
    public DateOnly Day { get; private set; }

    public string? Note { get; private set; }

    public Guid CreatedByUserId { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public Guid? VoidedByUserId { get; private set; }

    public DateTimeOffset? VoidedAtUtc { get; private set; }

    public string? VoidReason { get; private set; }

    public bool IsVoid => VoidedAtUtc.HasValue;

    public static FinanceExpense Create(
        ExpenseCategory category, string title, decimal amount, DateOnly day, string? note,
        Guid createdByUserId, DateTimeOffset now) =>
        new(Guid.NewGuid(), category, title, amount, day, note, createdByUserId, now);

    public void Void(Guid byUserId, string reason, DateTimeOffset now)
    {
        if (byUserId == Guid.Empty)
        {
            throw new DomainException("The person who voided the expense is required.");
        }

        if (IsVoid)
        {
            throw new DomainException("The expense is already voided.");
        }

        if (string.IsNullOrWhiteSpace(reason) || reason.Trim().Length > MaximumNoteLength)
        {
            throw new DomainException("The reason is required.");
        }

        VoidedByUserId = byUserId;
        VoidedAtUtc = now;
        VoidReason = reason.Trim();
    }
}
