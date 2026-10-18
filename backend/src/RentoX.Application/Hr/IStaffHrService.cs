namespace RentoX.Application.Hr;

/// <summary>How a staff member's day looks: the number is shown as a colour on the board and in the table.</summary>
public static class DayStatus
{
    /// <summary>Started in time.</summary>
    public const int Present = 1;

    /// <summary>Started after the start of the day (and the grace time).</summary>
    public const int Late = 2;

    /// <summary>A working day with no start and no approved day off: the red box.</summary>
    public const int Absent = 3;

    public const int OnLeave = 4;

    /// <summary>Not a working day of this person.</summary>
    public const int DayOff = 5;

    /// <summary>A working day that has begun, but the person still has time to start.</summary>
    public const int NotYet = 6;

    public const int Future = 7;
}

public sealed record StaffScheduleDto(int WorkStartMinute, int WorkEndMinute, int WorkDaysMask, int MonthlyDayOffLimit);

public sealed record StaffProfileDto(
    Guid UserId,
    string FullName,
    string? Phone,
    int MonthlySalaryInQepik,
    bool LoginAllowed,
    StaffScheduleDto Schedule,
    string? Note,
    IReadOnlyList<string> Sections);

public sealed record UpdateStaffProfileCommand(
    int MonthlySalaryInQepik,
    bool LoginAllowed,
    int WorkStartMinute,
    int WorkEndMinute,
    int WorkDaysMask,
    int MonthlyDayOffLimit,
    string? Note);

public sealed record DayBoardRow(
    Guid UserId,
    string FullName,
    int Status,
    DateTimeOffset? CheckInAtUtc,
    DateTimeOffset? CheckOutAtUtc,
    int WorkedMinutes,
    int WorkStartMinute,
    int WorkEndMinute,
    bool LoginAllowed);

public sealed record MonthSheetRow(
    Guid UserId,
    string FullName,
    IReadOnlyList<int> Days,
    int Present,
    int Late,
    int Absent,
    int Leave);

public sealed record MonthSheet(int Year, int Month, int DaysInMonth, IReadOnlyList<MonthSheetRow> Rows);

public sealed record MyDay(
    bool IsStaff,
    DateOnly Today,
    int Status,
    StaffScheduleDto Schedule,
    DateTimeOffset? CheckInAtUtc,
    DateTimeOffset? CheckOutAtUtc,
    int WorkedMinutes,
    /// <summary>Working days of this month already asked for (waiting or approved).</summary>
    int DayOffUsedThisMonth,
    /// <summary>Whether someone was given the job of looking after the staff (otherwise requests go to the owner).</summary>
    bool HasWatcher = false);

public sealed record LeaveRequestDto(
    Guid Id,
    Guid UserId,
    string FullName,
    DateOnly FromDay,
    DateOnly ToDay,
    int WorkingDays,
    bool OverLimit,
    string Reason,
    int Status,
    string? DecisionNote,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? DecidedAtUtc,
    int DayOffLimit);

public sealed record PayrollLineDto(
    Guid Id,
    int Kind,
    int AmountInQepik,
    string? Note,
    DateTimeOffset CreatedAtUtc,
    string? CreatedByName,
    bool IsVoid);

public sealed record PayrollSheetDto(
    Guid UserId,
    string FullName,
    DateOnly Month,
    int BaseInQepik,
    int BonusInQepik,
    int DeductionsInQepik,
    int AdvancesInQepik,
    int PaymentsInQepik,
    /// <summary>What is still to be paid: base + bonus − deductions − advances − payments.</summary>
    int RemainingInQepik,
    IReadOnlyList<PayrollLineDto> Lines);

/// <summary>One month in the money history of a person: what the pay of that month came to.</summary>
public sealed record PayrollMonthDto(
    DateOnly Month,
    int BaseInQepik,
    int BonusInQepik,
    int DeductionsInQepik,
    int AdvancesInQepik,
    int PaymentsInQepik,
    int RemainingInQepik,
    /// <summary>How many lines (bonus, deduction, advance, payment) were written for the month, voided ones apart.</summary>
    int LineCount,
    int VoidedCount);

/// <summary>A change of the monthly pay: from which month, how much, who made it and when.</summary>
public sealed record SalaryChangeDto(
    DateOnly EffectiveFrom,
    int AmountInQepik,
    int? PreviousAmountInQepik,
    string? ChangedByName,
    DateTimeOffset ChangedAtUtc);

/// <summary>
/// Everything about the money of one person over the months: the totals, month by month, and every change of the pay.
/// </summary>
public sealed record PayrollHistoryDto(
    Guid UserId,
    string FullName,
    int MonthsShown,
    int TotalBaseInQepik,
    int TotalBonusInQepik,
    int TotalDeductionsInQepik,
    int TotalAdvancesInQepik,
    int TotalPaymentsInQepik,
    /// <summary>Still to be paid over all the months shown (negative: paid more than the pay).</summary>
    int TotalRemainingInQepik,
    IReadOnlyList<PayrollMonthDto> Months,
    IReadOnlyList<SalaryChangeDto> SalaryChanges);

public sealed record StaffRequestDto(
    Guid Id,
    Guid FromUserId,
    string FromName,
    string Subject,
    string Body,
    bool ToOwner,
    int Status,
    string? Answer,
    string? HandledByName,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? HandledAtUtc);

/// <summary>
/// Looking after the staff: who came to work, days off, pay, requests. A staff member uses the "my" calls for
/// themselves; the owner (and the person given the staff section) uses the rest.
/// </summary>
public interface IStaffHrService
{
    // ---- the staff member, for themselves --------------------------------------------------

    Task<MyDay> GetMyDayAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<MyDay> CheckInAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<MyDay> CheckOutAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<LeaveRequestDto> RequestLeaveAsync(
        Guid userId, DateOnly firstDay, DateOnly lastDay, string reason, CancellationToken cancellationToken = default);

    Task CancelLeaveAsync(Guid userId, Guid leaveId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<LeaveRequestDto>> GetMyLeavesAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<PayrollSheetDto> GetPayrollAsync(Guid userId, DateOnly month, CancellationToken cancellationToken = default);

    /// <summary>The money of one person month by month (the last [months], 1 to 36) and every change of the pay.</summary>
    Task<PayrollHistoryDto> GetPayrollHistoryAsync(Guid userId, int months, CancellationToken cancellationToken = default);

    Task<StaffRequestDto> SendRequestAsync(
        Guid userId, string subject, string body, bool toOwner, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<StaffRequestDto>> GetMyRequestsAsync(Guid userId, CancellationToken cancellationToken = default);

    // ---- looking after the staff -------------------------------------------------------------

    Task<IReadOnlyList<StaffProfileDto>> ListStaffAsync(CancellationToken cancellationToken = default);

    Task<StaffProfileDto?> GetProfileAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>Pay, sign-in permission, schedule (the owner only).</summary>
    Task<StaffProfileDto> UpdateProfileAsync(
        Guid actorUserId, Guid userId, UpdateStaffProfileCommand command, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DayBoardRow>> GetDayBoardAsync(DateOnly day, CancellationToken cancellationToken = default);

    Task<MonthSheet> GetMonthSheetAsync(int year, int month, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<LeaveRequestDto>> ListLeavesAsync(int? status, CancellationToken cancellationToken = default);

    /// <summary>The owner approves or refuses; the person is told.</summary>
    Task<LeaveRequestDto> DecideLeaveAsync(
        Guid actorUserId, Guid leaveId, bool approve, string? note, CancellationToken cancellationToken = default);

    Task<PayrollSheetDto> AddPayrollLineAsync(
        Guid actorUserId, Guid userId, DateOnly month, int kind, int amountInQepik, string? note,
        CancellationToken cancellationToken = default);

    Task<PayrollSheetDto> VoidPayrollLineAsync(
        Guid actorUserId, Guid lineId, CancellationToken cancellationToken = default);

    /// <param name="forOwner">True: everything the owner decides on; false: the ones the watcher looks at.</param>
    Task<IReadOnlyList<StaffRequestDto>> ListRequestsAsync(
        bool forOwner, int? status, CancellationToken cancellationToken = default);

    /// <summary>action: 1 answer, 2 refuse, 3 pass to the owner.</summary>
    Task<StaffRequestDto> HandleRequestAsync(
        Guid actorUserId, Guid requestId, int action, string? note, bool actorIsOwner,
        CancellationToken cancellationToken = default);

    // ---- sign-in and the watching ------------------------------------------------------------

    /// <summary>False when the owner closed this person's access to the panel.</summary>
    Task<bool> IsAccessAllowedAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Looks for staff who should have started and have not, and tells the owner (and the watchers) once per
    /// person and day. Returns how many people were reported.
    /// </summary>
    Task<int> CheckAbsencesAsync(CancellationToken cancellationToken = default);
}
