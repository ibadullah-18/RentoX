using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RentoX.Application.Abstractions.Authentication;
using RentoX.Application.Stores;
using RentoX.Contracts.Stores;

namespace RentoX.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/stores")]
public sealed class StoresController(
    IStoreProfileService storeProfileService,
    IStoreSubmissionService storeSubmissionService,
    IStoreStatsService storeStatsService,
    ICurrentUserContext currentUserContext)
    : ControllerBase
{
    [HttpPost]
    [ProducesResponseType<StoreProfileResponse>(
        StatusCodes.Status201Created)]
    [ProducesResponseType(
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
        StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<StoreProfileResponse>>
        CreateAsync(
            CreateStoreRequest request,
            CancellationToken cancellationToken)
    {
        Guid ownerId = GetRequiredUserId();

        CreateStoreCommand command = new(
            ownerId,
            request.Name,
            request.Description,
            request.PhoneNumber,
            request.Email,
            request.Address,
            request.InstagramUrl,
            request.TiktokUrl,
            request.FacebookUrl,
            request.WebsiteUrl);

        StoreProfileResult result =
            await storeProfileService.CreateAsync(
                command,
                cancellationToken);

        StoreProfileResponse response =
            MapResponse(result);

        return Created(
            $"/api/stores/{result.Slug}",
            response);
    }

    [HttpPut("mine/info")]
    [ProducesResponseType<StoreProfileResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<StoreProfileResponse>> UpdateInfoAsync(
        UpdateStoreInfoRequest request,
        CancellationToken cancellationToken)
    {
        StoreProfileResult result = await storeProfileService.UpdateInfoAsync(
            GetRequiredUserId(), request.Name, request.Description, cancellationToken);

        return Ok(MapResponse(result));
    }

    [HttpPut("mine")]
    [ProducesResponseType<StoreProfileResponse>(
    StatusCodes.Status200OK)]
    [ProducesResponseType(
    StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
    StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<StoreProfileResponse>>
    UpdateAsync(
        UpdateStoreRequest request,
        CancellationToken cancellationToken)
    {
        Guid ownerId = GetRequiredUserId();

        UpdateStoreCommand command = new(
            ownerId,
            request.Name,
            request.Description,
            request.PhoneNumber,
            request.Email,
            request.Address,
            request.InstagramUrl,
            request.TiktokUrl,
            request.FacebookUrl,
            request.WebsiteUrl);

        StoreProfileResult result =
            await storeProfileService.UpdateAsync(
                command,
                cancellationToken);

        return Ok(MapResponse(result));
    }

    /// <summary>The contact side of the store (phone, social media, WhatsApp, hours): changeable at any time.</summary>
    [HttpPut("mine/contact")]
    [ProducesResponseType<StoreProfileResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<StoreProfileResponse>> UpdateContactAsync(
        UpdateStoreContactRequest request,
        CancellationToken cancellationToken)
    {
        StoreProfileResult result = await storeProfileService.UpdateContactAsync(
            new UpdateStoreContactCommand(
                GetRequiredUserId(),
                request.PhoneNumber,
                request.Email,
                request.Address,
                request.InstagramUrl,
                request.TiktokUrl,
                request.FacebookUrl,
                request.YoutubeUrl,
                request.WebsiteUrl,
                request.WhatsAppNumber,
                request.WhatsAppMessage,
                request.WorkingHours?
                    .Select(d => new StoreHoursDay(d.Day, d.OpenMinute, d.CloseMinute))
                    .ToArray()),
            cancellationToken);

        return Ok(MapResponse(result));
    }

    /// <summary>Followers, views of the store page and of its listings, and how often it was contacted.</summary>
    [HttpGet("mine/stats")]
    [ProducesResponseType<StoreStatsResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<StoreStatsResponse>> GetStatsAsync(CancellationToken cancellationToken)
    {
        StoreStatsResult? stats = await storeStatsService.GetMineAsync(GetRequiredUserId(), cancellationToken);
        if (stats is null)
        {
            return NotFound();
        }

        return Ok(new StoreStatsResponse(
            stats.Followers,
            stats.ActiveListings,
            stats.ListingViews,
            stats.ListingFavorites,
            stats.ProfileViews,
            stats.Calls,
            stats.WhatsApp,
            stats.Messages,
            stats.ProfileViewsLast7Days,
            stats.ProfileViewsLast30Days,
            stats.ContactsLast30Days,
            stats.Last30Days
                .Select(p => new StoreDayPointResponse(p.Day, p.Views, p.Calls, p.WhatsApp, p.Messages))
                .ToArray()));
    }

    [HttpGet("mine")]
    [ProducesResponseType<StoreProfileResponse>(
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(
        StatusCodes.Status404NotFound)]
    public async Task<ActionResult<StoreProfileResponse>>
        GetMineAsync(
            CancellationToken cancellationToken)
    {
        Guid ownerId = GetRequiredUserId();

        StoreProfileResult? result =
            await storeProfileService.GetMineAsync(
                ownerId,
                cancellationToken);

        return result is null
            ? NotFound()
            : Ok(MapResponse(result));
    }


    private static StoreProfileResponse MapResponse(
        StoreProfileResult result)
    {
        long imageVersion =
            (result.UpdatedAtUtc ??
             result.CreatedAtUtc)
                .ToUnixTimeMilliseconds();
        return new StoreProfileResponse(
            result.Id,
            result.OwnerId,
            result.Name,
            result.Slug,
            result.Description,
            result.PhoneNumber,
            result.Email,
            result.Address,
            result.LogoImageKey is null
                ? null
                : $"/api/stores/{result.Id}/logo?v={imageVersion}",
            result.CoverImageKey is null
                ? null
                : $"/api/stores/{result.Id}/cover?v={imageVersion}",
            result.InstagramUrl,
            result.TiktokUrl,
            result.FacebookUrl,
            result.WebsiteUrl,
            result.Status,
            result.RejectionReason,
            result.CreatedAtUtc,
            result.UpdatedAtUtc,
            result.YoutubeUrl,
            result.WhatsAppNumber,
            result.WhatsAppMessage,
            result.WorkingHours?
                .Select(d => new StoreHoursDayDto(d.Day, d.OpenMinute, d.CloseMinute))
                .ToArray());
    }

    [HttpPost("mine/submit")]
    [ProducesResponseType<StoreStatusResponse>(
    StatusCodes.Status200OK)]
    [ProducesResponseType(
    StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
    StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<StoreStatusResponse>>
    SubmitAsync(
        CancellationToken cancellationToken)
    {
        Guid ownerId = GetRequiredUserId();

        StoreStatusResult result =
            await storeSubmissionService.SubmitAsync(
                ownerId,
                cancellationToken);

        return Ok(new StoreStatusResponse(
            result.StoreId,
            result.Status,
            result.RejectionReason,
            result.UpdatedAtUtc));
    }

    private Guid GetRequiredUserId()
    {
        if (!currentUserContext.IsAuthenticated ||
            !currentUserContext.UserId.HasValue)
        {
            throw new UnauthorizedAccessException(
                "Authentication is required.");
        }

        return currentUserContext.UserId.Value;
    }
}