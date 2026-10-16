using Microsoft.EntityFrameworkCore;
using RentoX.Application.Abstractions.Time;
using RentoX.Application.Finance;
using RentoX.Application.Wallets;
using RentoX.Domain.Common.Exceptions;
using RentoX.Domain.Finance;
using RentoX.Domain.Hr;
using RentoX.Domain.Users;
using RentoX.Domain.Users.Enums;
using RentoX.Domain.Wallets.Enums;
using RentoX.Infrastructure.Finance;
using RentoX.Infrastructure.Identity;
using RentoX.Infrastructure.Persistence;
using RentoX.Infrastructure.Wallets;
using RentoX.IntegrationTests.Auditing;

namespace RentoX.IntegrationTests.Moderation;

/// <summary>Earnings from services (not top-ups), the owner's expenses, staff pay and the monthly report.</summary>
public sealed class FinanceTests(AuditPostgresFixture database)
    : IClassFixture<AuditPostgresFixture>
{
    // 14 October 2026, 12:00 in Baku.
    private static readonly DateTimeOffset Now = new(2026, 10, 14, 8, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task TopUpsAreNotIncomeServiceFeesAreAndRefundsAreTakenOff()
    {
        Guid user = await AddUserAsync("Müştəri");
        await using RentoXDbContext db = database.CreateContext();
        var wallet = new WalletService(db, new Clock());

        await wallet.CreditAsync(new CreditWalletCommand(user, 100m, WalletTransactionType.TopUp, "top", null, "fin-top-" + user));
        await wallet.DebitAsync(new DebitWalletCommand(user, 10m, WalletTransactionType.ListingFee, "fee", null, "fin-fee-" + user));
        await wallet.DebitAsync(new DebitWalletCommand(user, 20m, WalletTransactionType.PromotionFee, "vip", null, "fin-vip-" + user));
        await wallet.CreditAsync(new CreditWalletCommand(user, 5m, WalletTransactionType.Refund, "back", null, "fin-ref-" + user));

        FinanceSummary summary = await new FinanceService(db, new Clock()).GetSummaryAsync();
        FinancePeriod today = summary.Periods.Single(p => p.Name == "today");

        // 100 of top-up is not income; 10 + 20 spent on services less 5 given back.
        Assert.True(today.ServiceIncome >= 30m);
        Assert.True(today.Refunds >= 5m);
        Assert.Equal(today.ServiceIncome - today.Refunds, today.Income);
        Assert.Null(today.StaffCost);
        Assert.NotNull(summary.Periods.Single(p => p.Name == "month").StaffCost);
        Assert.True(summary.Periods.Single(p => p.Name == "all").Income >= today.Income);
    }

    [Fact]
    public async Task TheReportTakesPayBudgetsTaxesAndOtherCostsOffAndKeepsPersonalSpendingApart()
    {
        Guid owner = await AddUserAsync("Sahib");
        Guid worker = await AddUserAsync("İşçi Bir");

        await using (RentoXDbContext db = database.CreateContext())
        {
            StaffProfile staff = StaffProfile.Create(worker, Now.AddDays(-60));
            staff.Update(500_00, true, 540, 1080, 31, 3, null, Now.AddDays(-60));
            db.Set<StaffProfile>().Add(staff);
            // 500 from August; a raise to 600 from November must not change October.
            db.Set<StaffSalaryChange>().Add(StaffSalaryChange.Create(worker, new DateOnly(2026, 8, 1), 500_00, owner, Now.AddDays(-60)));
            db.Set<StaffSalaryChange>().Add(StaffSalaryChange.Create(worker, new DateOnly(2026, 11, 1), 600_00, owner, Now));
            db.Set<PayrollEntry>().Add(PayrollEntry.Create(worker, new DateOnly(2026, 10, 1), PayrollKind.Bonus, 50_00, null, owner, Now));
            db.Set<PayrollEntry>().Add(PayrollEntry.Create(worker, new DateOnly(2026, 10, 1), PayrollKind.Deduction, 20_00, "gecikmə", owner, Now));
            await db.SaveChangesAsync();
        }

        await using RentoXDbContext write = database.CreateContext();
        var service = new FinanceService(write, new Clock());
        var day = new DateOnly(2026, 10, 10);
        await service.AddExpenseAsync(owner, new AddExpenseCommand((int)ExpenseCategory.Budget, "Reklam", 40m, day, null));
        await service.AddExpenseAsync(owner, new AddExpenseCommand((int)ExpenseCategory.Tax, "Vergi", 15m, day, null));
        await service.AddExpenseAsync(owner, new AddExpenseCommand((int)ExpenseCategory.Other, "Ofis", 10m, day, null));
        ExpenseDto personal = await service.AddExpenseAsync(owner, new AddExpenseCommand((int)ExpenseCategory.Personal, "Şəxsi", 25m, day, "ailə"));
        ExpenseDto mistake = await service.AddExpenseAsync(owner, new AddExpenseCommand((int)ExpenseCategory.Other, "Səhv", 999m, day, null));
        await service.VoidExpenseAsync(owner, mistake.Id, "səhv yazılıb");

        MonthlyReport report = await service.GetMonthlyReportAsync(new DateOnly(2026, 10, 20));

        Assert.Equal(new DateOnly(2026, 10, 1), report.Month);
        ReportStaffLine line = Assert.Single(report.Staff, s => s.Name == "İşçi Bir");
        Assert.Equal(530m, line.Cost); // 500 + 50 bonus - 20 deduction
        // The months keep the pay they had: July is before the first change, November has the raise.
        Assert.DoesNotContain((await service.GetMonthlyReportAsync(new DateOnly(2026, 7, 1))).Staff, x => x.Name == "İşçi Bir" && x.Salary != 0m);
        Assert.Equal(600m, (await service.GetMonthlyReportAsync(new DateOnly(2026, 11, 1))).Staff.Single(x => x.Name == "İşçi Bir").Salary);
        Assert.Equal(40m, report.BudgetsTotal);
        Assert.Equal(15m, report.TaxesTotal);
        Assert.Equal(10m, report.OtherTotal); // the voided 999 is not counted
        Assert.Equal(25m, report.PersonalTotal);
        Assert.Equal(report.Income - report.StaffCost - 40m - 15m - 10m, report.NetIncome);
        Assert.Equal(report.NetIncome - 25m, report.AfterPersonal);

        // A voided line stays in the list, marked.
        IReadOnlyList<ExpenseDto> list = await service.GetExpensesAsync(day);
        Assert.Contains(list, e => e.Id == mistake.Id && e.IsVoid && e.VoidReason == "səhv yazılıb");
        Assert.Contains(list, e => e.Id == personal.Id && !e.IsVoid);
    }

    [Fact]
    public async Task ExpensesAreChecked()
    {
        Guid owner = await AddUserAsync("Yoxlama");
        await using RentoXDbContext db = database.CreateContext();
        var service = new FinanceService(db, new Clock());
        var day = new DateOnly(2026, 10, 10);

        await Assert.ThrowsAsync<DomainException>(() => service.AddExpenseAsync(owner, new AddExpenseCommand(1, "x", 0m, day, null)));
        await Assert.ThrowsAsync<DomainException>(() => service.AddExpenseAsync(owner, new AddExpenseCommand(1, " ", 5m, day, null)));
        await Assert.ThrowsAsync<DomainException>(() => service.AddExpenseAsync(owner, new AddExpenseCommand(9, "x", 5m, day, null)));
        await Assert.ThrowsAsync<DomainException>(() => service.AddExpenseAsync(owner, new AddExpenseCommand(1, "x", 5m, new DateOnly(2026, 11, 1), null)));
        await Assert.ThrowsAsync<DomainException>(() => service.VoidExpenseAsync(owner, Guid.NewGuid(), "yox", default));
    }

    [Fact]
    public async Task TheSignerDetailsStartWithTheOwnerNameAndCanBeChanged()
    {
        Guid owner = await AddUserAsync("Sahib2");
        await using RentoXDbContext db = database.CreateContext();
        var service = new FinanceService(db, new Clock());

        Assert.Equal("İbadulla Hüseynzadə", (await service.GetProfileAsync()).FullName);

        await service.SetProfileAsync(owner, new FinanceProfileDto("İbadulla Hüseynzadə", "1234567890", "+994501112233", "a@b.az", "Bakı"));
        FinanceProfileDto saved = await service.GetProfileAsync();
        Assert.Equal("1234567890", saved.Voen);
        Assert.Equal("Bakı", saved.Address);

        await Assert.ThrowsAsync<DomainException>(() => service.SetProfileAsync(owner, new FinanceProfileDto(" ", null, null, null, null)));
        await db.Database.ExecuteSqlRawAsync("DELETE FROM finance.profile");
    }

    private async Task<Guid> AddUserAsync(string name)
    {
        await using RentoXDbContext db = database.CreateContext();
        Guid id = Guid.NewGuid();
        string userName = $"fin-{id:N}";
        db.Users.Add(new AppUser
        {
            Id = id,
            UserName = userName,
            NormalizedUserName = userName.ToUpperInvariant(),
            PhoneNumber = "+99450" + Random.Shared.Next(1000000, 9999999),
            RegisteredAtUtc = Now,
            SecurityStamp = Guid.NewGuid().ToString("N"),
            ConcurrencyStamp = Guid.NewGuid().ToString("N")
        });
        db.Set<UserProfile>().Add(UserProfile.Create(id, name, PreferredLanguage.Azerbaijani));
        await db.SaveChangesAsync();
        return id;
    }

    private sealed class Clock : IClock
    {
        public DateTimeOffset UtcNow => Now;
    }
}
