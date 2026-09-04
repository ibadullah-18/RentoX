using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using RentoX.Application.Wallets;
using RentoX.Contracts.Wallets;
using RentoX.Domain.Common.Exceptions;
using RentoX.Domain.Wallets.Enums;

namespace RentoX.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/wallet")]
public sealed class WalletController(
    IWalletService walletService,
    IHostEnvironment environment)
    : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(
        typeof(WalletBalanceResponse),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        StatusCodes.Status401Unauthorized)]
    public async Task<
        ActionResult<WalletBalanceResponse>>
        GetAsync(
            CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out Guid userId))
        {
            return Unauthorized();
        }

        WalletBalanceResult result =
            await walletService.GetAsync(
                userId,
                cancellationToken);

        return Ok(CreateBalanceResponse(result));
    }

    [HttpGet("transactions")]
    [ProducesResponseType(
        typeof(WalletTransactionPageResponse),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        StatusCodes.Status401Unauthorized)]
    public async Task<
        ActionResult<WalletTransactionPageResponse>>
        GetTransactionsAsync(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            CancellationToken cancellationToken = default)
    {
        if (!TryGetCurrentUserId(out Guid userId))
        {
            return Unauthorized();
        }

        WalletTransactionPageResult result =
            await walletService.GetTransactionsAsync(
                userId,
                page,
                pageSize,
                cancellationToken);

        return Ok(
            new WalletTransactionPageResponse(
                result.Items
                    .Select(CreateTransactionResponse)
                    .ToList(),
                result.TotalCount,
                result.Page,
                result.PageSize,
                result.TotalPages));
    }

    [HttpPost("demo-top-up")]
    [ProducesResponseType(
        typeof(WalletOperationResponse),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
        StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(
        StatusCodes.Status404NotFound)]
    public async Task<
        ActionResult<WalletOperationResponse>>
        DemoTopUpAsync(
            DemoWalletTopUpRequest request,
            CancellationToken cancellationToken)
    {
        if (!environment.IsDevelopment())
        {
            return NotFound();
        }

        if (!TryGetCurrentUserId(out Guid userId))
        {
            return Unauthorized();
        }

        if (request.Amount is < 0.01m or > 10_000m)
        {
            throw new DomainException(
                "Demo top-up amount must be between 0.01 and 10000 AZN.");
        }

        WalletOperationResult result =
            await walletService.CreditAsync(
                new CreditWalletCommand(
                    userId,
                    request.Amount,
                    WalletTransactionType.TopUp,
                    "Development demo balance top-up",
                    null,
                    request.IdempotencyKey),
                cancellationToken);

        return Ok(CreateOperationResponse(result));
    }

    private bool TryGetCurrentUserId(
        out Guid userId)
    {
        string? userIdValue =
            User.FindFirstValue(
                ClaimTypes.NameIdentifier);

        return Guid.TryParse(
            userIdValue,
            out userId);
    }

    private static WalletBalanceResponse
        CreateBalanceResponse(
            WalletBalanceResult result)
    {
        return new WalletBalanceResponse(
            result.WalletId,
            result.UserId,
            result.Balance,
            result.Currency);
    }

    private static WalletTransactionResponse
        CreateTransactionResponse(
            WalletTransactionResult result)
    {
        return new WalletTransactionResponse(
            result.Id,
            result.WalletId,
            result.Direction,
            result.Type,
            result.Amount,
            result.BalanceBefore,
            result.BalanceAfter,
            result.Reason,
            result.RelatedEntityId,
            result.IdempotencyKey,
            result.OccurredAtUtc);
    }

    private static WalletOperationResponse
        CreateOperationResponse(
            WalletOperationResult result)
    {
        return new WalletOperationResponse(
            CreateBalanceResponse(result.Wallet),
            CreateTransactionResponse(
                result.Transaction),
            result.WasAlreadyProcessed);
    }
}
