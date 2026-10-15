using Microsoft.EntityFrameworkCore;
using RentoX.Application.Abstractions.Authentication;
using RentoX.Application.Abstractions.Time;
using RentoX.Application.Admin;
using RentoX.Application.Wallets;
using RentoX.Domain.Auditing;
using RentoX.Domain.Common.Exceptions;
using RentoX.Domain.Users;
using RentoX.Domain.Users.Enums;
using RentoX.Domain.Wallets.Enums;
using RentoX.Infrastructure.Persistence;

namespace RentoX.Infrastructure.Admin;

public sealed class WalletAdjustmentService(
    RentoXDbContext dbContext,
    IWalletService walletService,
    IClock clock,
    ICurrentUserContext currentUserContext)
    : IWalletAdjustmentService
{
    /// <summary>The most one hand-made adjustment may move.</summary>
    public const decimal MaximumAmount = 1000m;

    private const int MinimumReasonLength = 5;

    private const int MaximumReasonLength = 300;

    public async Task<WalletAdjustmentResult> AdjustAsync(
        Guid userId,
        Guid requestId,
        bool credit,
        int type,
        decimal amount,
        string reason,
        CancellationToken cancellationToken = default)
    {
        Guid actor = RequireActor();

        if (requestId == Guid.Empty)
        {
            throw new DomainException("A request id is required.");
        }

        if (userId == actor)
        {
            throw new DomainException("You cannot change your own wallet.");
        }

        if (amount <= 0 || amount > MaximumAmount || decimal.Round(amount, 2) != amount)
        {
            throw new DomainException(
                $"The amount must be between 0.01 and {MaximumAmount:0} with at most 2 decimal places.");
        }

        string text = (reason ?? string.Empty).Trim();
        if (text.Length < MinimumReasonLength || text.Length > MaximumReasonLength)
        {
            throw new DomainException(
                $"The reason must be {MinimumReasonLength} to {MaximumReasonLength} characters.");
        }

        WalletTransactionType transactionType = (WalletTransactionType)type;
        bool allowed = credit
            ? transactionType is WalletTransactionType.Bonus
                or WalletTransactionType.Refund
                or WalletTransactionType.AdminAdjustment
            : transactionType is WalletTransactionType.AdminAdjustment;
        if (!Enum.IsDefined(transactionType) || !allowed)
        {
            throw new DomainException("This kind of adjustment is not allowed.");
        }

        UserProfile profile = await dbContext.Set<UserProfile>().AsNoTracking()
            .SingleOrDefaultAsync(p => p.Id == userId, cancellationToken)
            ?? throw new DomainException("User was not found.");
        if (profile.Status == UserStatus.Deleted)
        {
            throw new DomainException("User was not found.");
        }

        // The same form sent twice: the first one already did it.
        if (await dbContext.Set<AuditLogEntry>().AnyAsync(
                a => a.OperationId == requestId && a.Action == AuditAction.WalletAdjusted && a.TargetId == userId,
                cancellationToken))
        {
            WalletBalanceResult current = await walletService.GetAsync(userId, cancellationToken);
            return new WalletAdjustmentResult(amount, current.Balance, true);
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        string key = $"admin-adjustment:{requestId:N}";
        WalletOperationResult done = credit
            ? await walletService.CreditAsync(
                new CreditWalletCommand(userId, amount, transactionType, text, null, key), cancellationToken)
            : await walletService.DebitAsync(
                new DebitWalletCommand(userId, amount, transactionType, text, null, key), cancellationToken);

        int qepik = (int)(amount * 100m);
        dbContext.Set<AuditLogEntry>().Add(AuditLogEntry.ForWalletAdjustment(
            requestId, actor, userId, credit ? qepik : -qepik, done.Transaction.Id, clock.UtcNow));
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return new WalletAdjustmentResult(amount, done.Wallet.Balance, false);
    }

    private Guid RequireActor()
    {
        Guid? id = currentUserContext.UserId;
        if (!currentUserContext.IsAuthenticated || !id.HasValue || id.Value == Guid.Empty)
        {
            throw new DomainException("An authenticated audit actor is required.");
        }

        return id.Value;
    }
}
