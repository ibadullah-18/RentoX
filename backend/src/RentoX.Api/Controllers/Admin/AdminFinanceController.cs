using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RentoX.Application.Abstractions.Authentication;
using RentoX.Application.Authorization;
using RentoX.Application.Finance;
using RentoX.Domain.Common.Exceptions;

namespace RentoX.Api.Controllers.Admin;

/// <summary>
/// Earnings, expenses and the monthly report. An Admin given the finance section can look; only the owner
/// (SuperAdmin) writes down expenses, voids them and changes the signer's details.
/// </summary>
[ApiController]
[Route("api/admin/finance")]
[Authorize(Policy = PolicyNames.AdminAccess)]
[Authorize(Policy = AdminSections.FinancePolicy)]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class AdminFinanceController(IFinanceService financeService, ICurrentUserContext currentUserContext) : ControllerBase
{
    public sealed record VoidRequest(string Reason);

    [HttpGet("summary")]
    public async Task<ActionResult<FinanceSummary>> SummaryAsync(CancellationToken cancellationToken) =>
        Ok(await financeService.GetSummaryAsync(cancellationToken));

    [HttpGet("report")]
    public async Task<ActionResult<MonthlyReport>> ReportAsync([FromQuery] string? month, CancellationToken cancellationToken) =>
        Ok(await financeService.GetMonthlyReportAsync(ParseMonth(month), cancellationToken));

    [HttpGet("expenses")]
    public async Task<ActionResult<IReadOnlyList<ExpenseDto>>> ExpensesAsync([FromQuery] string? month, CancellationToken cancellationToken) =>
        Ok(await financeService.GetExpensesAsync(ParseMonth(month), cancellationToken));

    [HttpGet("profile")]
    public async Task<ActionResult<FinanceProfileDto>> ProfileAsync(CancellationToken cancellationToken) =>
        Ok(await financeService.GetProfileAsync(cancellationToken));

    [HttpPost("expenses")]
    [Authorize(Policy = PolicyNames.AdminAccess, Roles = "SuperAdmin")]
    public async Task<ActionResult<ExpenseDto>> AddAsync(AddExpenseCommand request, CancellationToken cancellationToken) =>
        Ok(await financeService.AddExpenseAsync(ActorId(), request, cancellationToken));

    [HttpPost("expenses/{id:guid}/void")]
    [Authorize(Policy = PolicyNames.AdminAccess, Roles = "SuperAdmin")]
    public async Task<ActionResult<ExpenseDto>> VoidAsync(Guid id, VoidRequest request, CancellationToken cancellationToken) =>
        Ok(await financeService.VoidExpenseAsync(ActorId(), id, request.Reason, cancellationToken));

    [HttpPut("profile")]
    [Authorize(Policy = PolicyNames.AdminAccess, Roles = "SuperAdmin")]
    public async Task<ActionResult<FinanceProfileDto>> SetProfileAsync(FinanceProfileDto request, CancellationToken cancellationToken) =>
        Ok(await financeService.SetProfileAsync(ActorId(), request, cancellationToken));

    private Guid ActorId()
    {
        if (!currentUserContext.IsAuthenticated || !currentUserContext.UserId.HasValue)
        {
            throw new UnauthorizedAccessException("Authentication is required.");
        }

        return currentUserContext.UserId.Value;
    }

    /// <summary>"2026-10" (or nothing: this month in Baku).</summary>
    private static DateOnly ParseMonth(string? month)
    {
        if (!string.IsNullOrWhiteSpace(month)
            && DateOnly.TryParseExact(month.Trim() + "-01", "yyyy-MM-dd", out DateOnly parsed))
        {
            return parsed;
        }

        if (!string.IsNullOrWhiteSpace(month))
        {
            throw new DomainException("The month must look like 2026-10.");
        }

        DateTimeOffset now = DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(4));
        return new DateOnly(now.Year, now.Month, 1);
    }
}
