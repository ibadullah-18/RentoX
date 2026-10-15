using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RentoX.Application.Admin;
using RentoX.Application.Authorization;
using RentoX.Application.Common;
using RentoX.Contracts.Common;

namespace RentoX.Api.Controllers.Admin;

/// <summary>
/// The admin's own reading of the catalogue and of all listings: unlike the
/// public lists these include inactive categories, fields and options, every
/// translation, and listings in every status.
/// </summary>
[ApiController]
[Authorize(Policy = PolicyNames.AdminAccess)]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class AdminReadController(
    IAdminCatalogReadService catalogReadService,
    IAdminListingSearchService listingSearchService,
    IAdminUserService userService,
    IAdminStoreSearchService storeSearchService,
    IUserModerationService userModerationService)
    : ControllerBase
{
    [HttpGet("api/admin/categories")]
    [Authorize(Policy = AdminSections.CatalogPolicy)]
    [ProducesResponseType<IReadOnlyList<AdminCategoryResult>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<AdminCategoryResult>>> GetCategoriesAsync(
        CancellationToken cancellationToken)
    {
        return Ok(await catalogReadService.GetCategoriesAsync(cancellationToken));
    }

    [HttpGet("api/admin/categories/{categoryId:guid}/fields")]
    [Authorize(Policy = AdminSections.CatalogPolicy)]
    [ProducesResponseType<IReadOnlyList<AdminFieldResult>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<AdminFieldResult>>> GetFieldsAsync(
        Guid categoryId,
        CancellationToken cancellationToken)
    {
        return Ok(await catalogReadService.GetFieldsAsync(categoryId, cancellationToken));
    }

    [HttpGet("api/admin/listings")]
    [Authorize(Policy = AdminSections.ListingsPolicy)]
    [ProducesResponseType<PagedResponse<AdminListingSearchItem>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResponse<AdminListingSearchItem>>> SearchListingsAsync(
        [FromQuery] int? status = null,
        [FromQuery] string? q = null,
        [FromQuery] Guid? categoryId = null,
        [FromQuery] Guid? ownerId = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        PagedResult<AdminListingSearchItem> result =
            await listingSearchService.SearchAsync(status, q, categoryId, ownerId, page, pageSize, cancellationToken);

        return Ok(new PagedResponse<AdminListingSearchItem>(
            result.Items,
            result.Page,
            result.PageSize,
            result.TotalCount,
            result.TotalPages));
    }

    [HttpGet("api/admin/users")]
    [Authorize(Policy = AdminSections.UsersPolicy)]
    [ProducesResponseType<PagedResponse<AdminUserItem>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResponse<AdminUserItem>>> SearchUsersAsync(
        [FromQuery] string? q = null,
        [FromQuery] int? status = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        PagedResult<AdminUserItem> result =
            await userService.SearchAsync(q, status, page, pageSize, cancellationToken);

        return Ok(new PagedResponse<AdminUserItem>(
            result.Items, result.Page, result.PageSize, result.TotalCount, result.TotalPages));
    }

    [HttpGet("api/admin/users/{userId:guid}")]
    [Authorize(Policy = AdminSections.UsersPolicy)]
    [ProducesResponseType<AdminUserDetails>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AdminUserDetails>> GetUserAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        AdminUserDetails? details = await userService.GetAsync(userId, cancellationToken);
        return details is null ? NotFound() : Ok(details);
    }

    [HttpPost("api/admin/users/{userId:guid}/block")]
    [Authorize(Policy = AdminSections.UsersPolicy)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> BlockUserAsync(
        Guid userId,
        BlockUserRequest request,
        CancellationToken cancellationToken)
    {
        await userModerationService.BlockAsync(userId, request.Reason, cancellationToken);
        return NoContent();
    }

    [HttpPost("api/admin/users/{userId:guid}/unblock")]
    [Authorize(Policy = AdminSections.UsersPolicy)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> UnblockUserAsync(Guid userId, CancellationToken cancellationToken)
    {
        await userModerationService.UnblockAsync(userId, cancellationToken);
        return NoContent();
    }

    [HttpGet("api/admin/stores")]
    [Authorize(Policy = AdminSections.StoresPolicy)]
    [ProducesResponseType<PagedResponse<AdminStoreItem>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResponse<AdminStoreItem>>> SearchStoresAsync(
        [FromQuery] int? status = null,
        [FromQuery] string? q = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        PagedResult<AdminStoreItem> result =
            await storeSearchService.SearchAsync(status, q, page, pageSize, cancellationToken);

        return Ok(new PagedResponse<AdminStoreItem>(
            result.Items, result.Page, result.PageSize, result.TotalCount, result.TotalPages));
    }
}

public sealed record BlockUserRequest(string Reason);
