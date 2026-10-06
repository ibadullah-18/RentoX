using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RentoX.Application.Authorization;
using RentoX.Application.Common;
using RentoX.Application.Support;
using RentoX.Contracts.Common;
using RentoX.Contracts.Support;

namespace RentoX.Api.Controllers.Admin;

[ApiController]
[Authorize(Policy = PolicyNames.AdminAccess)]
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