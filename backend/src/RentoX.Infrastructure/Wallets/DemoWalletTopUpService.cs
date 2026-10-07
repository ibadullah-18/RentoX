using Microsoft.Extensions.Hosting;
using RentoX.Application.Abstractions.Authentication;
using RentoX.Application.Wallets;
using RentoX.Domain.Common.Exceptions;
using RentoX.Domain.Wallets.Enums;

namespace RentoX.Infrastructure.Wallets;

public sealed class DemoWalletTopUpService(
    IWalletService walletService,
    IHostEnvironment environment,
    ICurrentUserContext currentUserContext)
    : IDemoWalletTopUpService
{
    public Task<WalletOperationResult> TopUpAsync(
        decimal amount,
        string idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!environment.IsDevelopment())
        {
            throw new InvalidOperationException(
                "Demo wallet top-up is disabled outside Development.");
        }

        Guid? userId = currentUserContext.UserId;

        if (!currentUserContext.IsAuthenticated ||
            !userId.HasValue ||
            userId.Value == Guid.Empty)
        {
            throw new DomainException(
                "Authentication is required.");
        }

        if (amount is < 0.01m or > 10_000m)
        {
            throw new DomainException(
                "Demo top-up amount must be between 0.01 and 10000 AZN.");
        }

        if (decimal.Round(amount, 2) != amount)
        {
            throw new DomainException(
                "Wallet amount cannot contain more than 2 decimal places.");
        }

        return walletService.CreditAsync(
            new CreditWalletCommand(
                userId.Value,
                amount,
                WalletTransactionType.TopUp,
                "Development demo balance top-up",
                null,
                idempotencyKey),
            cancellationToken);
    }
}
