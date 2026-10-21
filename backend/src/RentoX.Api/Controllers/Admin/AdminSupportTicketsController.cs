using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RentoX.Application.Authorization;
using RentoX.Application.Common;
using RentoX.Application.Support;
using RentoX.Contracts.Common;
using RentoX.Api.Controllers;
using RentoX.Contracts.Support;

namespace RentoX.Api.Controllers.Admin;

[ApiController]
[Authorize(Policy = PolicyNames.AdminAccess)]
[Authorize(Policy = AdminSections.SupportPolicy)]
[Route("api/admin/support/tickets")]
public sealed class AdminSupportTicketsController(
    ISupportTicketService supportTicketService)
    : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<
        PagedResponse<SupportTicketSummaryResponse>>(
            StatusCodes.Status200OK)]
    public async Task<ActionResult<
        PagedResponse<SupportTicketSummaryResponse>>> GetAsync(
            [FromQuery] int? status = null,
            [FromQuery] int? priority = null,
            [FromQuery] int? category = null,
            [FromQuery] bool open = false,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            CancellationToken cancellationToken = default)
    {
        PagedResult<SupportTicketSummaryResult> result =
            await supportTicketService.GetForAdminAsync(
                status,
                priority,
                category,
                page,
                pageSize,
                open,
                cancellationToken);

        return Ok(MapPage(result));
    }

    [HttpGet("{ticketId:guid}")]
    [ProducesResponseType<SupportTicketDetailsResponse>(
        StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SupportTicketDetailsResponse>>
        GetDetailsAsync(
            Guid ticketId,
            CancellationToken cancellationToken)
    {
        SupportTicketDetailsResult? result =
            await supportTicketService.GetAdminDetailsAsync(
                ticketId,
                cancellationToken);

        return result is null
            ? NotFound()
            : Ok(MapDetails(result));
    }

    [HttpPost("{ticketId:guid}/messages")]
    [ProducesResponseType<SupportTicketMessageResponse>(
        StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SupportTicketMessageResponse>>
        AddMessageAsync(
            Guid ticketId,
            AddSupportTicketMessageRequest request,
            CancellationToken cancellationToken)
    {
        if (!TryGetAdminId(out Guid adminUserId))
        {
            return Unauthorized();
        }

        SupportTicketMessageResult? result =
            await supportTicketService.AddAdminMessageAsync(
                adminUserId,
                ticketId,
                request.Body,
                cancellationToken);

        if (result is null)
        {
            return NotFound();
        }

        return Created(
            $"/api/admin/support/tickets/{ticketId:D}",
            MapMessage(result));
    }

    /// <summary>A message with photos (and, if wanted, words) from the staff.</summary>
    [Microsoft.AspNetCore.RateLimiting.EnableRateLimiting("image-upload")]
    [HttpPost("{ticketId:guid}/messages/images")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(55 * 1024 * 1024)]
    [RequestFormLimits(MultipartBodyLengthLimit = 55 * 1024 * 1024)]
    [ProducesResponseType<SupportTicketMessageResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SupportTicketMessageResponse>> AddImagesAsync(
        Guid ticketId,
        [FromForm] SupportImagesForm form,
        CancellationToken cancellationToken)
    {
        if (!TryGetAdminId(out Guid adminUserId))
        {
            return Unauthorized();
        }

        if (form.Files is null || form.Files.Count is < 1 or > 5)
        {
            return Problem(statusCode: 400, detail: "A message must contain 1 to 5 images.");
        }

        List<SupportImageUpload> uploads = [];
        try
        {
            foreach (IFormFile file in form.Files)
            {
                uploads.Add(new SupportImageUpload(file.OpenReadStream(), file.ContentType, file.Length));
            }

            SupportTicketMessageResult? result = await supportTicketService.AddAdminMessageWithImagesAsync(
                adminUserId, ticketId, form.Body ?? string.Empty, uploads, cancellationToken);
            return result is null
                ? NotFound()
                : Created($"/api/admin/support/tickets/{ticketId:D}", MapMessage(result));
        }
        finally
        {
            foreach (SupportImageUpload upload in uploads)
            {
                await upload.Content.DisposeAsync();
            }
        }
    }

    [HttpGet("{ticketId:guid}/images/{imageId:guid}")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> DownloadImageAsync(Guid ticketId, Guid imageId, CancellationToken cancellationToken)
    {
        SupportImageDownload? image = await supportTicketService.OpenImageAsync(ticketId, imageId, null, cancellationToken);
        if (image is null)
        {
            return NotFound();
        }

        Response.Headers["X-Content-Type-Options"] = "nosniff";
        return File(image.Content, image.ContentType);
    }

    [HttpPatch("{ticketId:guid}/status")]
    [ProducesResponseType<SupportTicketDetailsResponse>(
        StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SupportTicketDetailsResponse>>
        ChangeStatusAsync(
            Guid ticketId,
            ChangeSupportTicketStatusRequest request,
            CancellationToken cancellationToken)
    {
        if (!TryGetAdminId(out Guid adminUserId))
        {
            return Unauthorized();
        }

        SupportTicketDetailsResult? result =
            await supportTicketService.ChangeStatusAsync(
                adminUserId,
                ticketId,
                request.Status,
                cancellationToken);

        return result is null
            ? NotFound()
            : Ok(MapDetails(result));
    }

    [HttpPatch("{ticketId:guid}/priority")]
    [ProducesResponseType<SupportTicketDetailsResponse>(
        StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SupportTicketDetailsResponse>>
        ChangePriorityAsync(
            Guid ticketId,
            ChangeSupportTicketPriorityRequest request,
            CancellationToken cancellationToken)
    {
        if (!TryGetAdminId(out Guid adminUserId))
        {
            return Unauthorized();
        }

        SupportTicketDetailsResult? result =
            await supportTicketService.ChangePriorityAsync(
                adminUserId,
                ticketId,
                request.Priority,
                cancellationToken);

        return result is null
            ? NotFound()
            : Ok(MapDetails(result));
    }

    private bool TryGetAdminId(out Guid adminUserId)
    {
        return Guid.TryParse(
                   User.FindFirstValue(
                       ClaimTypes.NameIdentifier),
                   out adminUserId) &&
               adminUserId != Guid.Empty;
    }

    private static PagedResponse<SupportTicketSummaryResponse>
        MapPage(
            PagedResult<SupportTicketSummaryResult> result)
    {
        return new PagedResponse<SupportTicketSummaryResponse>(
            result.Items
                .Select(MapSummary)
                .ToArray(),
            result.Page,
            result.PageSize,
            result.TotalCount,
            result.TotalPages);
    }

    private static SupportTicketSummaryResponse MapSummary(
        SupportTicketSummaryResult result)
    {
        return new SupportTicketSummaryResponse(
            result.Id,
            result.UserId,
            result.Category,
            result.Subject,
            result.Priority,
            result.Status,
            result.CreatedAtUtc,
            result.UpdatedAtUtc,
            result.UserName,
            result.LastMessageAtUtc,
            result.LastMessageIsAdmin,
            result.LastMessagePreview,
            result.MessageCount);
    }

    private static SupportTicketDetailsResponse MapDetails(
        SupportTicketDetailsResult result)
    {
        return new SupportTicketDetailsResponse(
            result.Id,
            result.UserId,
            result.Category,
            result.Subject,
            result.Priority,
            result.Status,
            result.CreatedAtUtc,
            result.UpdatedAtUtc,
            result.ResolvedAtUtc,
            result.ClosedAtUtc,
            result.Messages
                .Select(MapMessage)
                .ToArray(),
            result.UserName,
            result.UserPhone,
            result.OtherTickets,
            result.Notifications?
                .Select(n => new SupportNotificationLogResponse(
                    n.Type, n.Title, n.CreatedAtUtc, n.ReadAtUtc, n.PushStatus,
                    n.Body, n.PushAttempts, n.PushSentAtUtc, n.PushError))
                .ToArray(),
            result.UserDevices);
    }

    private static SupportTicketMessageResponse MapMessage(
        SupportTicketMessageResult result)
    {
        return new SupportTicketMessageResponse(
            result.Id,
            result.SenderId,
            result.IsAdmin,
            result.Body,
            result.SentAtUtc,
            result.SenderName,
            MapImages(result));
    }

    private static SupportImageResponse[]? MapImages(SupportTicketMessageResult result) =>
        result.Images?
            .Select(image => new SupportImageResponse(image.Id, image.ContentType, image.SizeBytes, image.DisplayOrder))
            .ToArray();

}