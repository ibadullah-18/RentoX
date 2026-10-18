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
using RentoX.Domain.Notifications;
using RentoX.Domain.Users;
using RentoX.Infrastructure.Persistence;

namespace RentoX.Infrastructure.Hr;

public sealed class StaffHrService(
    RentoXDbContext dbContext,
    IClock clock,
    IMemoryCache cache,
    INotificationService notifications)
    : IStaffHrService
{
    private static readonly TimeSpan Keep = TimeSpan.FromSeconds(20);

    // The kinds of change written in the audit log (see AuditAction.StaffHrChanged).
    private const int AuditProfile = 1;
    private const int AuditAccess = 2;
    private const int AuditPayLine = 3;
    private const int AuditPayVoid = 4;
    private const int AuditLeave = 5;
    private const int AuditRequest = 6;

    private sealed record Person(Guid Id, string FullName, string? Phone);

    // ------------------------------------------------------------------ the staff member

    public async Task<MyDay> GetMyDayAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        DateTimeOffset now = clock.UtcNow;
        DateOnly today = HrTime.BakuDay(now);
        // An Admin gets the usual hours the first time they look; anyone else is not on the staff.
        StaffProfile? profile = await FindProfileAsync(userId, cancellationToken);
        if (profile is null && (await RosterAsync(cancellationToken)).Any(p => p.Id == userId))
        {
            profile = await RequireProfileAsync(userId, cancellationToken);
        }

        if (profile is null)
        {
            return new MyDay(
                false, today, DayStatus.DayOff,
                Schedule(null), null, null, 0, 0);
        }

        AttendanceRecord? record = await dbContext.Set<AttendanceRecord>()
            .AsNoTracking()
            .SingleOrDefaultAsync(r => r.UserId == userId && r.Day == today, cancellationToken);
        bool onLeave = await dbContext.Set<LeaveRequest>()
            .AsNoTracking()
            .AnyAsync(l => l.UserId == userId && l.Status == LeaveStatus.Approved && l.FromDay <= today && l.ToDay >= today, cancellationToken);

        int used = await UsedInMonthAsync(userId, profile, today.Year, today.Month, null, cancellationToken);

        return new MyDay(
            true,
            today,
            StatusOf(profile, today, record, onLeave, now),
            Schedule(profile),
            record?.CheckInAtUtc,
            record?.CheckOutAtUtc,
            record?.WorkedMinutes(now) ?? 0,
            used,
            (await WatchersAsync(cancellationToken)).Count > 0);
    }

    public async Task<MyDay> CheckInAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        StaffProfile profile = await RequireProfileAsync(userId, cancellationToken);
        if (!profile.LoginAllowed)
        {
            throw new DomainException("Your access to the panel is closed.");
        }

        DateTimeOffset now = clock.UtcNow;
        DateOnly today = HrTime.BakuDay(now);

        bool already = await dbContext.Set<AttendanceRecord>()
            .AnyAsync(r => r.UserId == userId && r.Day == today, cancellationToken);
        if (already)
        {
            throw new DomainException("You have already started today.");
        }

        dbContext.Set<AttendanceRecord>().Add(AttendanceRecord.Start(userId, today, now));
        await dbContext.SaveChangesAsync(cancellationToken);
        return await GetMyDayAsync(userId, cancellationToken);
    }

    public async Task<MyDay> CheckOutAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        await RequireProfileAsync(userId, cancellationToken);
        DateTimeOffset now = clock.UtcNow;
        DateOnly today = HrTime.BakuDay(now);

        AttendanceRecord record = await dbContext.Set<AttendanceRecord>()
            .SingleOrDefaultAsync(r => r.UserId == userId && r.Day == today, cancellationToken)
            ?? throw new DomainException("You have not started today.");

        record.Finish(now);
        await dbContext.SaveChangesAsync(cancellationToken);
        return await GetMyDayAsync(userId, cancellationToken);
    }

    public async Task<LeaveRequestDto> RequestLeaveAsync(
        Guid userId, DateOnly firstDay, DateOnly lastDay, string reason, CancellationToken cancellationToken = default)
    {
        StaffProfile profile = await RequireProfileAsync(userId, cancellationToken);
        DateTimeOffset now = clock.UtcNow;
        DateOnly today = HrTime.BakuDay(now);

        if (firstDay <= today)
        {
            throw new DomainException("A day off is asked for in advance: choose tomorrow or a later day.");
        }

        if (lastDay < firstDay)
        {
            throw new DomainException("The last day cannot be before the first day.");
        }

        bool overlaps = await dbContext.Set<LeaveRequest>().AnyAsync(
            l => l.UserId == userId &&
                 (l.Status == LeaveStatus.Pending || l.Status == LeaveStatus.Approved) &&
                 l.FromDay <= lastDay && l.ToDay >= firstDay,
            cancellationToken);
        if (overlaps)
        {
            throw new DomainException("You already have a request for some of these days.");
        }

        int working = HrTime.WorkingDaysBetween(firstDay, lastDay, profile);

        // Past the monthly limit in any month the request touches: it is still sent, but marked for the owner.
        bool over = false;
        for (DateOnly month = new(firstDay.Year, firstDay.Month, 1); month <= lastDay; month = month.AddMonths(1))
        {
            int inThisMonth = CountInMonth(firstDay, lastDay, month, profile);
            if (inThisMonth == 0)
            {
                continue;
            }

            int used = await UsedInMonthAsync(userId, profile, month.Year, month.Month, null, cancellationToken);
            if (used + inThisMonth > profile.MonthlyDayOffLimit)
            {
                over = true;
            }
        }

        LeaveRequest leave = LeaveRequest.Create(userId, firstDay, lastDay, working, over, reason, now);
        dbContext.Set<LeaveRequest>().Add(leave);
        await dbContext.SaveChangesAsync(cancellationToken);

        string name = await NameOfAsync(userId, cancellationToken);
        string period = firstDay == lastDay ? $"{firstDay:dd.MM.yyyy}" : $"{firstDay:dd.MM.yyyy} – {lastDay:dd.MM.yyyy}";
        await NotifyAsync(
            await OwnersAsync(cancellationToken),
            NotificationType.StaffLeaveRequested,
            over ? "İcazə sorğusu: aylıq limit aşılır" : "Yeni icazə sorğusu",
            $"{name}: {period} ({working} iş günü). Səbəb: {leave.Reason}",
            leave.Id,
            "/hr/leaves",
            cancellationToken);

        return ToDto(leave, name, profile.MonthlyDayOffLimit);
    }

    public async Task CancelLeaveAsync(Guid userId, Guid leaveId, CancellationToken cancellationToken = default)
    {
        LeaveRequest leave = await dbContext.Set<LeaveRequest>()
            .SingleOrDefaultAsync(l => l.Id == leaveId && l.UserId == userId, cancellationToken)
            ?? throw new DomainException("The request was not found.");

        DateTimeOffset now = clock.UtcNow;
        leave.Cancel(HrTime.BakuDay(now), now);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<LeaveRequestDto>> GetMyLeavesAsync(
        Guid userId, CancellationToken cancellationToken = default)
    {
        StaffProfile? profile = await FindProfileAsync(userId, cancellationToken);
        string name = await NameOfAsync(userId, cancellationToken);
        List<LeaveRequest> rows = await dbContext.Set<LeaveRequest>()
            .AsNoTracking()
            .Where(l => l.UserId == userId)
            .OrderByDescending(l => l.FromDay)
            .Take(60)
            .ToListAsync(cancellationToken);

        return rows.Select(l => ToDto(l, name, profile?.MonthlyDayOffLimit ?? 0)).ToArray();
    }

    public async Task<PayrollSheetDto> GetPayrollAsync(
        Guid userId, DateOnly month, CancellationToken cancellationToken = default)
    {
        DateOnly first = new(month.Year, month.Month, 1);
        StaffProfile? profile = await FindProfileAsync(userId, cancellationToken);
        string name = await NameOfAsync(userId, cancellationToken);

        List<PayrollEntry> entries = await dbContext.Set<PayrollEntry>()
            .AsNoTracking()
            .Where(e => e.UserId == userId && e.Month == first)
            .OrderByDescending(e => e.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        Dictionary<Guid, string> names = await NamesAsync(entries.Select(e => e.CreatedByUserId), cancellationToken);

        int Sum(PayrollKind kind) => entries.Where(e => !e.IsVoid && e.Kind == kind).Sum(e => e.AmountInQepik);

        // The pay in force in THAT month (a later raise does not rewrite earlier months).
        List<StaffSalaryChange> salaryHistory = await dbContext.Set<StaffSalaryChange>()
            .AsNoTracking()
            .Where(c => c.UserId == userId)
            .ToListAsync(cancellationToken);
        int baseAmount = profile is null ? 0 : StaffSalaryChange.For(salaryHistory, first);
        int bonus = Sum(PayrollKind.Bonus);
        int deductions = Sum(PayrollKind.Deduction);
        int advances = Sum(PayrollKind.Advance);
        int payments = Sum(PayrollKind.Payment);

        return new PayrollSheetDto(
            userId,
            name,
            first,
            baseAmount,
            bonus,
            deductions,
            advances,
            payments,
            baseAmount + bonus - deductions - advances - payments,
            entries.Select(e => new PayrollLineDto(
                e.Id, (int)e.Kind, e.AmountInQepik, e.Note, e.CreatedAtUtc,
                names.GetValueOrDefault(e.CreatedByUserId), e.IsVoid)).ToArray());
    }

    public async Task<PayrollHistoryDto> GetPayrollHistoryAsync(
        Guid userId, int months, CancellationToken cancellationToken = default)
    {
        months = Math.Clamp(months, 1, 36);
        string name = await NameOfAsync(userId, cancellationToken);

        DateOnly today = DateOnly.FromDateTime(clock.UtcNow.UtcDateTime);
        DateOnly current = new(today.Year, today.Month, 1);
        DateOnly from = current.AddMonths(-(months - 1));

        List<StaffSalaryChange> changes = await dbContext.Set<StaffSalaryChange>()
            .AsNoTracking()
            .Where(c => c.UserId == userId)
            .OrderBy(c => c.EffectiveFrom).ThenBy(c => c.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        List<PayrollEntry> entries = await dbContext.Set<PayrollEntry>()
            .AsNoTracking()
            .Where(e => e.UserId == userId && e.Month >= from && e.Month <= current)
            .ToListAsync(cancellationToken);

        // The first month with any pay or line: months before it say nothing, so they are not shown as empty rows.
        DateOnly? firstKnown = changes.Count > 0 ? changes[0].EffectiveFrom : null;
        if (entries.Count > 0)
        {
            DateOnly firstLine = entries.Min(e => e.Month);
            firstKnown = firstKnown is null || firstLine < firstKnown ? firstLine : firstKnown;
        }

        List<PayrollMonthDto> rows = [];
        for (DateOnly month = current; month >= from; month = month.AddMonths(-1))
        {
            if (firstKnown is null || month < firstKnown)
            {
                break;
            }

            List<PayrollEntry> of = entries.Where(e => e.Month == month).ToList();
            int Sum(PayrollKind kind) => of.Where(e => !e.IsVoid && e.Kind == kind).Sum(e => e.AmountInQepik);

            int baseAmount = StaffSalaryChange.For(changes, month);
            int bonus = Sum(PayrollKind.Bonus);
            int deductions = Sum(PayrollKind.Deduction);
            int advances = Sum(PayrollKind.Advance);
            int payments = Sum(PayrollKind.Payment);

            rows.Add(new PayrollMonthDto(
                month, baseAmount, bonus, deductions, advances, payments,
                baseAmount + bonus - deductions - advances - payments,
                of.Count(e => !e.IsVoid), of.Count(e => e.IsVoid)));
        }

        Dictionary<Guid, string> names = await NamesAsync(changes.Select(c => c.CreatedByUserId), cancellationToken);
        List<SalaryChangeDto> changeRows = [];
        for (int i = 0; i < changes.Count; i++)
        {
            changeRows.Add(new SalaryChangeDto(
                changes[i].EffectiveFrom,
                changes[i].AmountInQepik,
                i == 0 ? null : changes[i - 1].AmountInQepik,
                names.GetValueOrDefault(changes[i].CreatedByUserId),
                changes[i].CreatedAtUtc));
        }

        changeRows.Reverse();

        return new PayrollHistoryDto(
            userId,
            name,
            rows.Count,
            rows.Sum(r => r.BaseInQepik),
            rows.Sum(r => r.BonusInQepik),
            rows.Sum(r => r.DeductionsInQepik),
            rows.Sum(r => r.AdvancesInQepik),
            rows.Sum(r => r.PaymentsInQepik),
            rows.Sum(r => r.RemainingInQepik),
            rows,
            changeRows);
    }

    public async Task<StaffRequestDto> SendRequestAsync(
        Guid userId, string subject, string body, bool toOwner, CancellationToken cancellationToken = default)
    {
        await RequireProfileAsync(userId, cancellationToken);
        StaffRequest request = StaffRequest.Create(userId, subject, body, toOwner, clock.UtcNow);
        dbContext.Set<StaffRequest>().Add(request);
        await dbContext.SaveChangesAsync(cancellationToken);

        string name = await NameOfAsync(userId, cancellationToken);
        IReadOnlyList<Guid> to = toOwner
            ? await OwnersAsync(cancellationToken)
            : await WatchersOrOwnersAsync(cancellationToken);
        await NotifyAsync(
            to,
            NotificationType.StaffRequestReceived,
            "İşçidən yeni müraciət",
            $"{name}: {request.Subject}",
            request.Id,
            "/hr/requests",
            cancellationToken);

        return ToDto(request, name, null);
    }

    public async Task<IReadOnlyList<StaffRequestDto>> GetMyRequestsAsync(
        Guid userId, CancellationToken cancellationToken = default)
    {
        List<StaffRequest> rows = await dbContext.Set<StaffRequest>()
            .AsNoTracking()
            .Where(r => r.FromUserId == userId)
            .OrderByDescending(r => r.CreatedAtUtc)
            .Take(60)
            .ToListAsync(cancellationToken);

        return await RequestDtosAsync(rows, cancellationToken);
    }

    // ------------------------------------------------------------------ looking after the staff

    public async Task<IReadOnlyList<StaffProfileDto>> ListStaffAsync(CancellationToken cancellationToken = default)
    {
        List<Person> people = await RosterAsync(cancellationToken);
        Dictionary<Guid, StaffProfile> profiles = await ProfilesAsync(people, cancellationToken);
        List<(Guid UserId, string Value)> chosen = (await dbContext.Set<IdentityUserClaim<Guid>>()
            .AsNoTracking()
            .Where(c => c.ClaimType == AdminSections.ClaimType)
            .Select(c => new { c.UserId, c.ClaimValue })
            .ToListAsync(cancellationToken))
            .Select(c => (c.UserId, c.ClaimValue ?? string.Empty))
            .ToList();

        return people
            .Select(p => ToDto(p, profiles[p.Id], chosen.Where(c => c.UserId == p.Id).Select(c => c.Value).Order().ToArray()))
            .ToArray();
    }

    public async Task<StaffProfileDto?> GetProfileAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<StaffProfileDto> all = await ListStaffAsync(cancellationToken);
        return all.FirstOrDefault(p => p.UserId == userId);
    }

    public async Task<StaffProfileDto> UpdateProfileAsync(
        Guid actorUserId, Guid userId, UpdateStaffProfileCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        List<Person> people = await RosterAsync(cancellationToken);
        if (people.All(p => p.Id != userId))
        {
            throw new DomainException("This person is not a staff member.");
        }

        DateTimeOffset now = clock.UtcNow;
        StaffProfile profile = await RequireProfileAsync(userId, cancellationToken);
        bool wasAllowed = profile.LoginAllowed;
        bool salaryChanged = profile.MonthlySalaryInQepik != command.MonthlySalaryInQepik;

        profile.Update(
            command.MonthlySalaryInQepik,
            command.LoginAllowed,
            command.WorkStartMinute,
            command.WorkEndMinute,
            command.WorkDaysMask,
            command.MonthlyDayOffLimit,
            command.Note,
            now);

        // A new salary counts from this month on; the months before keep the pay they had.
        if (salaryChanged)
        {
            DateOnly today = HrTime.BakuDay(now);
            dbContext.Set<StaffSalaryChange>().Add(StaffSalaryChange.Create(
                userId, new DateOnly(today.Year, today.Month, 1), command.MonthlySalaryInQepik, actorUserId, now));
        }

        // One entry per kind of change (the log keeps one entry per operation, action and person).
        dbContext.Set<AuditLogEntry>().Add(AuditLogEntry.ForStaffHr(Guid.NewGuid(), actorUserId, userId, AuditProfile, profile.Id, now));
        if (wasAllowed != profile.LoginAllowed)
        {
            dbContext.Set<AuditLogEntry>().Add(AuditLogEntry.ForStaffHr(Guid.NewGuid(), actorUserId, userId, AuditAccess, profile.Id, now));
        }

        // Closing the access ends the person's sessions at once.
        if (wasAllowed && !profile.LoginAllowed)
        {
            List<RefreshToken> tokens = await dbContext.RefreshTokens
                .Where(t => t.UserId == userId && t.RevokedAtUtc == null && t.ExpiresAtUtc > now)
                .ToListAsync(cancellationToken);
            foreach (RefreshToken token in tokens)
            {
                token.Revoke(now);
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        cache.Remove(AccessKey(userId));

        return (await GetProfileAsync(userId, cancellationToken))!;
    }

    public async Task<IReadOnlyList<DayBoardRow>> GetDayBoardAsync(DateOnly day, CancellationToken cancellationToken = default)
    {
        DateTimeOffset now = clock.UtcNow;
        List<Person> people = await RosterAsync(cancellationToken);
        Dictionary<Guid, StaffProfile> profiles = await ProfilesAsync(people, cancellationToken);

        Dictionary<Guid, AttendanceRecord> records = (await dbContext.Set<AttendanceRecord>()
            .AsNoTracking()
            .Where(r => r.Day == day)
            .ToListAsync(cancellationToken))
            .ToDictionary(r => r.UserId);

        List<LeaveRequest> leaves = await dbContext.Set<LeaveRequest>()
            .AsNoTracking()
            .Where(l => l.Status == LeaveStatus.Approved && l.FromDay <= day && l.ToDay >= day)
            .ToListAsync(cancellationToken);

        return people.Select(p =>
        {
            StaffProfile profile = profiles[p.Id];
            records.TryGetValue(p.Id, out AttendanceRecord? record);
            bool onLeave = leaves.Any(l => l.UserId == p.Id);
            return new DayBoardRow(
                p.Id,
                p.FullName,
                StatusOf(profile, day, record, onLeave, now),
                record?.CheckInAtUtc,
                record?.CheckOutAtUtc,
                record?.WorkedMinutes(now) ?? 0,
                profile.WorkStartMinute,
                profile.WorkEndMinute,
                profile.LoginAllowed);
        })
        .OrderBy(r => r.FullName)
        .ToArray();
    }

    public async Task<MonthSheet> GetMonthSheetAsync(int year, int month, CancellationToken cancellationToken = default)
    {
        if (year is < 2020 or > 2100 || month is < 1 or > 12)
        {
            throw new DomainException("The month is not valid.");
        }

        DateTimeOffset now = clock.UtcNow;
        int days = DateTime.DaysInMonth(year, month);
        DateOnly first = new(year, month, 1);
        DateOnly last = new(year, month, days);

        List<Person> people = await RosterAsync(cancellationToken);
        Dictionary<Guid, StaffProfile> profiles = await ProfilesAsync(people, cancellationToken);

        List<AttendanceRecord> records = await dbContext.Set<AttendanceRecord>()
            .AsNoTracking()
            .Where(r => r.Day >= first && r.Day <= last)
            .ToListAsync(cancellationToken);
        List<LeaveRequest> leaves = await dbContext.Set<LeaveRequest>()
            .AsNoTracking()
            .Where(l => l.Status == LeaveStatus.Approved && l.FromDay <= last && l.ToDay >= first)
            .ToListAsync(cancellationToken);

        MonthSheetRow[] rows = people.Select(p =>
        {
            StaffProfile profile = profiles[p.Id];
            int[] statuses = new int[days];
            for (int i = 0; i < days; i++)
            {
                DateOnly day = first.AddDays(i);
                AttendanceRecord? record = records.FirstOrDefault(r => r.UserId == p.Id && r.Day == day);
                bool onLeave = leaves.Any(l => l.UserId == p.Id && l.FromDay <= day && l.ToDay >= day);
                statuses[i] = StatusOf(profile, day, record, onLeave, now);
            }

            return new MonthSheetRow(
                p.Id,
                p.FullName,
                statuses,
                statuses.Count(s => s == DayStatus.Present),
                statuses.Count(s => s == DayStatus.Late),
                statuses.Count(s => s == DayStatus.Absent),
                statuses.Count(s => s == DayStatus.OnLeave));
        })
        .OrderBy(r => r.FullName)
        .ToArray();

        return new MonthSheet(year, month, days, rows);
    }

    public async Task<IReadOnlyList<LeaveRequestDto>> ListLeavesAsync(int? status, CancellationToken cancellationToken = default)
    {
        IQueryable<LeaveRequest> query = dbContext.Set<LeaveRequest>().AsNoTracking();
        if (status is { } wanted && Enum.IsDefined(typeof(LeaveStatus), wanted))
        {
            query = query.Where(l => l.Status == (LeaveStatus)wanted);
        }

        List<LeaveRequest> rows = await query
            .OrderBy(l => l.Status == LeaveStatus.Pending ? 0 : 1)
            .ThenByDescending(l => l.CreatedAtUtc)
            .Take(200)
            .ToListAsync(cancellationToken);

        Dictionary<Guid, string> names = await NamesAsync(rows.Select(r => r.UserId), cancellationToken);
        Dictionary<Guid, int> limits = (await dbContext.Set<StaffProfile>()
            .AsNoTracking()
            .Where(p => rows.Select(r => r.UserId).Contains(p.UserId))
            .Select(p => new { p.UserId, p.MonthlyDayOffLimit })
            .ToListAsync(cancellationToken))
            .ToDictionary(p => p.UserId, p => p.MonthlyDayOffLimit);

        return rows.Select(l => ToDto(l, names.GetValueOrDefault(l.UserId, "—"), limits.GetValueOrDefault(l.UserId))).ToArray();
    }

    public async Task<LeaveRequestDto> DecideLeaveAsync(
        Guid actorUserId, Guid leaveId, bool approve, string? note, CancellationToken cancellationToken = default)
    {
        LeaveRequest leave = await dbContext.Set<LeaveRequest>()
            .SingleOrDefaultAsync(l => l.Id == leaveId, cancellationToken)
            ?? throw new DomainException("The request was not found.");

        DateTimeOffset now = clock.UtcNow;
        leave.Decide(approve, note, actorUserId, now);
        dbContext.Set<AuditLogEntry>().Add(AuditLogEntry.ForStaffHr(Guid.NewGuid(), actorUserId, leave.UserId, AuditLeave, leave.Id, now));
        await dbContext.SaveChangesAsync(cancellationToken);

        string period = leave.FromDay == leave.ToDay ? $"{leave.FromDay:dd.MM.yyyy}" : $"{leave.FromDay:dd.MM.yyyy} – {leave.ToDay:dd.MM.yyyy}";
        await NotifyAsync(
            [leave.UserId],
            NotificationType.StaffLeaveDecided,
            approve ? "İcazəniz təsdiqləndi" : "İcazəniz rədd edildi",
            $"{period}." + (leave.DecisionNote is null ? string.Empty : $" {leave.DecisionNote}"),
            leave.Id,
            "/hr/me",
            cancellationToken);

        StaffProfile? profile = await FindProfileAsync(leave.UserId, cancellationToken);
        return ToDto(leave, await NameOfAsync(leave.UserId, cancellationToken), profile?.MonthlyDayOffLimit ?? 0);
    }

    public async Task<PayrollSheetDto> AddPayrollLineAsync(
        Guid actorUserId, Guid userId, DateOnly month, int kind, int amountInQepik, string? note,
        CancellationToken cancellationToken = default)
    {
        if (!Enum.IsDefined(typeof(PayrollKind), kind))
        {
            throw new DomainException("The kind of line is not valid.");
        }

        await RequireProfileAsync(userId, cancellationToken);
        PayrollKind lineKind = (PayrollKind)kind;
        PayrollSheetDto before = await GetPayrollAsync(userId, month, cancellationToken);

        // Money given out cannot be more than what is left of the month.
        if (lineKind is PayrollKind.Advance or PayrollKind.Payment && amountInQepik > before.RemainingInQepik)
        {
            throw new DomainException("The amount is more than what is left to pay for this month.");
        }

        DateTimeOffset now = clock.UtcNow;
        PayrollEntry entry = PayrollEntry.Create(userId, month, lineKind, amountInQepik, note, actorUserId, now);
        dbContext.Set<PayrollEntry>().Add(entry);
        dbContext.Set<AuditLogEntry>().Add(AuditLogEntry.ForStaffHr(Guid.NewGuid(), actorUserId, userId, AuditPayLine, entry.Id, now));
        await dbContext.SaveChangesAsync(cancellationToken);

        string amount = Money(amountInQepik);
        string monthText = $"{entry.Month:MM.yyyy}";
        (string title, string body) = lineKind switch
        {
            PayrollKind.Deduction => ("Maaşınızdan kəsinti", $"{monthText}: {amount} AZN kəsildi. Səbəb: {entry.Note}"),
            PayrollKind.Advance => ("Avans verildi", $"{monthText}: {amount} AZN avans verildi."),
            PayrollKind.Payment => ("Maaş ödənildi", $"{monthText}: {amount} AZN ödənildi."),
            _ => ("Maaşınıza əlavə", $"{monthText}: {amount} AZN əlavə edildi.")
        };
        await NotifyAsync([userId], NotificationType.StaffPayrollChanged, title, body, entry.Id, "/hr/me", cancellationToken);

        return await GetPayrollAsync(userId, month, cancellationToken);
    }

    public async Task<PayrollSheetDto> VoidPayrollLineAsync(
        Guid actorUserId, Guid lineId, CancellationToken cancellationToken = default)
    {
        PayrollEntry entry = await dbContext.Set<PayrollEntry>()
            .SingleOrDefaultAsync(e => e.Id == lineId, cancellationToken)
            ?? throw new DomainException("The line was not found.");

        DateTimeOffset now = clock.UtcNow;
        entry.Void(actorUserId, now);
        dbContext.Set<AuditLogEntry>().Add(AuditLogEntry.ForStaffHr(Guid.NewGuid(), actorUserId, entry.UserId, AuditPayVoid, entry.Id, now));
        await dbContext.SaveChangesAsync(cancellationToken);

        return await GetPayrollAsync(entry.UserId, entry.Month, cancellationToken);
    }

    public async Task<IReadOnlyList<StaffRequestDto>> ListRequestsAsync(
        bool forOwner, int? status, CancellationToken cancellationToken = default)
    {
        IQueryable<StaffRequest> query = dbContext.Set<StaffRequest>().AsNoTracking();
        query = forOwner
            ? query.Where(r => r.ToOwner)
            : query.Where(r => !r.ToOwner || r.Status == StaffRequestStatus.Forwarded);

        if (status is { } wanted && Enum.IsDefined(typeof(StaffRequestStatus), wanted))
        {
            query = query.Where(r => r.Status == (StaffRequestStatus)wanted);
        }

        List<StaffRequest> rows = await query
            .OrderBy(r => r.Status == StaffRequestStatus.Open || r.Status == StaffRequestStatus.Forwarded ? 0 : 1)
            .ThenByDescending(r => r.CreatedAtUtc)
            .Take(200)
            .ToListAsync(cancellationToken);

        return await RequestDtosAsync(rows, cancellationToken);
    }

    public async Task<StaffRequestDto> HandleRequestAsync(
        Guid actorUserId, Guid requestId, int action, string? note, bool actorIsOwner,
        CancellationToken cancellationToken = default)
    {
        StaffRequest request = await dbContext.Set<StaffRequest>()
            .SingleOrDefaultAsync(r => r.Id == requestId, cancellationToken)
            ?? throw new DomainException("The request was not found.");

        // The watcher only handles what was sent to the staff side; what goes to the owner is the owner's.
        if (!actorIsOwner && request.ToOwner)
        {
            throw new DomainException("This request is for the owner to decide.");
        }

        DateTimeOffset now = clock.UtcNow;
        string name = await NameOfAsync(request.FromUserId, cancellationToken);

        switch (action)
        {
            case 1:
            case 2:
                request.Resolve(action == 1, note, actorUserId, now);
                break;
            case 3:
                if (actorIsOwner)
                {
                    throw new DomainException("The owner decides; there is no one to pass it to.");
                }

                request.Forward(actorUserId, note, now);
                break;
            default:
                throw new DomainException("The action is not valid.");
        }

        dbContext.Set<AuditLogEntry>().Add(AuditLogEntry.ForStaffHr(Guid.NewGuid(), actorUserId, request.FromUserId, AuditRequest, request.Id, now));
        await dbContext.SaveChangesAsync(cancellationToken);

        if (action == 3)
        {
            await NotifyAsync(
                await OwnersAsync(cancellationToken),
                NotificationType.StaffRequestReceived,
                "Müraciət sizə yönləndirildi",
                $"{name}: {request.Subject}",
                request.Id,
                "/hr/requests",
                cancellationToken);
        }
        else
        {
            await NotifyAsync(
                [request.FromUserId],
                NotificationType.StaffRequestAnswered,
                action == 1 ? "Müraciətinizə cavab verildi" : "Müraciətiniz rədd edildi",
                request.Subject + (request.Answer is null ? string.Empty : $" — {request.Answer}"),
                request.Id,
                "/hr/me",
                cancellationToken);
        }

        string? handler = request.HandledByUserId is { } h ? await NameOfAsync(h, cancellationToken) : null;
        return ToDto(request, name, handler);
    }

    // ------------------------------------------------------------------ sign-in and watching

    public async Task<bool> IsAccessAllowedAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        if (cache.TryGetValue(AccessKey(userId), out bool known))
        {
            return known;
        }

        // No profile: nothing was ever closed for this person. With one, the switch decides.
        bool? switchedOn = await dbContext.Set<StaffProfile>()
            .AsNoTracking()
            .Where(p => p.UserId == userId)
            .Select(p => (bool?)p.LoginAllowed)
            .SingleOrDefaultAsync(cancellationToken);
        bool allowed = switchedOn ?? true;

        cache.Set(AccessKey(userId), allowed, Keep);
        return allowed;
    }

    public async Task<int> CheckAbsencesAsync(CancellationToken cancellationToken = default)
    {
        DateTimeOffset now = clock.UtcNow;
        DateOnly today = HrTime.BakuDay(now);
        int minute = HrTime.BakuMinute(now);

        List<Person> people = await RosterAsync(cancellationToken);
        Dictionary<Guid, StaffProfile> profiles = await ProfilesAsync(people, cancellationToken);
        HashSet<Guid> started = (await dbContext.Set<AttendanceRecord>()
            .AsNoTracking()
            .Where(r => r.Day == today)
            .Select(r => r.UserId)
            .ToListAsync(cancellationToken)).ToHashSet();
        HashSet<Guid> away = (await dbContext.Set<LeaveRequest>()
            .AsNoTracking()
            .Where(l => l.Status == LeaveStatus.Approved && l.FromDay <= today && l.ToDay >= today)
            .Select(l => l.UserId)
            .ToListAsync(cancellationToken)).ToHashSet();
        HashSet<Guid> told = (await dbContext.Set<AttendanceAlert>()
            .AsNoTracking()
            .Where(a => a.Day == today)
            .Select(a => a.UserId)
            .ToListAsync(cancellationToken)).ToHashSet();

        List<Person> missing = people
            .Where(p =>
            {
                StaffProfile profile = profiles[p.Id];
                return profile.LoginAllowed &&
                       profile.Works(today.DayOfWeek) &&
                       minute >= profile.WorkStartMinute + HrTime.GraceMinutes &&
                       !started.Contains(p.Id) &&
                       !away.Contains(p.Id) &&
                       !told.Contains(p.Id);
            })
            .ToList();

        if (missing.Count == 0)
        {
            return 0;
        }

        foreach (Person person in missing)
        {
            dbContext.Set<AttendanceAlert>().Add(AttendanceAlert.Create(person.Id, today, now));
        }

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            // Another run told the owner at the same moment; nothing more to do.
            return 0;
        }

        IReadOnlyList<Guid> to = await WatchersAndOwnersAsync(cancellationToken);
        foreach (Person person in missing)
        {
            StaffProfile profile = profiles[person.Id];
            await NotifyAsync(
                to,
                NotificationType.StaffAbsent,
                "İşçi işə başlamayıb",
                $"{person.FullName} bu gün {Hhmm(profile.WorkStartMinute)}-da işə başlamalı idi, hələ başlamayıb.",
                person.Id,
                "/hr/today",
                cancellationToken);
        }

        return missing.Count;
    }

    // ------------------------------------------------------------------ helpers

    private static string AccessKey(Guid userId) => $"staff-access:{userId:N}";

    private static string Hhmm(int minute) => $"{minute / 60:00}:{minute % 60:00}";

    private static string Money(int qepik) => (qepik / 100m).ToString("0.00", System.Globalization.CultureInfo.InvariantCulture);

    private static StaffScheduleDto Schedule(StaffProfile? p) => p is null
        ? new StaffScheduleDto(StaffProfile.DefaultStartMinute, StaffProfile.DefaultEndMinute, StaffProfile.DefaultWorkDays, StaffProfile.DefaultMonthlyDayOffLimit)
        : new StaffScheduleDto(p.WorkStartMinute, p.WorkEndMinute, p.WorkDaysMask, p.MonthlyDayOffLimit);

    /// <summary>The colour of one day of one person.</summary>
    private static int StatusOf(StaffProfile profile, DateOnly day, AttendanceRecord? record, bool onLeave, DateTimeOffset now)
    {
        DateOnly today = HrTime.BakuDay(now);

        if (record is not null)
        {
            DateTimeOffset lateAfter = HrTime.FromBaku(day, profile.WorkStartMinute + HrTime.GraceMinutes);
            return profile.Works(day.DayOfWeek) && record.CheckInAtUtc > lateAfter ? DayStatus.Late : DayStatus.Present;
        }

        if (!profile.Works(day.DayOfWeek))
        {
            return DayStatus.DayOff;
        }

        if (onLeave)
        {
            return DayStatus.OnLeave;
        }

        if (day > today)
        {
            return DayStatus.Future;
        }

        if (day < today)
        {
            return DayStatus.Absent;
        }

        return HrTime.BakuMinute(now) >= profile.WorkStartMinute + HrTime.GraceMinutes ? DayStatus.Absent : DayStatus.NotYet;
    }

    private static int CountInMonth(DateOnly from, DateOnly to, DateOnly monthStart, StaffProfile profile)
    {
        DateOnly monthEnd = monthStart.AddMonths(1).AddDays(-1);
        DateOnly a = from > monthStart ? from : monthStart;
        DateOnly b = to < monthEnd ? to : monthEnd;
        return b < a ? 0 : HrTime.WorkingDaysBetween(a, b, profile);
    }

    /// <summary>Working days of a month already taken (approved) or asked for (waiting).</summary>
    private async Task<int> UsedInMonthAsync(
        Guid userId, StaffProfile profile, int year, int month, Guid? except, CancellationToken cancellationToken)
    {
        DateOnly start = new(year, month, 1);
        DateOnly end = start.AddMonths(1).AddDays(-1);
        List<LeaveRequest> leaves = await dbContext.Set<LeaveRequest>()
            .AsNoTracking()
            .Where(l => l.UserId == userId &&
                        (l.Status == LeaveStatus.Pending || l.Status == LeaveStatus.Approved) &&
                        l.FromDay <= end && l.ToDay >= start &&
                        (except == null || l.Id != except))
            .ToListAsync(cancellationToken);

        return leaves.Sum(l => CountInMonth(l.FromDay, l.ToDay, start, profile));
    }

    private async Task<StaffProfile?> FindProfileAsync(Guid userId, CancellationToken cancellationToken) =>
        await dbContext.Set<StaffProfile>().AsNoTracking().SingleOrDefaultAsync(p => p.UserId == userId, cancellationToken);

    /// <summary>The person's profile, made with the usual hours the first time it is needed.</summary>
    private async Task<StaffProfile> RequireProfileAsync(Guid userId, CancellationToken cancellationToken)
    {
        StaffProfile? profile = await dbContext.Set<StaffProfile>().SingleOrDefaultAsync(p => p.UserId == userId, cancellationToken);
        if (profile is not null)
        {
            return profile;
        }

        bool isAdmin = (await RosterAsync(cancellationToken)).Any(p => p.Id == userId);
        if (!isAdmin)
        {
            throw new DomainException("This is for staff members (Admins) only.");
        }

        profile = StaffProfile.Create(userId, clock.UtcNow);
        dbContext.Set<StaffProfile>().Add(profile);
        await dbContext.SaveChangesAsync(cancellationToken);
        return profile;
    }

    private async Task<Dictionary<Guid, StaffProfile>> ProfilesAsync(IReadOnlyCollection<Person> people, CancellationToken cancellationToken)
    {
        Guid[] ids = people.Select(p => p.Id).ToArray();
        Dictionary<Guid, StaffProfile> found = (await dbContext.Set<StaffProfile>()
            .Where(p => ids.Contains(p.UserId))
            .ToListAsync(cancellationToken))
            .ToDictionary(p => p.UserId);

        bool added = false;
        foreach (Person person in people.Where(p => !found.ContainsKey(p.Id)))
        {
            StaffProfile profile = StaffProfile.Create(person.Id, clock.UtcNow);
            dbContext.Set<StaffProfile>().Add(profile);
            found[person.Id] = profile;
            added = true;
        }

        if (added)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return found;
    }

    /// <summary>The Admins (the owner is not on the staff list: he has no hours to keep).</summary>
    private async Task<List<Person>> RosterAsync(CancellationToken cancellationToken)
    {
        var owners = dbContext.Set<IdentityUserRole<Guid>>()
            .Join(dbContext.Roles, ur => ur.RoleId, r => r.Id, (ur, r) => new { ur.UserId, r.Name })
            .Where(x => x.Name == RoleNames.SuperAdmin)
            .Select(x => x.UserId);

        return await (
            from userRole in dbContext.Set<IdentityUserRole<Guid>>()
            join role in dbContext.Roles on userRole.RoleId equals role.Id
            join user in dbContext.Users on userRole.UserId equals user.Id
            join profile in dbContext.Set<UserProfile>() on user.Id equals profile.Id
            where role.Name == RoleNames.Admin && !owners.Contains(user.Id)
            orderby profile.FullName
            select new Person(user.Id, profile.FullName, user.PhoneNumber))
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    private async Task<IReadOnlyList<Guid>> OwnersAsync(CancellationToken cancellationToken) =>
        await (
            from userRole in dbContext.Set<IdentityUserRole<Guid>>()
            join role in dbContext.Roles on userRole.RoleId equals role.Id
            where role.Name == RoleNames.SuperAdmin
            select userRole.UserId)
            .Distinct()
            .ToListAsync(cancellationToken);

    /// <summary>Admins who were given the staff section on purpose.</summary>
    private async Task<IReadOnlyList<Guid>> WatchersAsync(CancellationToken cancellationToken) =>
        await (
            from claim in dbContext.Set<IdentityUserClaim<Guid>>()
            join userRole in dbContext.Set<IdentityUserRole<Guid>>() on claim.UserId equals userRole.UserId
            join role in dbContext.Roles on userRole.RoleId equals role.Id
            where claim.ClaimType == AdminSections.ClaimType &&
                  claim.ClaimValue == AdminSections.Hr &&
                  role.Name == RoleNames.Admin
            select claim.UserId)
            .Distinct()
            .ToListAsync(cancellationToken);

    private async Task<IReadOnlyList<Guid>> WatchersAndOwnersAsync(CancellationToken cancellationToken) =>
        (await OwnersAsync(cancellationToken)).Concat(await WatchersAsync(cancellationToken)).Distinct().ToArray();

    /// <summary>Whoever looks after the staff; the owner when nobody was given that job.</summary>
    private async Task<IReadOnlyList<Guid>> WatchersOrOwnersAsync(CancellationToken cancellationToken)
    {
        IReadOnlyList<Guid> watchers = await WatchersAsync(cancellationToken);
        return watchers.Count > 0 ? watchers : await OwnersAsync(cancellationToken);
    }

    private async Task NotifyAsync(
        IEnumerable<Guid> users, NotificationType type, string title, string body, Guid related, string url,
        CancellationToken cancellationToken)
    {
        foreach (Guid user in users.Distinct())
        {
            await notifications.CreateAsync(
                new CreateNotificationCommand(user, (int)type, Cut(title, 160), Cut(body, 1000), related, url),
                cancellationToken);
        }
    }

    private static string Cut(string text, int max) => text.Length <= max ? text : text[..(max - 1)] + "…";

    private async Task<string> NameOfAsync(Guid userId, CancellationToken cancellationToken) =>
        await dbContext.Set<UserProfile>()
            .AsNoTracking()
            .Where(p => p.Id == userId)
            .Select(p => p.FullName)
            .FirstOrDefaultAsync(cancellationToken) ?? "—";

    private async Task<Dictionary<Guid, string>> NamesAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken)
    {
        Guid[] wanted = ids.Distinct().ToArray();
        return await dbContext.Set<UserProfile>()
            .AsNoTracking()
            .Where(p => wanted.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, p => p.FullName, cancellationToken);
    }

    private static LeaveRequestDto ToDto(LeaveRequest l, string name, int limit) =>
        new(l.Id, l.UserId, name, l.FromDay, l.ToDay, l.WorkingDays, l.OverLimit, l.Reason, (int)l.Status,
            l.DecisionNote, l.CreatedAtUtc, l.DecidedAtUtc, limit);

    private static StaffRequestDto ToDto(StaffRequest r, string fromName, string? handler) =>
        new(r.Id, r.FromUserId, fromName, r.Subject, r.Body, r.ToOwner, (int)r.Status, r.Answer, handler,
            r.CreatedAtUtc, r.HandledAtUtc);

    private async Task<IReadOnlyList<StaffRequestDto>> RequestDtosAsync(List<StaffRequest> rows, CancellationToken cancellationToken)
    {
        Dictionary<Guid, string> names = await NamesAsync(
            rows.Select(r => r.FromUserId).Concat(rows.Where(r => r.HandledByUserId.HasValue).Select(r => r.HandledByUserId!.Value)),
            cancellationToken);

        return rows.Select(r => ToDto(
            r,
            names.GetValueOrDefault(r.FromUserId, "—"),
            r.HandledByUserId is { } h ? names.GetValueOrDefault(h) : null)).ToArray();
    }

    private static StaffProfileDto ToDto(Person person, StaffProfile p, IReadOnlyList<string> sections) =>
        new(person.Id, person.FullName, person.Phone, p.MonthlySalaryInQepik, p.LoginAllowed, Schedule(p), p.Note, sections);
}
