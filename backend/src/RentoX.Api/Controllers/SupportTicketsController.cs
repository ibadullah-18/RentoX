using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RentoX.Application.Common;
using RentoX.Application.Support;
using RentoX.Contracts.Common;
using RentoX.Contracts.Support;

namespace RentoX.Api.Controllers;

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
            result.SentAtUtc);
    }
}
