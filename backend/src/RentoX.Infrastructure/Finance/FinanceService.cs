using Microsoft.EntityFrameworkCore;
using RentoX.Application.Abstractions.Time;
using RentoX.Application.Finance;
using RentoX.Domain.Common.Exceptions;
using RentoX.Domain.Finance;
using RentoX.Domain.Hr;
using RentoX.Domain.Stores;
using RentoX.Domain.Users;
using RentoX.Domain.Wallets;
using RentoX.Domain.Wallets.Enums;
using RentoX.Infrastructure.Persistence;

namespace RentoX.Infrastructure.Finance;

public sealed class FinanceService(RentoXDbContext dbContext, IClock clock) : IFinanceService
{
    private const string Currency = "AZN";

    public async Task<FinanceSummary> GetSummaryAsync(CancellationToken cancellationToken = default)
    {
        DateTimeOffset now = clock.UtcNow;
        DateOnly today = HrTime.BakuDay(now);
        DateOnly weekStart = today.AddDays(-(((int)today.DayOfWeek + 6) % 7));
        DateOnly monthStart = new(today.Year, today.Month, 1);

        List<FinancePeriod> periods =
        [
            await PeriodAsync("today", today, today, false, cancellationToken),
            await PeriodAsync("week", weekStart, today, false, cancellationToken),
            await PeriodAsync("month", monthStart, today, true, cancellationToken),
            await PeriodAsync("all", DateOnly.MinValue, today, true, cancellationToken)
        ];

        return new FinanceSummary(Currency, now, periods);
    }

    public async Task<MonthlyReport> GetMonthlyReportAsync(DateOnly month, CancellationToken cancellationToken = default)
    {
        DateOnly first = new(month.Year, month.Month, 1);
        DateOnly last = first.AddMonths(1).AddDays(-1);
        (DateTimeOffset from, DateTimeOffset to) = Window(first, last);

        IQueryable<WalletTransaction> inMonth = dbContext.Set<WalletTransaction>()
            .AsNoTracking()
            .Where(t => t.OccurredAtUtc >= from && t.OccurredAtUtc < to);

        IQueryable<Guid> storeVipPayments = dbContext.Set<StoreVipPurchase>().Select(p => p.WalletTransactionId);

        decimal listingFees = await SumAsync(
            inMonth.Where(t => t.Direction == WalletTransactionDirection.Debit && t.Type == WalletTransactionType.ListingFee), cancellationToken);
        decimal storeVip = await SumAsync(
            inMonth.Where(t => t.Direction == WalletTransactionDirection.Debit && t.Type == WalletTransactionType.PromotionFee
                && storeVipPayments.Contains(t.Id)), cancellationToken);
        decimal promotions = await SumAsync(
            inMonth.Where(t => t.Direction == WalletTransactionDirection.Debit && t.Type == WalletTransactionType.PromotionFee
                && !storeVipPayments.Contains(t.Id)), cancellationToken);
        decimal refunds = await SumAsync(
            inMonth.Where(t => t.Direction == WalletTransactionDirection.Credit && t.Type == WalletTransactionType.Refund), cancellationToken);

        List<ReportIncomeLine> lines =
        [
            new("Elanların aktivləşdirilməsi və yenilənməsi", listingFees),
            new("Elanın irəli çəkilməsi və VIP elan", promotions),
            new("VIP mağaza", storeVip)
        ];
        decimal serviceIncome = listingFees + promotions + storeVip;
        decimal income = serviceIncome - refunds;

        (List<ReportStaffLine> staff, decimal staffCost) = await StaffCostAsync(first, cancellationToken);

        List<FinanceExpense> expenses = await dbContext.Set<FinanceExpense>()
            .AsNoTracking()
            .Where(e => e.VoidedAtUtc == null && e.Day >= first && e.Day <= last)
            .OrderBy(e => e.Day).ThenBy(e => e.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        List<ReportExpenseLine> Lines(ExpenseCategory category) => expenses
            .Where(e => e.Category == category)
            .Select(e => new ReportExpenseLine(e.Day, e.Title, e.Amount))
            .ToList();

        List<ReportExpenseLine> budgets = Lines(ExpenseCategory.Budget);
        List<ReportExpenseLine> taxes = Lines(ExpenseCategory.Tax);
        List<ReportExpenseLine> other = Lines(ExpenseCategory.Other);
        List<ReportExpenseLine> personal = Lines(ExpenseCategory.Personal);

        decimal budgetsTotal = budgets.Sum(l => l.Amount);
        decimal taxesTotal = taxes.Sum(l => l.Amount);
        decimal otherTotal = other.Sum(l => l.Amount);
        decimal personalTotal = personal.Sum(l => l.Amount);
        decimal net = income - staffCost - budgetsTotal - taxesTotal - otherTotal;

        return new MonthlyReport(
            first, Currency, clock.UtcNow, lines, serviceIncome, refunds, income, staff, staffCost,
            budgets, budgetsTotal, taxes, taxesTotal, other, otherTotal, net, personal, personalTotal,
            net - personalTotal, await GetProfileAsync(cancellationToken));
    }

    public async Task<IReadOnlyList<ExpenseDto>> GetExpensesAsync(DateOnly month, CancellationToken cancellationToken = default)
    {
        DateOnly first = new(month.Year, month.Month, 1);
        DateOnly last = first.AddMonths(1).AddDays(-1);

        List<FinanceExpense> rows = await dbContext.Set<FinanceExpense>()
            .AsNoTracking()
            .Where(e => e.Day >= first && e.Day <= last)
            .OrderByDescending(e => e.Day).ThenByDescending(e => e.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        Dictionary<Guid, string> names = await NamesAsync(
            rows.SelectMany(r => new[] { r.CreatedByUserId, r.VoidedByUserId ?? Guid.Empty }), cancellationToken);

        return rows.Select(e => ToDto(e, names)).ToArray();
    }

    public async Task<ExpenseDto> AddExpenseAsync(
        Guid actorUserId, AddExpenseCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        FinanceExpense expense = FinanceExpense.Create(
            (ExpenseCategory)command.Category, command.Title, command.Amount, command.Day, command.Note,
            actorUserId, clock.UtcNow);

        if (expense.Day > HrTime.BakuDay(clock.UtcNow))
        {
            throw new DomainException("An expense cannot be in the future.");
        }

        dbContext.Set<FinanceExpense>().Add(expense);
        await dbContext.SaveChangesAsync(cancellationToken);

        return ToDto(expense, await NamesAsync([actorUserId], cancellationToken));
    }

    public async Task<ExpenseDto> VoidExpenseAsync(
        Guid actorUserId, Guid expenseId, string reason, CancellationToken cancellationToken = default)
    {
        FinanceExpense expense = await dbContext.Set<FinanceExpense>()
            .SingleOrDefaultAsync(e => e.Id == expenseId, cancellationToken)
            ?? throw new DomainException("The expense was not found.");

        expense.Void(actorUserId, reason, clock.UtcNow);
        await dbContext.SaveChangesAsync(cancellationToken);

        return ToDto(expense, await NamesAsync([expense.CreatedByUserId, actorUserId], cancellationToken));
    }

    public async Task<FinanceProfileDto> GetProfileAsync(CancellationToken cancellationToken = default)
    {
        FinanceProfile? profile = await dbContext.Set<FinanceProfile>()
            .AsNoTracking()
            .SingleOrDefaultAsync(p => p.Id == FinanceProfile.SingleId, cancellationToken);

        return profile is null
            ? new FinanceProfileDto("İbadulla Hüseynzadə", null, null, null, null)
            : new FinanceProfileDto(profile.FullName, profile.Voen, profile.Phone, profile.Email, profile.Address);
    }

    public async Task<FinanceProfileDto> SetProfileAsync(
        Guid actorUserId, FinanceProfileDto profile, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(profile);

        FinanceProfile? row = await dbContext.Set<FinanceProfile>()
            .SingleOrDefaultAsync(p => p.Id == FinanceProfile.SingleId, cancellationToken);

        if (row is null)
        {
            row = FinanceProfile.CreateEmpty();
            dbContext.Set<FinanceProfile>().Add(row);
        }

        row.Update(profile.FullName, profile.Voen, profile.Phone, profile.Email, profile.Address, actorUserId, clock.UtcNow);
        await dbContext.SaveChangesAsync(cancellationToken);

        return await GetProfileAsync(cancellationToken);
    }

    private async Task<FinancePeriod> PeriodAsync(
        string name, DateOnly from, DateOnly to, bool withStaff, CancellationToken cancellationToken)
    {
        (DateTimeOffset start, DateTimeOffset end) = from == DateOnly.MinValue
            ? (DateTimeOffset.MinValue, HrTime.FromBaku(to.AddDays(1), 0))
            : Window(from, to);

        IQueryable<WalletTransaction> inside = dbContext.Set<WalletTransaction>()
            .AsNoTracking()
            .Where(t => t.OccurredAtUtc >= start && t.OccurredAtUtc < end);

        decimal service = await SumAsync(
            inside.Where(t => t.Direction == WalletTransactionDirection.Debit
                && (t.Type == WalletTransactionType.ListingFee || t.Type == WalletTransactionType.PromotionFee)), cancellationToken);
        decimal refunds = await SumAsync(
            inside.Where(t => t.Direction == WalletTransactionDirection.Credit && t.Type == WalletTransactionType.Refund), cancellationToken);

        decimal expenses = await dbContext.Set<FinanceExpense>()
            .AsNoTracking()
            .Where(e => e.VoidedAtUtc == null && e.Category != ExpenseCategory.Personal && e.Day >= from && e.Day <= to)
            .Select(e => (decimal?)e.Amount)
            .SumAsync(cancellationToken) ?? 0m;

        decimal? staff = null;
        if (withStaff)
        {
            staff = 0m;
            DateOnly firstMonth = from == DateOnly.MinValue ? await EarliestStaffMonthAsync(cancellationToken) : new DateOnly(from.Year, from.Month, 1);
            for (DateOnly m = firstMonth; m <= to; m = m.AddMonths(1))
            {
                staff += (await StaffCostAsync(m, cancellationToken)).Total;
            }
        }

        decimal income = service - refunds;
        return new FinancePeriod(
            name, from == DateOnly.MinValue ? to : from, to, service, refunds, income, expenses, staff,
            staff.HasValue ? income - staff.Value - expenses : null);
    }

    private async Task<DateOnly> EarliestStaffMonthAsync(CancellationToken cancellationToken)
    {
        DateTimeOffset? first = await dbContext.Set<StaffProfile>()
            .AsNoTracking()
            .Select(p => (DateTimeOffset?)p.CreatedAtUtc)
            .MinAsync(cancellationToken);

        DateOnly today = HrTime.BakuDay(clock.UtcNow);
        if (!first.HasValue)
        {
            return new DateOnly(today.Year, today.Month, 1);
        }

        DateOnly day = HrTime.BakuDay(first.Value);
        return new DateOnly(day.Year, day.Month, 1);
    }

    /// <summary>Pay for a month: each person's salary, plus bonuses, less deductions (staff who started by then).</summary>
    private async Task<(List<ReportStaffLine> Lines, decimal Total)> StaffCostAsync(DateOnly month, CancellationToken cancellationToken)
    {
        DateOnly first = new(month.Year, month.Month, 1);
        DateTimeOffset monthEnd = HrTime.FromBaku(first.AddMonths(1), 0);

        var profiles = await dbContext.Set<StaffProfile>()
            .AsNoTracking()
            .Where(p => p.CreatedAtUtc < monthEnd)
            .Select(p => new { p.UserId })
            .ToListAsync(cancellationToken);

        if (profiles.Count == 0)
        {
            return ([], 0m);
        }

        Guid[] ids = profiles.Select(p => p.UserId).ToArray();
        var lines = await dbContext.Set<PayrollEntry>()
            .AsNoTracking()
            .Where(e => e.Month == first && e.VoidedAtUtc == null && ids.Contains(e.UserId)
                && (e.Kind == PayrollKind.Bonus || e.Kind == PayrollKind.Deduction))
            .Select(e => new { e.UserId, e.Kind, e.AmountInQepik })
            .ToListAsync(cancellationToken);

        // The salary in force in that month, from the history of changes (not today's salary).
        List<StaffSalaryChange> changes = await dbContext.Set<StaffSalaryChange>()
            .AsNoTracking()
            .Where(c => ids.Contains(c.UserId))
            .ToListAsync(cancellationToken);

        Dictionary<Guid, string> names = await NamesAsync(ids, cancellationToken);

        List<ReportStaffLine> result = profiles
            .Select(p =>
            {
                decimal bonus = lines.Where(l => l.UserId == p.UserId && l.Kind == PayrollKind.Bonus).Sum(l => l.AmountInQepik) / 100m;
                decimal deductions = lines.Where(l => l.UserId == p.UserId && l.Kind == PayrollKind.Deduction).Sum(l => l.AmountInQepik) / 100m;
                decimal salary = StaffSalaryChange.For(changes.Where(c => c.UserId == p.UserId), first) / 100m;
                return new ReportStaffLine(names.GetValueOrDefault(p.UserId, "—"), salary, bonus, deductions, salary + bonus - deductions);
            })
            .Where(l => l.Cost != 0m || l.Salary != 0m)
            .OrderBy(l => l.Name)
            .ToList();

        return (result, result.Sum(l => l.Cost));
    }

    private static (DateTimeOffset From, DateTimeOffset To) Window(DateOnly from, DateOnly to) =>
        (HrTime.FromBaku(from, 0), HrTime.FromBaku(to.AddDays(1), 0));

    private static async Task<decimal> SumAsync(IQueryable<WalletTransaction> query, CancellationToken cancellationToken) =>
        await query.Select(t => (decimal?)t.Amount).SumAsync(cancellationToken) ?? 0m;

    private async Task<Dictionary<Guid, string>> NamesAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken)
    {
        Guid[] wanted = ids.Where(i => i != Guid.Empty).Distinct().ToArray();
        return await dbContext.Set<UserProfile>()
            .AsNoTracking()
            .Where(p => wanted.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, p => p.FullName, cancellationToken);
    }

    private static ExpenseDto ToDto(FinanceExpense e, Dictionary<Guid, string> names) =>
        new(
            e.Id, (int)e.Category, e.Title, e.Amount, e.Day, e.Note,
            names.GetValueOrDefault(e.CreatedByUserId), e.CreatedAtUtc, e.IsVoid, e.VoidReason,
            e.VoidedByUserId.HasValue ? names.GetValueOrDefault(e.VoidedByUserId.Value) : null);
}
