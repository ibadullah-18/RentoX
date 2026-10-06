using System.Buffers.Binary;
using System.Data;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;
using RentoX.Application.Authentication;
using RentoX.Domain.Authentication;
using RentoX.Domain.Common.Exceptions;
using RentoX.Infrastructure.Persistence;

namespace RentoX.Infrastructure.Authentication;

public sealed class OtpOperationScopeFactory(
    RentoXDbContext dbContext)
    : IOtpOperationScopeFactory
{
    public async Task<IOtpOperationScope> BeginForPhoneAsync(
        string phoneNumber,
        CancellationToken cancellationToken = default)
    {
        string normalizedPhone =
            PhoneNumber.Create(phoneNumber).Value;

        if (dbContext.Database.CurrentTransaction is not null)
        {
            throw new InvalidOperationException(
                "An OTP operation cannot start inside another transaction.");
        }

        byte[] hash = SHA256.HashData(
            Encoding.UTF8.GetBytes(
                "RentoX:OTP:" + normalizedPhone));

        long lockKey = BinaryPrimitives.ReadInt64BigEndian(hash);

        IDbContextTransaction transaction =
            await dbContext.Database.BeginTransactionAsync(
                IsolationLevel.ReadCommitted,
                cancellationToken);

        try
        {
            await dbContext.Database.ExecuteSqlRawAsync(
                "SET LOCAL lock_timeout = '5s';",
                cancellationToken);

            await dbContext.Database.ExecuteSqlInterpolatedAsync(
                $"SELECT pg_advisory_xact_lock({lockKey});",
                cancellationToken);

            return new OtpOperationScope(transaction);
        }
        catch (PostgresException exception)
            when (exception.SqlState == PostgresErrorCodes.LockNotAvailable)
        {
            await transaction.DisposeAsync();

            throw new DomainException(
                "Another authentication request is in progress. Please try again.");
        }
        catch
        {
            await transaction.DisposeAsync();
            throw;
        }
    }

    public async Task<IOtpOperationScope> BeginForChallengeAsync(
        Guid challengeId,
        CancellationToken cancellationToken = default)
    {
        if (challengeId == Guid.Empty)
        {
            throw new DomainException(
                "OTP challenge id is required.");
        }

        // Read only the immutable phone number before acquiring the lock.
        // The service loads the challenge state after acquiring the lock.
        string? phoneNumber =
            await dbContext.OtpChallenges
                .AsNoTracking()
                .Where(challenge => challenge.Id == challengeId)
                .Select(challenge => challenge.PhoneNumber)
                .SingleOrDefaultAsync(cancellationToken);

        if (phoneNumber is null)
        {
            throw new DomainException(
                "OTP challenge was not found.");
        }

        return await BeginForPhoneAsync(
            phoneNumber,
            cancellationToken);
    }

    private sealed class OtpOperationScope(
        IDbContextTransaction transaction)
        : IOtpOperationScope
    {
        public Task CommitAsync(
            CancellationToken cancellationToken = default)
        {
            return transaction.CommitAsync(cancellationToken);
        }

        public ValueTask DisposeAsync()
        {
            return transaction.DisposeAsync();
        }
    }
}
