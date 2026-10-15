using RentoX.Domain.Common.Exceptions;
using RentoX.Domain.Hr;

namespace RentoX.Domain.UnitTests.Hr;

public sealed class StaffHrTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 14, 8, 0, 0, TimeSpan.Zero);

    private static StaffProfile Profile() => StaffProfile.Create(Guid.NewGuid(), Now);

    [Fact]
    public void ANewProfileHasUsualHoursMondayToFridayAndThreeDaysOff()
    {
        StaffProfile p = Profile();

        Assert.True(p.LoginAllowed);
        Assert.Equal(540, p.WorkStartMinute);
        Assert.Equal(1080, p.WorkEndMinute);
        Assert.Equal(3, p.MonthlyDayOffLimit);
        Assert.True(p.Works(DayOfWeek.Monday));
        Assert.True(p.Works(DayOfWeek.Friday));
        Assert.False(p.Works(DayOfWeek.Saturday));
        Assert.False(p.Works(DayOfWeek.Sunday));
    }

    [Theory]
    [InlineData(-1, 540, 1080, 31, 3)]
    [InlineData(100, 600, 600, 31, 3)]
    [InlineData(100, 1000, 900, 31, 3)]
    [InlineData(100, 540, 1080, 128, 3)]
    [InlineData(100, 540, 1080, 31, 32)]
    public void AnInvalidProfileChangesNothing(int salary, int start, int end, int days, int limit)
    {
        StaffProfile p = Profile();

        Assert.Throws<DomainException>(() => p.Update(salary, false, start, end, days, limit, "x", Now));

        Assert.Equal(0, p.MonthlySalaryInQepik);
        Assert.True(p.LoginAllowed);
        Assert.Null(p.Note);
    }

    [Fact]
    public void ADayOnlyCountsWhenItIsAWorkingDay()
    {
        StaffProfile p = Profile();

        // Wednesday 14 October to Wednesday 21 October: six working days (a weekend between).
        Assert.Equal(6, HrTime.WorkingDaysBetween(new DateOnly(2026, 10, 14), new DateOnly(2026, 10, 21), p));
        // Saturday and Sunday only: none.
        Assert.Equal(0, HrTime.WorkingDaysBetween(new DateOnly(2026, 10, 17), new DateOnly(2026, 10, 18), p));
    }

    [Fact]
    public void BakuTimeIsFourHoursAheadOfUtc()
    {
        Assert.Equal(new DateOnly(2026, 10, 14), HrTime.BakuDay(new DateTimeOffset(2026, 10, 13, 20, 30, 0, TimeSpan.Zero)));
        Assert.Equal(30, HrTime.BakuMinute(new DateTimeOffset(2026, 10, 13, 20, 30, 0, TimeSpan.Zero)));
        Assert.Equal(new DateTimeOffset(2026, 10, 14, 5, 0, 0, TimeSpan.Zero), HrTime.FromBaku(new DateOnly(2026, 10, 14), 540));
    }

    [Fact]
    public void ADayIsStartedOnceFinishedOnceAndNotBeforeItStarted()
    {
        AttendanceRecord r = AttendanceRecord.Start(Guid.NewGuid(), new DateOnly(2026, 10, 14), Now);

        Assert.Throws<DomainException>(() => r.Finish(Now.AddMinutes(-1)));
        r.Finish(Now.AddHours(8));
        Assert.Equal(480, r.WorkedMinutes(Now.AddDays(1)));
        Assert.Throws<DomainException>(() => r.Finish(Now.AddHours(9)));
    }

    [Fact]
    public void ADayOffNeedsAWorkingDayAReasonAndIsDecidedOnce()
    {
        StaffProfile p = Profile();
        Guid user = p.UserId;
        DateOnly monday = new(2026, 10, 19);

        Assert.Throws<DomainException>(() => LeaveRequest.Create(user, new DateOnly(2026, 10, 17), new DateOnly(2026, 10, 18), 0, false, "Həkim", Now));
        Assert.Throws<DomainException>(() => LeaveRequest.Create(user, monday, monday, 1, false, "  ", Now));
        Assert.Throws<DomainException>(() => LeaveRequest.Create(user, monday, monday.AddDays(-1), 1, false, "Həkim", Now));

        LeaveRequest leave = LeaveRequest.Create(user, monday, monday.AddDays(1), 2, true, "Həkim", Now);
        Assert.False(leave.Covers(monday)); // not before it is approved

        leave.Decide(true, "Yaxşı", Guid.NewGuid(), Now);
        Assert.True(leave.Covers(monday));
        Assert.True(leave.Covers(monday.AddDays(1)));
        Assert.False(leave.Covers(monday.AddDays(2)));
        Assert.Throws<DomainException>(() => leave.Decide(false, null, Guid.NewGuid(), Now));
    }

    [Fact]
    public void ADayOffCanBeTakenBackWhileItIsAheadButNotOnceItHasStarted()
    {
        DateOnly monday = new(2026, 10, 19);
        LeaveRequest leave = LeaveRequest.Create(Guid.NewGuid(), monday, monday, 1, false, "Həkim", Now);
        leave.Decide(true, null, Guid.NewGuid(), Now);

        Assert.Throws<DomainException>(() => leave.Cancel(monday, Now));
        leave.Cancel(new DateOnly(2026, 10, 14), Now);
        Assert.Equal(LeaveStatus.Cancelled, leave.Status);
        Assert.False(leave.Covers(monday));
    }

    [Fact]
    public void APayLineNeedsAnAmountAndADeductionNeedsItsReasonAndAVoidedLineStaysVoid()
    {
        Guid user = Guid.NewGuid();
        Guid boss = Guid.NewGuid();
        DateOnly month = new(2026, 10, 20);

        Assert.Throws<DomainException>(() => PayrollEntry.Create(user, month, PayrollKind.Bonus, 0, null, boss, Now));
        Assert.Throws<DomainException>(() => PayrollEntry.Create(user, month, PayrollKind.Deduction, 500, " ", boss, Now));

        PayrollEntry line = PayrollEntry.Create(user, month, PayrollKind.Deduction, 500, "Gecikmə", boss, Now);
        Assert.Equal(new DateOnly(2026, 10, 1), line.Month);
        Assert.False(line.IsVoid);

        line.Void(boss, Now);
        Assert.True(line.IsVoid);
        Assert.Throws<DomainException>(() => line.Void(boss, Now));
    }

    [Fact]
    public void ARequestIsOpenPassedOnOnceAndAnsweredOnce()
    {
        Guid person = Guid.NewGuid();
        Guid watcher = Guid.NewGuid();

        Assert.Throws<DomainException>(() => StaffRequest.Create(person, " ", "Mətn", false, Now));
        Assert.Throws<DomainException>(() => StaffRequest.Create(person, "Mövzu", "", false, Now));

        StaffRequest request = StaffRequest.Create(person, "Mövzu", "Mətn", false, Now);
        Assert.Equal(StaffRequestStatus.Open, request.Status);
        Assert.False(request.ToOwner);

        request.Forward(watcher, "Sizə baxmaq lazımdır", Now);
        Assert.True(request.ToOwner);
        Assert.Equal(StaffRequestStatus.Forwarded, request.Status);
        Assert.Throws<DomainException>(() => request.Forward(watcher, null, Now));

        request.Resolve(true, "Olar", Guid.NewGuid(), Now);
        Assert.Equal(StaffRequestStatus.Answered, request.Status);
        Assert.Throws<DomainException>(() => request.Resolve(false, "Yox", Guid.NewGuid(), Now));
    }
}
