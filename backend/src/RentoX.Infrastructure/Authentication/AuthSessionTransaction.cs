using System.Buffers.Binary;
using System.Data;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;
using RentoX.Domain.Common.Exceptions;
using RentoX.Infrastructure.Persistence;

namespace RentoX.Infrastructure.Authentication;

internal sealed class AuthSessionTransaction(
    IDbContextTransaction? ownedTransaction)
    : IAsyncDisposable
{
    public static async Task<AuthSessionTransaction> BeginAsync(
        RentoXDbContext dbContext,
        Guid userId,
        CancellationToken cancellationToken)
    {
        if (userId == Guid.Empty)
        {
            throw new DomainException("User ID cannot be empty.");
        }

        IDbContextTransaction? transaction = null;

        try
        {
            if (dbContext.Database.CurrentTransaction is null)
            {
                transaction =
                    await dbContext.Database.BeginTransactionAsync(
                        IsolationLevel.ReadCommitted,
                        cancellationToken);
            }

            string resource =
                "RentoX:Sessions:" +
                userId.ToString("N", CultureInfo.InvariantCulture);

            byte[] hash =
                SHA256.HashData(Encoding.UTF8.GetBytes(resource));

            long lockKey =
                BinaryPrimitives.ReadInt64BigEndian(hash);

            await dbContext.Database.ExecuteSqlRawAsync(
                "SET LOCAL lock_timeout = '5s';",
                cancellationToken);

            await dbContext.Database.ExecuteSqlInterpolatedAsync(
                $"SELECT pg_advisory_xact_lock({lockKey});",
                cancellationToken);

            return new AuthSessionTransaction(transaction);
        }
        catch (PostgresException exception)
            when (exception.SqlState == PostgresErrorCodes.LockNotAvailable)
        {
            if (transaction is not null)
            {
                await transaction.DisposeAsync();
            }

            throw new DomainException(
                "Another session operation is in progress. Please try again.");
        }
        catch
        {
            if (transaction is not null)
            {
                await transaction.DisposeAsync();
            }

            throw;
        }
    }

    public Task CommitAsync(CancellationToken cancellationToken)
    {
        return ownedTransaction is null
            ? Task.CompletedTask
            : ownedTransaction.CommitAsync(cancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        if (ownedTransaction is not null)
        {
            await ownedTransaction.DisposeAsync();
        }

        GC.SuppressFinalize(this);
    }
}
