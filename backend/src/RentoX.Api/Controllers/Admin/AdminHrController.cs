using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RentoX.Application.Authorization;
using RentoX.Application.Hr;
using RentoX.Domain.Common.Exceptions;
using RentoX.Domain.Hr;

namespace RentoX.Api.Controllers.Admin;

public sealed record AccessResponse(bool Allowed);

public sealed record RequestLeaveRequest(DateOnly From, DateOnly To, string Reason);

public sealed record DecideLeaveRequest(bool Approve, string? Note);

public sealed record SendStaffRequest(string Subject, string Body, bool ToOwner);

public sealed record HandleStaffRequest(int Action, string? Note);

public sealed record AddPayrollLineRequest(string Month, int Kind, int AmountInQepik, string? Note);

/// <summary>
/// The staff: a member's own day (start, finish, days off, pay, requests), and, for the owner and the person given
/// the staff section, who came to work, the table, the days off, the pay and the requests.
/// </summary>
[ApiController]
[Route("api/admin/hr")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class AdminHrController(IStaffHrService hr) : ControllerBase
{
    private Guid Me =>
        Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out Guid id) && id != Guid.Empty
            ? id
            : throw new DomainException("Authentication is required.");

    private bool IsOwner => User.IsInRole(RoleNames.SuperAdmin);

    // ---- sign-in -------------------------------------------------------------------------------

    /// <summary>Asked right after signing in: false when the owner closed the person's access. Open to any admin.</summary>
    [HttpGet("me/access")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<ActionResult<AccessResponse>> AccessAsync(CancellationToken cancellationToken) =>
        Ok(new AccessResponse(IsOwner || await hr.IsAccessAllowedAsync(Me, cancellationToken)));

    // ---- the staff member, for themselves -----------------------------------------------------------

    [HttpGet("me/day")]
    [Authorize(Policy = PolicyNames.AdminAccess)]
    public async Task<ActionResult<MyDay>> MyDayAsync(CancellationToken cancellationToken) =>
        Ok(await hr.GetMyDayAsync(Me, cancellationToken));

    [HttpPost("me/check-in")]
    [Authorize(Policy = PolicyNames.AdminAccess)]
    public async Task<ActionResult<MyDay>> CheckInAsync(CancellationToken cancellationToken) =>
        Ok(await hr.CheckInAsync(Me, cancellationToken));

    [HttpPost("me/check-out")]
    [Authorize(Policy = PolicyNames.AdminAccess)]
    public async Task<ActionResult<MyDay>> CheckOutAsync(CancellationToken cancellationToken) =>
        Ok(await hr.CheckOutAsync(Me, cancellationToken));

    [HttpGet("me/leaves")]
    [Authorize(Policy = PolicyNames.AdminAccess)]
    public async Task<ActionResult<IReadOnlyList<LeaveRequestDto>>> MyLeavesAsync(CancellationToken cancellationToken) =>
        Ok(await hr.GetMyLeavesAsync(Me, cancellationToken));

    [HttpPost("me/leaves")]
    [Authorize(Policy = PolicyNames.AdminAccess)]
    public async Task<ActionResult<LeaveRequestDto>> RequestLeaveAsync(
        RequestLeaveRequest request, CancellationToken cancellationToken) =>
        Ok(await hr.RequestLeaveAsync(Me, request.From, request.To, request.Reason, cancellationToken));

    [HttpDelete("me/leaves/{leaveId:guid}")]
    [Authorize(Policy = PolicyNames.AdminAccess)]
    public async Task<IActionResult> CancelLeaveAsync(Guid leaveId, CancellationToken cancellationToken)
    {
        await hr.CancelLeaveAsync(Me, leaveId, cancellationToken);
        return NoContent();
    }

    [HttpGet("me/payroll")]
    [Authorize(Policy = PolicyNames.AdminAccess)]
    public async Task<ActionResult<PayrollSheetDto>> MyPayrollAsync([FromQuery] string? month, CancellationToken cancellationToken) =>
        Ok(await hr.GetPayrollAsync(Me, ParseMonth(month), cancellationToken));

    [HttpGet("me/requests")]
    [Authorize(Policy = PolicyNames.AdminAccess)]
    public async Task<ActionResult<IReadOnlyList<StaffRequestDto>>> MyRequestsAsync(CancellationToken cancellationToken) =>
        Ok(await hr.GetMyRequestsAsync(Me, cancellationToken));

    [HttpPost("me/requests")]
    [Authorize(Policy = PolicyNames.AdminAccess)]
    public async Task<ActionResult<StaffRequestDto>> SendRequestAsync(
        SendStaffRequest request, CancellationToken cancellationToken) =>
        Ok(await hr.SendRequestAsync(Me, request.Subject, request.Body, request.ToOwner, cancellationToken));

    // ---- who came to work: the owner and the person given the staff section -------------------------------

    [HttpGet("board")]
    [Authorize(Policy = PolicyNames.AdminAccess)]
    [Authorize(Policy = AdminSections.HrPolicy)]
    public async Task<ActionResult<IReadOnlyList<DayBoardRow>>> BoardAsync(
        [FromQuery] DateOnly? day, CancellationToken cancellationToken) =>
        Ok(await hr.GetDayBoardAsync(day ?? HrTime.BakuDay(DateTimeOffset.UtcNow), cancellationToken));

    [HttpGet("month")]
    [Authorize(Policy = PolicyNames.AdminAccess)]
    [Authorize(Policy = AdminSections.HrPolicy)]
    public async Task<ActionResult<MonthSheet>> MonthAsync(
        [FromQuery] int? year, [FromQuery] int? month, CancellationToken cancellationToken)
    {
        DateOnly today = HrTime.BakuDay(DateTimeOffset.UtcNow);
        return Ok(await hr.GetMonthSheetAsync(year ?? today.Year, month ?? today.Month, cancellationToken));
    }

    [HttpGet("leaves")]
    [Authorize(Policy = PolicyNames.AdminAccess)]
    [Authorize(Policy = AdminSections.HrPolicy)]
    public async Task<ActionResult<IReadOnlyList<LeaveRequestDto>>> LeavesAsync(
        [FromQuery] int? status, CancellationToken cancellationToken) =>
        Ok(await hr.ListLeavesAsync(status, cancellationToken));

    /// <summary>scope "owner": what goes to the owner (owner only). Otherwise what the staff side handles.</summary>
    [HttpGet("requests")]
    [Authorize(Policy = PolicyNames.AdminAccess)]
    [Authorize(Policy = AdminSections.HrPolicy)]
    public async Task<ActionResult<IReadOnlyList<StaffRequestDto>>> RequestsAsync(
        [FromQuery] string? scope, [FromQuery] int? status, CancellationToken cancellationToken)
    {
        bool forOwner = string.Equals(scope, "owner", StringComparison.OrdinalIgnoreCase);
        if (forOwner && !IsOwner)
        {
            return Forbid();
        }

        return Ok(await hr.ListRequestsAsync(forOwner, status, cancellationToken));
    }

    [HttpPost("requests/{requestId:guid}/handle")]
    [Authorize(Policy = PolicyNames.AdminAccess)]
    [Authorize(Policy = AdminSections.HrPolicy)]
    public async Task<ActionResult<StaffRequestDto>> HandleRequestAsync(
        Guid requestId, HandleStaffRequest request, CancellationToken cancellationToken) =>
        Ok(await hr.HandleRequestAsync(Me, requestId, request.Action, request.Note, IsOwner, cancellationToken));

    // ---- the owner: pay, access, days off -----------------------------------------------------------------

    [HttpGet("staff")]
    [Authorize(Policy = PolicyNames.AdminAccess, Roles = "SuperAdmin")]
    public async Task<ActionResult<IReadOnlyList<StaffProfileDto>>> StaffAsync(CancellationToken cancellationToken) =>
        Ok(await hr.ListStaffAsync(cancellationToken));

    [HttpGet("staff/{userId:guid}")]
    [Authorize(Policy = PolicyNames.AdminAccess, Roles = "SuperAdmin")]
    public async Task<ActionResult<StaffProfileDto>> ProfileAsync(Guid userId, CancellationToken cancellationToken)
    {
        StaffProfileDto? profile = await hr.GetProfileAsync(userId, cancellationToken);
        return profile is null ? NotFound() : Ok(profile);
    }

    [HttpPut("staff/{userId:guid}")]
    [Authorize(Policy = PolicyNames.AdminAccess, Roles = "SuperAdmin")]
    public async Task<ActionResult<StaffProfileDto>> UpdateProfileAsync(
        Guid userId, UpdateStaffProfileCommand request, CancellationToken cancellationToken) =>
        Ok(await hr.UpdateProfileAsync(Me, userId, request, cancellationToken));

    [HttpPost("leaves/{leaveId:guid}/decision")]
    [Authorize(Policy = PolicyNames.AdminAccess, Roles = "SuperAdmin")]
    public async Task<ActionResult<LeaveRequestDto>> DecideLeaveAsync(
        Guid leaveId, DecideLeaveRequest request, CancellationToken cancellationToken) =>
        Ok(await hr.DecideLeaveAsync(Me, leaveId, request.Approve, request.Note, cancellationToken));

    [HttpGet("payroll/{userId:guid}")]
    [Authorize(Policy = PolicyNames.AdminAccess, Roles = "SuperAdmin")]
    public async Task<ActionResult<PayrollSheetDto>> PayrollAsync(
        Guid userId, [FromQuery] string? month, CancellationToken cancellationToken) =>
        Ok(await hr.GetPayrollAsync(userId, ParseMonth(month), cancellationToken));

    [HttpGet("payroll/{userId:guid}/history")]
    [Authorize(Policy = PolicyNames.AdminAccess, Roles = "SuperAdmin")]
    public async Task<ActionResult<PayrollHistoryDto>> PayrollHistoryAsync(
        Guid userId, [FromQuery] int months = 12, CancellationToken cancellationToken = default) =>
        Ok(await hr.GetPayrollHistoryAsync(userId, months, cancellationToken));

    [HttpPost("payroll/{userId:guid}/lines")]
    [Authorize(Policy = PolicyNames.AdminAccess, Roles = "SuperAdmin")]
    public async Task<ActionResult<PayrollSheetDto>> AddPayrollLineAsync(
        Guid userId, AddPayrollLineRequest request, CancellationToken cancellationToken) =>
        Ok(await hr.AddPayrollLineAsync(
            Me, userId, ParseMonth(request.Month), request.Kind, request.AmountInQepik, request.Note, cancellationToken));

    [HttpPost("payroll/lines/{lineId:guid}/void")]
    [Authorize(Policy = PolicyNames.AdminAccess, Roles = "SuperAdmin")]
    public async Task<ActionResult<PayrollSheetDto>> VoidPayrollLineAsync(Guid lineId, CancellationToken cancellationToken) =>
        Ok(await hr.VoidPayrollLineAsync(Me, lineId, cancellationToken));

    private static DateOnly ParseMonth(string? month)
    {
        if (!string.IsNullOrWhiteSpace(month) &&
            DateOnly.TryParseExact(month + "-01", "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateOnly parsed))
        {
            return parsed;
        }

        DateOnly today = HrTime.BakuDay(DateTimeOffset.UtcNow);
        return new DateOnly(today.Year, today.Month, 1);
    }
}
