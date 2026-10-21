using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RentoX.Application.Common;
using RentoX.Application.Support;
using RentoX.Contracts.Common;
using RentoX.Contracts.Support;

namespace RentoX.Api.Controllers;

public sealed class SupportImagesForm
{
    [System.ComponentModel.DataAnnotations.StringLength(4000)]
    public string? Body { get; set; }

    public List<IFormFile> Files { get; set; } = [];
}

[ApiController]
[Authorize]
[Route("api/support/tickets")]
public sealed class SupportTicketsController(
    ISupportTicketService supportTicketService)
    : ControllerBase
{
    [Microsoft.AspNetCore.RateLimiting.EnableRateLimiting("support-create")]
    [HttpPost]
    [ProducesResponseType<SupportTicketDetailsResponse>(
        StatusCodes.Status201Created)]
    public async Task<ActionResult<SupportTicketDetailsResponse>>
        CreateAsync(
            CreateSupportTicketRequest request,
            CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out Guid userId))
        {
            return Unauthorized();
        }

        SupportTicketDetailsResult result =
            await supportTicketService.CreateAsync(
                new CreateSupportTicketCommand(
                    userId,
                    request.Category,
                    request.Subject,
                    request.InitialMessage),
                cancellationToken);

        return Created(
            $"/api/support/tickets/{result.Id:D}",
            MapDetails(result));
    }

    [HttpGet]
    [ProducesResponseType<
        PagedResponse<SupportTicketSummaryResponse>>(
            StatusCodes.Status200OK)]
    public async Task<ActionResult<
        PagedResponse<SupportTicketSummaryResponse>>> GetMineAsync(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            CancellationToken cancellationToken = default)
    {
        if (!TryGetUserId(out Guid userId))
        {
            return Unauthorized();
        }

        PagedResult<SupportTicketSummaryResult> result =
            await supportTicketService.GetMineAsync(
                userId,
                page,
                pageSize,
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
        if (!TryGetUserId(out Guid userId))
        {
            return Unauthorized();
        }

        SupportTicketDetailsResult? result =
            await supportTicketService.GetMineDetailsAsync(
                userId,
                ticketId,
                cancellationToken);

        return result is null
            ? NotFound()
            : Ok(MapDetails(result));
    }

    [Microsoft.AspNetCore.RateLimiting.EnableRateLimiting("support-reply")]
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
        if (!TryGetUserId(out Guid userId))
        {
            return Unauthorized();
        }

        SupportTicketMessageResult? result =
            await supportTicketService.AddUserMessageAsync(
                userId,
                ticketId,
                request.Body,
                cancellationToken);

        if (result is null)
        {
            return NotFound();
        }

        return Created(
            $"/api/support/tickets/{ticketId:D}",
            MapMessage(result));
    }

    /// <summary>A message with photos (and, if wanted, words) from the person.</summary>
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
        if (!TryGetUserId(out Guid userId))
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

            SupportTicketMessageResult? result = await supportTicketService.AddUserMessageWithImagesAsync(
                userId, ticketId, form.Body ?? string.Empty, uploads, cancellationToken);
            return result is null
                ? NotFound()
                : Created($"/api/support/tickets/{ticketId:D}", MapMessage(result));
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
        if (!TryGetUserId(out Guid userId))
        {
            return Unauthorized();
        }

        SupportImageDownload? image = await supportTicketService.OpenImageAsync(ticketId, imageId, userId, cancellationToken);
        if (image is null)
        {
            return NotFound();
        }

        Response.Headers["X-Content-Type-Options"] = "nosniff";
        return File(image.Content, image.ContentType);
    }

    private bool TryGetUserId(out Guid userId)
    {
        return Guid.TryParse(
                   User.FindFirstValue(
                       ClaimTypes.NameIdentifier),
                   out userId) &&
               userId != Guid.Empty;
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
            result.UpdatedAtUtc);
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
                .ToArray());
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
            null,
            MapImages(result));
    }

    private static SupportImageResponse[]? MapImages(SupportTicketMessageResult result) =>
        result.Images?
            .Select(image => new SupportImageResponse(image.Id, image.ContentType, image.SizeBytes, image.DisplayOrder))
            .ToArray();

}
