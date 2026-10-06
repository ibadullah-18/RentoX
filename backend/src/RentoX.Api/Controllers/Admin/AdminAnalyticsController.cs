using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RentoX.Application.Analytics;
using RentoX.Application.Authorization;
using RentoX.Contracts.Analytics;

namespace RentoX.Api.Controllers.Admin;

[ApiController]
[Authorize(Policy = PolicyNames.AdminAccess)]
[Route("api/admin/analytics")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class AdminAnalyticsController(
    IAdminAnalyticsService analyticsService)
    : ControllerBase
{
    [HttpGet("dashboard")]
    [ProducesResponseType<AdminDashboardResponse>(
        StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<AdminDashboardResponse>>
        GetDashboardAsync(CancellationToken cancellationToken)
    {
        AdminDashboardResult result =
            await analyticsService.GetDashboardAsync(
                cancellationToken);

        return Ok(new AdminDashboardResponse(
            result.GeneratedAtUtc,
            result.Last7DaysFromUtc,
            result.Last30DaysFromUtc,
            result.Activity.Select(item =>
                new AdminActivityResponse(
                    item.Metric,
                    item.Total,
                    item.Last7Days,
                    item.Last30Days)).ToArray(),
            result.StatusCounts.Select(item =>
                new AdminStatusCountResponse(
                    item.Module,
                    item.Status,
                    item.Count)).ToArray(),
            result.WalletBalances.Select(item =>
                new AdminWalletBalanceResponse(
                    item.Currency,
                    item.WalletCount,
                    item.TotalBalance)).ToArray(),
            result.WalletActivity.Select(item =>
                new AdminWalletActivityResponse(
                    item.Currency,
                    item.Direction,
                    item.Type,
                    item.TransactionCount,
                    item.TotalAmount,
                    item.Last7DaysAmount,
                    item.Last30DaysAmount)).ToArray(),
            result.MostViewedListings.Select(MapListing).ToArray(),
            result.MostFavoritedListings.Select(MapListing).ToArray()));
    }

    private static AdminTopListingResponse MapListing(
        AdminTopListingResult result)
    {
        return new AdminTopListingResponse(
            result.Id,
            result.Title,
            result.Status,
            result.ViewCount,
            result.FavoriteCount);
    }
}