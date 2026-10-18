namespace RentoX.Application.Finance;

/// <summary>
/// Money the site earned from its services in a period (listing fees, promotions, VIP store) less refunds, and what
/// was spent. Top-ups are NOT income: that money still belongs to the user until it is spent on a service.
/// </summary>
public sealed record FinancePeriod(
    string Name,
    DateOnly From,
    DateOnly To,
    decimal ServiceIncome,
    decimal Refunds,
    decimal Income,
    decimal BusinessExpenses,
    /// <summary>Null for a day and a week: pay is counted by the month.</summary>
    decimal? StaffCost,
    decimal? NetIncome);

public sealed record FinanceSummary(string Currency, DateTimeOffset GeneratedAtUtc, IReadOnlyList<FinancePeriod> Periods);

public sealed record ExpenseDto(
    Guid Id,
    int Category,
    string Title,
    decimal Amount,
    DateOnly Day,
    string? Note,
    string? CreatedByName,
    DateTimeOffset CreatedAtUtc,
    bool IsVoid,
    string? VoidReason,
    string? VoidedByName);

public sealed record AddExpenseCommand(int Category, string Title, decimal Amount, DateOnly Day, string? Note);

public sealed record FinanceProfileDto(
    string FullName,
    string? Voen,
    string? Phone,
    string? Email,
    string? Address);

public sealed record ReportIncomeLine(string Label, decimal Amount);

public sealed record ReportStaffLine(string Name, decimal Salary, decimal Bonus, decimal Deductions, decimal Cost);

public sealed record ReportExpenseLine(DateOnly Day, string Title, decimal Amount);

public sealed record MonthlyReport(
    DateOnly Month,
    string Currency,
    DateTimeOffset GeneratedAtUtc,
    IReadOnlyList<ReportIncomeLine> IncomeLines,
    decimal ServiceIncome,
    decimal Refunds,
    decimal Income,
    IReadOnlyList<ReportStaffLine> Staff,
    decimal StaffCost,
    IReadOnlyList<ReportExpenseLine> Budgets,
    decimal BudgetsTotal,
    IReadOnlyList<ReportExpenseLine> Taxes,
    decimal TaxesTotal,
    IReadOnlyList<ReportExpenseLine> OtherExpenses,
    decimal OtherTotal,
    /// <summary>Income less pay, budgets, taxes and other costs: what is left for the owner.</summary>
    decimal NetIncome,
    IReadOnlyList<ReportExpenseLine> Personal,
    decimal PersonalTotal,
    decimal AfterPersonal,
    FinanceProfileDto Profile,
    /// <summary>What RentoX gave for free in the month: no money was taken, so none of it is income.</summary>
    GiftCounts Gifts);

public sealed record GiftCounts(int Bumps, int ListingVip, int StoreVip);

public interface IFinanceService
{
    Task<FinanceSummary> GetSummaryAsync(CancellationToken cancellationToken = default);

    Task<MonthlyReport> GetMonthlyReportAsync(DateOnly month, CancellationToken cancellationToken = default);

    /// <summary>The expenses of a month (voided ones included, marked), newest first.</summary>
    Task<IReadOnlyList<ExpenseDto>> GetExpensesAsync(DateOnly month, CancellationToken cancellationToken = default);

    Task<ExpenseDto> AddExpenseAsync(Guid actorUserId, AddExpenseCommand command, CancellationToken cancellationToken = default);

    Task<ExpenseDto> VoidExpenseAsync(Guid actorUserId, Guid expenseId, string reason, CancellationToken cancellationToken = default);

    Task<FinanceProfileDto> GetProfileAsync(CancellationToken cancellationToken = default);

    Task<FinanceProfileDto> SetProfileAsync(Guid actorUserId, FinanceProfileDto profile, CancellationToken cancellationToken = default);
}
