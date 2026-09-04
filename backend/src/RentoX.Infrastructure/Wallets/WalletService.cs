using Microsoft.EntityFrameworkCore;
using RentoX.Application.Abstractions.Time;
using RentoX.Application.Wallets;
using RentoX.Domain.Common.Exceptions;
using RentoX.Domain.Wallets;
using RentoX.Domain.Wallets.Enums;
using RentoX.Infrastructure.Persistence;

namespace RentoX.Infrastructure.Wallets;

public sealed class WalletService(
    RentoXDbContext dbContext,
    IClock clock)
    : IWalletService
{
    private const int MaximumPageSize = 100;

    public async Task<WalletBalanceResult> GetAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        ValidateUserId(userId);

        Wallet? wallet =
            await dbContext.Wallets
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    item => item.UserId == userId,
                    cancellationToken);

        if (wallet is not null)
        {
            return CreateBalanceResult(wallet);
        }

        await EnsureUserExistsAsync(
            userId,
            cancellationToken);

        wallet = Wallet.Create(userId);

        dbContext.Wallets.Add(wallet);

        await dbContext.SaveChangesAsync(
            cancellationToken);

        return CreateBalanceResult(wallet);
    }

    public async Task<WalletOperationResult> CreditAsync(
        CreditWalletCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        ValidateUserId(command.UserId);

        string idempotencyKey =
            NormalizeIdempotencyKey(
                command.IdempotencyKey);

        string reason =
            NormalizeReason(command.Reason);

        WalletOperationResult? existingResult =
            await FindExistingOperationAsync(
                command.UserId,
                WalletTransactionDirection.Credit,
                command.Type,
                command.Amount,
                reason,
                command.RelatedEntityId,
                idempotencyKey,
                cancellationToken);

        if (existingResult is not null)
        {
            return existingResult;
        }

        WalletLookup walletLookup =
            await GetOrCreateTrackedWalletAsync(
                command.UserId,
                cancellationToken);

        WalletTransaction transaction =
            walletLookup.Wallet.Credit(
                command.Amount,
                command.Type,
                reason,
                command.RelatedEntityId,
                idempotencyKey,
                clock.UtcNow);

        TrackWalletOperation(
            walletLookup,
            transaction);

        return await SaveOperationAsync(
            command.UserId,
            WalletTransactionDirection.Credit,
            command.Type,
            command.Amount,
            reason,
            command.RelatedEntityId,
            idempotencyKey,
            walletLookup.Wallet,
            transaction,
            cancellationToken);
    }

    public async Task<WalletOperationResult> DebitAsync(
        DebitWalletCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        ValidateUserId(command.UserId);

        string idempotencyKey =
            NormalizeIdempotencyKey(
                command.IdempotencyKey);

        string reason =
            NormalizeReason(command.Reason);

        WalletOperationResult? existingResult =
            await FindExistingOperationAsync(
                command.UserId,
                WalletTransactionDirection.Debit,
                command.Type,
                command.Amount,
                reason,
                command.RelatedEntityId,
                idempotencyKey,
                cancellationToken);

        if (existingResult is not null)
        {
            return existingResult;
        }

        WalletLookup walletLookup =
            await GetOrCreateTrackedWalletAsync(
                command.UserId,
                cancellationToken);

        WalletTransaction transaction =
            walletLookup.Wallet.Debit(
                command.Amount,
                command.Type,
                reason,
                command.RelatedEntityId,
                idempotencyKey,
                clock.UtcNow);

        TrackWalletOperation(
            walletLookup,
            transaction);

        return await SaveOperationAsync(
            command.UserId,
            WalletTransactionDirection.Debit,
            command.Type,
            command.Amount,
            reason,
            command.RelatedEntityId,
            idempotencyKey,
            walletLookup.Wallet,
            transaction,
            cancellationToken);
    }

    public async Task<WalletTransactionPageResult>
        GetTransactionsAsync(
            Guid userId,
            int page,
            int pageSize,
            CancellationToken cancellationToken = default)
    {
        ValidateUserId(userId);
        ValidatePagination(page, pageSize);

        Wallet? wallet =
            await dbContext.Wallets
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    item => item.UserId == userId,
                    cancellationToken);

        if (wallet is null)
        {
            WalletBalanceResult balance =
                await GetAsync(
                    userId,
                    cancellationToken);

            return new WalletTransactionPageResult(
                [],
                0,
                page,
                pageSize,
                0);
        }

        IQueryable<WalletTransaction> query =
            dbContext.WalletTransactions
                .AsNoTracking()
                .Where(transaction =>
                    transaction.WalletId == wallet.Id);

        long totalCount =
            await query.LongCountAsync(
                cancellationToken);

        List<WalletTransactionResult> items =
            await query
                .OrderByDescending(transaction =>
                    transaction.OccurredAtUtc)
                .ThenByDescending(transaction =>
                    transaction.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(transaction =>
                    new WalletTransactionResult(
                        transaction.Id,
                        transaction.WalletId,
                        (int)transaction.Direction,
                        (int)transaction.Type,
                        transaction.Amount,
                        transaction.BalanceBefore,
                        transaction.BalanceAfter,
                        transaction.Reason,
                        transaction.RelatedEntityId,
                        transaction.IdempotencyKey,
                        transaction.OccurredAtUtc))
                .ToListAsync(cancellationToken);

        int totalPages =
            totalCount == 0
                ? 0
                : (int)Math.Ceiling(
                    totalCount /
                    (double)pageSize);

        return new WalletTransactionPageResult(
            items,
            totalCount,
            page,
            pageSize,
            totalPages);
    }

    private async Task<WalletOperationResult>
        SaveOperationAsync(
            Guid userId,
            WalletTransactionDirection direction,
            WalletTransactionType type,
            decimal amount,
            string reason,
            Guid? relatedEntityId,
            string idempotencyKey,
            Wallet wallet,
            WalletTransaction transaction,
            CancellationToken cancellationToken)
    {
        try
        {
            await dbContext.SaveChangesAsync(
                cancellationToken);

            return CreateOperationResult(
                wallet,
                transaction,
                false);
        }
        catch (DbUpdateConcurrencyException)
        {
            dbContext.ChangeTracker.Clear();

            throw new DomainException(
                "Wallet balance changed. Please retry the operation.");
        }
        catch (DbUpdateException)
        {
            dbContext.ChangeTracker.Clear();

            WalletOperationResult? existingResult =
                await FindExistingOperationAsync(
                    userId,
                    direction,
                    type,
                    amount,
                    reason,
                    relatedEntityId,
                    idempotencyKey,
                    cancellationToken);

            if (existingResult is not null)
            {
                return existingResult;
            }

            throw;
        }
    }

    private async Task<WalletOperationResult?>
        FindExistingOperationAsync(
            Guid userId,
            WalletTransactionDirection direction,
            WalletTransactionType type,
            decimal amount,
            string reason,
            Guid? relatedEntityId,
            string idempotencyKey,
            CancellationToken cancellationToken)
    {
        WalletTransaction? transaction =
            await dbContext.WalletTransactions
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    item =>
                        item.IdempotencyKey ==
                        idempotencyKey,
                    cancellationToken);

        if (transaction is null)
        {
            return null;
        }

        Wallet wallet =
            await dbContext.Wallets
                .AsNoTracking()
                .SingleAsync(
                    item =>
                        item.Id ==
                        transaction.WalletId,
                    cancellationToken);

        bool sameOperation =
            wallet.UserId == userId &&
            transaction.Direction == direction &&
            transaction.Type == type &&
            transaction.Amount == amount &&
            transaction.Reason == reason &&
            transaction.RelatedEntityId ==
            relatedEntityId;

        if (!sameOperation)
        {
            throw new DomainException(
                "Idempotency key was already used for a different wallet operation.");
        }

        return CreateOperationResult(
            wallet,
            transaction,
            true);
    }

    private async Task<WalletLookup>
        GetOrCreateTrackedWalletAsync(
            Guid userId,
            CancellationToken cancellationToken)
    {
        Wallet? wallet =
            await dbContext.Wallets
                .SingleOrDefaultAsync(
                    item => item.UserId == userId,
                    cancellationToken);

        if (wallet is not null)
        {
            return new WalletLookup(
                wallet,
                false);
        }

        await EnsureUserExistsAsync(
            userId,
            cancellationToken);

        return new WalletLookup(
            Wallet.Create(userId),
            true);
    }

    private async Task EnsureUserExistsAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        bool userExists =
            await dbContext.Users
                .AsNoTracking()
                .AnyAsync(
                    user => user.Id == userId,
                    cancellationToken);

        if (!userExists)
        {
            throw new DomainException(
                "User account was not found.");
        }
    }

    private void TrackWalletOperation(
        WalletLookup walletLookup,
        WalletTransaction transaction)
    {
        if (walletLookup.IsNew)
        {
            dbContext.Wallets.Add(
                walletLookup.Wallet);

            return;
        }

        dbContext.WalletTransactions.Add(
            transaction);
    }

    private static WalletBalanceResult
        CreateBalanceResult(Wallet wallet)
    {
        return new WalletBalanceResult(
            wallet.Id,
            wallet.UserId,
            wallet.Balance,
            wallet.Currency);
    }

    private static WalletTransactionResult
        CreateTransactionResult(
            WalletTransaction transaction)
    {
        return new WalletTransactionResult(
            transaction.Id,
            transaction.WalletId,
            (int)transaction.Direction,
            (int)transaction.Type,
            transaction.Amount,
            transaction.BalanceBefore,
            transaction.BalanceAfter,
            transaction.Reason,
            transaction.RelatedEntityId,
            transaction.IdempotencyKey,
            transaction.OccurredAtUtc);
    }

    private static WalletOperationResult
        CreateOperationResult(
            Wallet wallet,
            WalletTransaction transaction,
            bool wasAlreadyProcessed)
    {
        return new WalletOperationResult(
            CreateBalanceResult(wallet),
            CreateTransactionResult(transaction),
            wasAlreadyProcessed);
    }

    private static void ValidateUserId(Guid userId)
    {
        if (userId == Guid.Empty)
        {
            throw new DomainException(
                "User id is required.");
        }
    }

    private static void ValidatePagination(
        int page,
        int pageSize)
    {
        if (page < 1)
        {
            throw new DomainException(
                "Page must be at least 1.");
        }

        if (pageSize is < 1 or > MaximumPageSize)
        {
            throw new DomainException(
                "Page size must be between 1 and 100.");
        }
    }

    private static string NormalizeReason(
        string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new DomainException(
                "Wallet transaction reason is required.");
        }

        return reason.Trim();
    }

    private static string NormalizeIdempotencyKey(
        string idempotencyKey)
    {
        if (string.IsNullOrWhiteSpace(
                idempotencyKey))
        {
            throw new DomainException(
                "Idempotency key is required.");
        }

        string normalized =
            idempotencyKey.Trim();

        if (normalized.Length >
            WalletTransaction
                .MaximumIdempotencyKeyLength)
        {
            throw new DomainException(
                "Idempotency key is too long.");
        }

        return normalized;
    }

    private sealed record WalletLookup(
        Wallet Wallet,
        bool IsNew);
}
