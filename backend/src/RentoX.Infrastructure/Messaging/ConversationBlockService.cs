using Microsoft.EntityFrameworkCore;
using RentoX.Application.Abstractions.Time;
using RentoX.Application.Messaging;
using RentoX.Domain.Messaging;
using RentoX.Infrastructure.Persistence;

namespace RentoX.Infrastructure.Messaging;

public sealed class ConversationBlockService(RentoXDbContext dbContext, IClock clock)
    : IConversationBlockService
{
    public async Task<ConversationBlockStatusResult?> GetStatusAsync(
        Guid userId, Guid conversationId, CancellationToken cancellationToken = default)
    {
        Guid? otherUserId = await FindOtherUserAsync(userId, conversationId, cancellationToken);
        if (!otherUserId.HasValue) { return null; }
        Guid otherId = otherUserId.Value;

        Guid[] blockers = await dbContext.Set<UserBlock>().AsNoTracking()
            .Where(block =>
                (block.BlockerId == userId && block.BlockedUserId == otherId) ||
                (block.BlockerId == otherId && block.BlockedUserId == userId))
            .Select(block => block.BlockerId)
            .ToArrayAsync(cancellationToken);

        return new ConversationBlockStatusResult(
            conversationId, blockers.Contains(userId), blockers.Length == 0);
    }

    public async Task<bool> BlockAsync(
        Guid userId, Guid conversationId, CancellationToken cancellationToken = default)
    {
        Guid? otherUserId = await FindOtherUserAsync(userId, conversationId, cancellationToken);
        if (!otherUserId.HasValue) { return false; }
        Guid otherId = otherUserId.Value;

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        await MessagingBlockGuard.AcquirePairLockAsync(dbContext, userId, otherId, cancellationToken);

        bool exists = await dbContext.Set<UserBlock>().AnyAsync(block =>
            block.BlockerId == userId && block.BlockedUserId == otherId, cancellationToken);
        if (!exists)
        {
            dbContext.Set<UserBlock>().Add(UserBlock.Create(userId, otherId, clock.UtcNow));
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        await transaction.CommitAsync(cancellationToken);
        return true;
    }

    public async Task<bool> UnblockAsync(
        Guid userId, Guid conversationId, CancellationToken cancellationToken = default)
    {
        Guid? otherUserId = await FindOtherUserAsync(userId, conversationId, cancellationToken);
        if (!otherUserId.HasValue) { return false; }
        Guid otherId = otherUserId.Value;

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        await MessagingBlockGuard.AcquirePairLockAsync(dbContext, userId, otherId, cancellationToken);

        // A caller can remove only the block they created.
        await dbContext.Set<UserBlock>()
            .Where(block => block.BlockerId == userId && block.BlockedUserId == otherId)
            .ExecuteDeleteAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return true;
    }

    private async Task<Guid?> FindOtherUserAsync(
        Guid userId, Guid conversationId, CancellationToken cancellationToken)
    {
        if (userId == Guid.Empty || conversationId == Guid.Empty) { return null; }

        // Membership lookup must keep working while either participant is blocked.
        return await dbContext.Conversations.AsNoTracking()
            .Where(conversation => conversation.Id == conversationId &&
                (conversation.BuyerId == userId || conversation.SellerId == userId))
            .Select(conversation => (Guid?)(conversation.BuyerId == userId
                ? conversation.SellerId : conversation.BuyerId))
            .SingleOrDefaultAsync(cancellationToken);
    }
}
