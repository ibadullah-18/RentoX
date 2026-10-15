using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using RentoX.Application.Abstractions.Time;
using RentoX.Application.Authorization;
using RentoX.Application.Hr;
using RentoX.Application.Notifications;
using RentoX.Domain.Auditing;
using RentoX.Domain.Authentication;
using RentoX.Domain.Common.Exceptions;
using RentoX.Domain.Hr;
using RentoX.Domain.Users;
using RentoX.Domain.Users.Enums;
using RentoX.Infrastructure.Hr;
using RentoX.Infrastructure.Identity;
using RentoX.Infrastructure.Persistence;
using RentoX.IntegrationTests.Auditing;

namespace RentoX.IntegrationTests.Moderation;

/// <summary>The staff: coming to work, days off, pay, requests, and the owner closing a person's access.</summary>
public sealed class StaffHrTests(AuditPostgresFixture database)
    : IClassFixture<AuditPostgresFixture>
{
    // Wednesday 14 October 2026, 12:00 in Baku (08:00 UTC): the working day began at 09:00.
    private static readonly DateTimeOffset NoonInBaku = new(2026, 10, 14, 8, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task TheStaffListHasTheAdminsWithUsualHoursAndNotTheOwner()
    {
        Guid boss = await AddUserAsync(RoleNames.SuperAdmin);
        Guid admin = await AddUserAsync(RoleNames.Admin);

        IReadOnlyList<StaffProfileDto> staff = await Service().ListStaffAsync();

        StaffProfileDto mine = staff.Single(s => s.UserId == admin);
        Assert.True(mine.LoginAllowed);
        Assert.Equal(9 * 60, mine.Schedule.WorkStartMinute);
        Assert.Equal(18 * 60, mine.Schedule.WorkEndMinute);
        Assert.Equal(3, mine.Schedule.MonthlyDayOffLimit);
        Assert.DoesNotContain(staff, s => s.UserId == boss);
    }

    [Fact]
    public async Task StartingAfterTheGraceTimeIsLateStartingTwiceIsRefusedAndFinishingCountsTheTime()
    {
        Guid admin = await AddUserAsync(RoleNames.Admin);
        var clock = new MutableClock(new DateTimeOffset(2026, 10, 14, 5, 40, 0, TimeSpan.Zero)); // 09:40 in Baku
        StaffHrService service = Service(clock);

        MyDay started = await service.CheckInAsync(admin);
        Assert.Equal(DayStatus.Late, started.Status);
        await Assert.ThrowsAsync<DomainException>(() => service.CheckInAsync(admin));

        clock.Now = new DateTimeOffset(2026, 10, 14, 14, 40, 0, TimeSpan.Zero); // 18:40, nine hours later
        MyDay finished = await service.CheckOutAsync(admin);
        Assert.Equal(540, finished.WorkedMinutes);
        await Assert.ThrowsAsync<DomainException>(() => service.CheckOutAsync(admin));
    }

    [Fact]
    public async Task TheBoardShowsAWorkingDayWithoutAStartInRedAndALeaveDayAsLeave()
    {
        Guid boss = await AddUserAsync(RoleNames.SuperAdmin);
        Guid absent = await AddUserAsync(RoleNames.Admin);
        Guid present = await AddUserAsync(RoleNames.Admin);
        Guid away = await AddUserAsync(RoleNames.Admin);

        // A day off asked a day earlier and approved.
        var yesterday = new MutableClock(new DateTimeOffset(2026, 10, 13, 8, 0, 0, TimeSpan.Zero));
        StaffHrService early = Service(yesterday);
        LeaveRequestDto leave = await early.RequestLeaveAsync(away, new DateOnly(2026, 10, 14), new DateOnly(2026, 10, 14), "Həkim");
        await early.DecideLeaveAsync(boss, leave.Id, approve: true, "Yaxşı");

        var morning = new MutableClock(new DateTimeOffset(2026, 10, 14, 5, 0, 0, TimeSpan.Zero)); // 09:00
        await Service(morning).CheckInAsync(present);

        StaffHrService now = Service(new MutableClock(NoonInBaku));
        IReadOnlyList<DayBoardRow> board = await now.GetDayBoardAsync(new DateOnly(2026, 10, 14));

        Assert.Equal(DayStatus.Absent, board.Single(r => r.UserId == absent).Status);
        Assert.Equal(DayStatus.Present, board.Single(r => r.UserId == present).Status);
        Assert.Equal(DayStatus.OnLeave, board.Single(r => r.UserId == away).Status);

        // A Saturday is not a working day.
        IReadOnlyList<DayBoardRow> saturday = await now.GetDayBoardAsync(new DateOnly(2026, 10, 17));
        Assert.Equal(DayStatus.DayOff, saturday.Single(r => r.UserId == absent).Status);

        // The month table has one colour a day and counts the absences.
        MonthSheet sheet = await now.GetMonthSheetAsync(2026, 10);
        MonthSheetRow row = sheet.Rows.Single(r => r.UserId == absent);
        Assert.Equal(31, sheet.DaysInMonth);
        Assert.Equal(DayStatus.Absent, row.Days[13]);   // 14 October
        Assert.Equal(DayStatus.Future, row.Days[14]);   // 15 October
        Assert.True(row.Absent >= 1);
    }

    [Fact]
    public async Task ADayOffIsAskedInAdvanceCannotOverlapAndPastTheMonthlyLimitTheOwnerIsToldSoAndTheAnswerGoesBack()
    {
        Guid boss = await AddUserAsync(RoleNames.SuperAdmin);
        Guid admin = await AddUserAsync(RoleNames.Admin);
        var told = new Recorder();
        StaffHrService service = Service(new MutableClock(NoonInBaku), told);

        // Today and the past are refused.
        await Assert.ThrowsAsync<DomainException>(() =>
            service.RequestLeaveAsync(admin, new DateOnly(2026, 10, 14), new DateOnly(2026, 10, 14), "Bu gün"));

        // Mon 19 to Thu 22 October is four working days: more than the three a month.
        LeaveRequestDto big = await service.RequestLeaveAsync(admin, new DateOnly(2026, 10, 19), new DateOnly(2026, 10, 22), "Ailə işi");
        Assert.True(big.OverLimit);
        Assert.Equal(4, big.WorkingDays);
        Assert.Contains(told.Sent, n => n.UserId == boss && n.Title.Contains("limit", StringComparison.OrdinalIgnoreCase));

        // It overlaps the first one.
        await Assert.ThrowsAsync<DomainException>(() =>
            service.RequestLeaveAsync(admin, new DateOnly(2026, 10, 21), new DateOnly(2026, 10, 21), "Üst-üstə"));

        MyDay mine = await service.GetMyDayAsync(admin);
        Assert.Equal(4, mine.DayOffUsedThisMonth);

        // The owner approves: the person is told, and it is in the audit log.
        told.Sent.Clear();
        LeaveRequestDto decided = await service.DecideLeaveAsync(boss, big.Id, approve: true, "Təsdiq");
        Assert.Equal((int)LeaveStatus.Approved, decided.Status);
        Assert.Contains(told.Sent, n => n.UserId == admin && n.Title.Contains("təsdiq", StringComparison.OrdinalIgnoreCase));
        await using RentoXDbContext db = database.CreateContext();
        Assert.True(await db.Set<AuditLogEntry>().AnyAsync(a => a.Action == AuditAction.StaffHrChanged && a.TargetId == admin && a.CurrentValue == 5));

        // Decided requests cannot be decided again.
        await Assert.ThrowsAsync<DomainException>(() => service.DecideLeaveAsync(boss, big.Id, approve: false, null));
    }

    [Fact]
    public async Task ThePayOfAMonthIsBasePlusBonusMinusDeductionsAdvancesAndPaymentsAndAVoidedLineStopsCounting()
    {
        Guid boss = await AddUserAsync(RoleNames.SuperAdmin);
        Guid admin = await AddUserAsync(RoleNames.Admin);
        var told = new Recorder();
        StaffHrService service = Service(new MutableClock(NoonInBaku), told);
        await service.UpdateProfileAsync(boss, admin, new UpdateStaffProfileCommand(100000, true, 540, 1080, 31, 3, null)); // 1000.00 AZN
        var month = new DateOnly(2026, 10, 1);

        // A deduction needs its reason.
        await Assert.ThrowsAsync<DomainException>(() =>
            service.AddPayrollLineAsync(boss, admin, month, (int)PayrollKind.Deduction, 5000, null));

        await service.AddPayrollLineAsync(boss, admin, month, (int)PayrollKind.Deduction, 5000, "Gecikmə");
        await service.AddPayrollLineAsync(boss, admin, month, (int)PayrollKind.Advance, 20000, null);
        PayrollSheetDto sheet = await service.AddPayrollLineAsync(boss, admin, month, (int)PayrollKind.Bonus, 10000, "Mükafat");

        Assert.Equal(100000, sheet.BaseInQepik);
        Assert.Equal(5000, sheet.DeductionsInQepik);
        Assert.Equal(20000, sheet.AdvancesInQepik);
        Assert.Equal(10000, sheet.BonusInQepik);
        Assert.Equal(85000, sheet.RemainingInQepik);

        // More than what is left cannot be paid out.
        await Assert.ThrowsAsync<DomainException>(() =>
            service.AddPayrollLineAsync(boss, admin, month, (int)PayrollKind.Payment, 85001, null));

        PayrollSheetDto paid = await service.AddPayrollLineAsync(boss, admin, month, (int)PayrollKind.Payment, 85000, null);
        Assert.Equal(0, paid.RemainingInQepik);
        Assert.Contains(told.Sent, n => n.UserId == admin && n.Title.Contains("kəsinti", StringComparison.OrdinalIgnoreCase));

        // A mistake is voided, not erased.
        PayrollLineDto payment = paid.Lines.Single(l => l.Kind == (int)PayrollKind.Payment);
        PayrollSheetDto after = await service.VoidPayrollLineAsync(boss, payment.Id);
        Assert.Equal(85000, after.RemainingInQepik);
        Assert.Contains(after.Lines, l => l.IsVoid);
        await using RentoXDbContext db = database.CreateContext();
        Assert.True(await db.Set<AuditLogEntry>().CountAsync(a => a.Action == AuditAction.StaffHrChanged && a.TargetId == admin && a.CurrentValue == 3) == 4);
    }

    [Fact]
    public async Task ClosingAccessStopsThePersonEndsTheirSessionsAndIsRecordedAndOpeningItAgainWorks()
    {
        Guid boss = await AddUserAsync(RoleNames.SuperAdmin);
        Guid admin = await AddUserAsync(RoleNames.Admin);
        await using (RentoXDbContext seed = database.CreateContext())
        {
            seed.RefreshTokens.Add(RefreshToken.Create(admin, Guid.NewGuid().ToString("N"), NoonInBaku.AddMinutes(-5), TimeSpan.FromDays(30)));
            await seed.SaveChangesAsync();
        }

        StaffHrService service = Service(new MutableClock(NoonInBaku));
        Assert.True(await service.IsAccessAllowedAsync(admin));

        await service.UpdateProfileAsync(boss, admin, new UpdateStaffProfileCommand(0, false, 540, 1080, 31, 3, "Müvəqqəti"));

        Assert.False(await service.IsAccessAllowedAsync(admin));
        await using (RentoXDbContext db = database.CreateContext())
        {
            Assert.Empty(await db.RefreshTokens.Where(t => t.UserId == admin && t.RevokedAtUtc == null).ToListAsync());
            Assert.True(await db.Set<AuditLogEntry>().AnyAsync(a => a.Action == AuditAction.StaffHrChanged && a.TargetId == admin && a.CurrentValue == 2));
        }

        // Someone shut out cannot start the day either.
        await Assert.ThrowsAsync<DomainException>(() => service.CheckInAsync(admin));

        await service.UpdateProfileAsync(boss, admin, new UpdateStaffProfileCommand(0, true, 540, 1080, 31, 3, null));
        Assert.True(await service.IsAccessAllowedAsync(admin));
        // A person with no profile at all (the owner, anyone else) was never shut out.
        Assert.True(await service.IsAccessAllowedAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task SomeoneWhoDidNotStartIsReportedOncePerDayButNotWhenTheyStartedOrAreOnLeaveOrItIsTooEarly()
    {
        Guid boss = await AddUserAsync(RoleNames.SuperAdmin);
        Guid watcher = await AddUserAsync(RoleNames.Admin, AdminSections.Hr);
        Guid lazy = await AddUserAsync(RoleNames.Admin);
        Guid early = await AddUserAsync(RoleNames.Admin);
        var told = new Recorder();

        // 09:10: still inside the grace time.
        var clock = new MutableClock(new DateTimeOffset(2026, 10, 14, 5, 10, 0, TimeSpan.Zero));
        StaffHrService service = Service(clock, told);
        await service.CheckInAsync(early);
        Assert.Equal(0, await service.CheckAbsencesAsync());

        // 09:30: the one who did not start is reported to the owner and to the watcher.
        clock.Now = new DateTimeOffset(2026, 10, 14, 5, 30, 0, TimeSpan.Zero);
        int reported = await service.CheckAbsencesAsync();
        Assert.True(reported >= 1);
        Assert.Contains(told.Sent, n => n.UserId == boss && n.Title.Contains("başlamayıb", StringComparison.Ordinal) && n.Body.Contains("Test", StringComparison.Ordinal));
        Assert.Contains(told.Sent, n => n.UserId == watcher && n.RelatedEntityId == lazy);
        Assert.DoesNotContain(told.Sent, n => n.RelatedEntityId == early);

        // The same day, never twice.
        told.Sent.Clear();
        Assert.Equal(0, await service.CheckAbsencesAsync());
        Assert.Empty(told.Sent);
    }

    [Fact]
    public async Task ARequestGoesToTheWatcherWhoAnswersOrPassesItOnAndWhatIsSentToTheOwnerIsTheOwnersAlone()
    {
        Guid boss = await AddUserAsync(RoleNames.SuperAdmin);
        Guid watcher = await AddUserAsync(RoleNames.Admin, AdminSections.Hr);
        Guid admin = await AddUserAsync(RoleNames.Admin);
        var told = new Recorder();
        StaffHrService service = Service(new MutableClock(NoonInBaku), told);

        StaffRequestDto normal = await service.SendRequestAsync(admin, "Kompüter", "Yeni klaviatura lazımdır", toOwner: false);
        Assert.Contains(told.Sent, n => n.UserId == watcher);
        Assert.DoesNotContain(told.Sent, n => n.UserId == boss);

        StaffRequestDto direct = await service.SendRequestAsync(admin, "Maaş", "Avans istəyirəm", toOwner: true);
        Assert.Contains(told.Sent, n => n.UserId == boss && n.RelatedEntityId == direct.Id);

        // The watcher cannot decide what is for the owner.
        await Assert.ThrowsAsync<DomainException>(() => service.HandleRequestAsync(watcher, direct.Id, 1, "Bəli", actorIsOwner: false));

        // The watcher passes the first one on; the owner answers it; the person is told.
        StaffRequestDto passed = await service.HandleRequestAsync(watcher, normal.Id, 3, "Sizə baxmaq lazımdır", actorIsOwner: false);
        Assert.Equal((int)StaffRequestStatus.Forwarded, passed.Status);
        told.Sent.Clear();
        StaffRequestDto answered = await service.HandleRequestAsync(boss, normal.Id, 1, "Alınacaq", actorIsOwner: true);
        Assert.Equal((int)StaffRequestStatus.Answered, answered.Status);
        Assert.Contains(told.Sent, n => n.UserId == admin);

        IReadOnlyList<StaffRequestDto> mine = await service.GetMyRequestsAsync(admin);
        Assert.Equal(2, mine.Count);
        Assert.Contains((await service.ListRequestsAsync(forOwner: true, null)), r => r.Id == direct.Id);
        Assert.DoesNotContain((await service.ListRequestsAsync(forOwner: false, null)), r => r.Id == direct.Id);
    }

    // ---------------------------------------------------------------- seeding

    private StaffHrService Service(MutableClock? clock = null, Recorder? told = null) =>
        new(database.CreateContext(), clock ?? new MutableClock(NoonInBaku), new MemoryCache(new MemoryCacheOptions()), told ?? new Recorder());

    private async Task<Guid> AddUserAsync(string role, string? section = null)
    {
        await using RentoXDbContext db = database.CreateContext();
        Guid id = Guid.NewGuid();
        string userName = $"hr-{id:N}";
        db.Users.Add(new AppUser
        {
            Id = id,
            UserName = userName,
            NormalizedUserName = userName.ToUpperInvariant(),
            PhoneNumber = "+99450" + Random.Shared.Next(1000000, 9999999),
            RegisteredAtUtc = NoonInBaku,
            SecurityStamp = Guid.NewGuid().ToString("N"),
            ConcurrencyStamp = Guid.NewGuid().ToString("N")
        });
        db.Set<UserProfile>().Add(UserProfile.Create(id, "Test " + id.ToString("N")[..6], PreferredLanguage.Azerbaijani));
        await db.SaveChangesAsync();

        AppRole? existing = await db.Roles.SingleOrDefaultAsync(r => r.Name == role);
        if (existing is null)
        {
            existing = new AppRole
            {
                Id = Guid.NewGuid(),
                Name = role,
                NormalizedName = role.ToUpperInvariant(),
                ConcurrencyStamp = Guid.NewGuid().ToString("N")
            };
            db.Roles.Add(existing);
        }

        db.Set<IdentityUserRole<Guid>>().Add(new IdentityUserRole<Guid> { UserId = id, RoleId = existing.Id });
        if (section is not null)
        {
            db.Set<IdentityUserClaim<Guid>>().Add(new IdentityUserClaim<Guid> { UserId = id, ClaimType = AdminSections.ClaimType, ClaimValue = section });
        }

        await db.SaveChangesAsync();
        return id;
    }

    private sealed class MutableClock(DateTimeOffset now) : IClock
    {
        public DateTimeOffset Now { get; set; } = now;

        public DateTimeOffset UtcNow => Now;
    }

    private sealed class Recorder : INotificationService
    {
        public List<CreateNotificationCommand> Sent { get; } = [];

        public Task<NotificationResult> CreateAsync(CreateNotificationCommand command, CancellationToken cancellationToken = default)
        {
            Sent.Add(command);
            return Task.FromResult<NotificationResult>(null!);
        }

        public Task<RentoX.Application.Common.PagedResult<NotificationResult>> GetAsync(
            Guid userId, int page, int pageSize, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<int> GetUnreadCountAsync(Guid userId, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<bool> MarkAsReadAsync(Guid userId, Guid notificationId, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<int> MarkAllAsReadAsync(Guid userId, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }
}
