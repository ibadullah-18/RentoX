using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using RentoX.Domain.Common.Exceptions;
using RentoX.Domain.Messaging;
using RentoX.Infrastructure.Persistence;

namespace RentoX.Infrastructure.Messaging;

internal static class MessagingBlockGuard
{
    public static Task<bool> IsBlockedAsync(
        RentoXDbContext dbContext, Guid firstUserId, Guid secondUserId,
        CancellationToken cancellationToken)
    {
        return dbContext.Set<UserBlock>().AsNoTracking().AnyAsync(block =>
            (block.BlockerId == firstUserId && block.BlockedUserId == secondUserId) ||
            (block.BlockerId == secondUserId && block.BlockedUserId == firstUserId),
            cancellationToken);
    }

    public static async Task EnsureAllowedAsync(
        RentoXDbContext dbContext, Guid firstUserId, Guid secondUserId,
        CancellationToken cancellationToken)
    {
        if (await IsBlockedAsync(dbContext, firstUserId, secondUserId, cancellationToken))
        {
            throw new DomainException("Messaging is unavailable between these users.");
        }
    }

    public static async Task AcquirePairLockAsync(
        RentoXDbContext dbContext, Guid firstUserId, Guid secondUserId,
        CancellationToken cancellationToken)
    {
        if (dbContext.Database.CurrentTransaction is null)
        {
            throw new InvalidOperationException("A transaction is required for the messaging lock.");
        }

        // Same key in either direction and across all conversations of this pair.
        bool firstComesBefore = firstUserId.CompareTo(secondUserId) < 0;
        Guid lower = firstComesBefore ? firstUserId : secondUserId;
        Guid upper = firstComesBefore ? secondUserId : firstUserId;
        byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(
            $"rentox:messaging-pair:v1:{lower:N}:{upper:N}"));
        long key = BinaryPrimitives.ReadInt64BigEndian(hash);

        // Parameterized SQL; released automatically at COMMIT or ROLLBACK.
        await dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT pg_advisory_xact_lock({key})", cancellationToken);
    }
}
