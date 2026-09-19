using Microsoft.EntityFrameworkCore;
using Npgsql;
using RentoX.Application.Abstractions.Time;
using RentoX.Application.Common;
using RentoX.Application.Messaging;
using RentoX.Domain.Common.Exceptions;
using RentoX.Domain.Listings.Enums;
using RentoX.Domain.Messaging;
using RentoX.Infrastructure.Persistence;

namespace RentoX.Infrastructure.Messaging;

public sealed class ConversationService(
    RentoXDbContext dbContext,
    IClock clock,
    IConversationEventPublisher eventPublisher)
    : IConversationService
{
    public async Task<StartConversationResult> StartAsync(
        Guid buyerId,
        Guid listingId,
        string body,
        CancellationToken cancellationToken = default)
    {
        RequireUser(buyerId);

        if (listingId == Guid.Empty)
        {
            throw new DomainException("Listing id is required.");
        }

        var listing = await dbContext.Listings
            .AsNoTracking()
            .Where(item => item.Id == listingId)
            .Select(item => new
            {
                item.OwnerId,
                item.Status,
                item.ExpiresAtUtc
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (listing is null ||
            listing.Status != ListingStatus.Active ||
            !listing.ExpiresAtUtc.HasValue ||
            listing.ExpiresAtUtc.Value <= clock.UtcNow)
        {
            throw new DomainException(
                "An active listing is required to start a conversation.");
        }

        if (listing.OwnerId == buyerId)
        {
            throw new DomainException(
                "You cannot message your own listing.");
        }

        Conversation? conversation =
            await dbContext.Conversations
                .SingleOrDefaultAsync(
                    item => item.ListingId == listingId &&
                            item.BuyerId == buyerId,
                    cancellationToken);

        bool created = conversation is null;

        if (conversation is null)
        {
            conversation = Conversation.Create(
                listingId,
                buyerId,
                listing.OwnerId,
                clock.UtcNow);

            dbContext.Conversations.Add(conversation);
        }

        Message message = Message.Create(
            conversation.Id,
            buyerId,
            body,
            clock.UtcNow);

        dbContext.Messages.Add(message);

        try
        {
            await dbContext.SaveChangesAsync(
                cancellationToken);
        }
        catch (DbUpdateException exception)
            when (created &&
                  exception.InnerException is PostgresException
                  {
                      SqlState: PostgresErrorCodes.UniqueViolation
                  })
        {
            // Eyni alıcı eyni elan üçün iki sorğunu eyni anda
            // göndəribsə, unikal söhbəti tapıb mesajı ona əlavə et.
            dbContext.ChangeTracker.Clear();

            conversation = await dbContext.Conversations
                .SingleOrDefaultAsync(
                    item => item.ListingId == listingId &&
                            item.BuyerId == buyerId,
                    cancellationToken);

            if (conversation is null)
            {
                throw;
            }

            created = false;
            message = Message.Create(
                conversation.Id,
                buyerId,
                body,
                clock.UtcNow);

            dbContext.Messages.Add(message);
            await dbContext.SaveChangesAsync(
                cancellationToken);
        }

        await eventPublisher.MessageCreatedAsync(
            conversation.BuyerId,
            conversation.SellerId,
            MapMessage(message),
            CancellationToken.None);
        return new StartConversationResult(
            conversation.Id,
            created,
            MapMessage(message));
    }

    public Task<int> GetUnreadCountAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        RequireUser(userId);

        return (
            from message in dbContext.Messages.AsNoTracking()
            join conversation in
                dbContext.Conversations.AsNoTracking()
                on message.ConversationId equals
                    conversation.Id
            where message.ReadAtUtc == null &&
                  message.SenderId != userId &&
                  (conversation.BuyerId == userId ||
                   conversation.SellerId == userId)
            select message.Id
        ).CountAsync(cancellationToken);
    }

    public async Task<PagedResult<ConversationSummaryResult>>
        GetMineAsync(
            Guid userId,
            int page,
            int pageSize,
            CancellationToken cancellationToken = default)
    {
        RequireUser(userId);
        ValidatePage(page, pageSize);

        var query = dbContext.Conversations
            .AsNoTracking()
            .Where(item =>
                item.BuyerId == userId ||
                item.SellerId == userId);

        int totalCount =
            await query.CountAsync(cancellationToken);

        var rows = await query
            .Select(item => new
            {
                item.Id,
                item.ListingId,
                ListingTitle = dbContext.Listings
                    .Where(listing =>
                        listing.Id == item.ListingId)
                    .Select(listing => listing.Title)
                    .FirstOrDefault(),
                OtherUserId =
                    item.BuyerId == userId
                        ? item.SellerId
                        : item.BuyerId,
                LastMessage = dbContext.Messages
                    .Where(message =>
                        message.ConversationId == item.Id)
                    .OrderByDescending(message =>
                        message.SentAtUtc)
                    .ThenByDescending(message =>
                        message.Id)
                    .Select(message => message.Body)
                    .FirstOrDefault(),
                LastMessageAtUtc = dbContext.Messages
                    .Where(message =>
                        message.ConversationId == item.Id)
                    .OrderByDescending(message =>
                        message.SentAtUtc)
                    .ThenByDescending(message =>
                        message.Id)
                    .Select(message =>
                        (DateTimeOffset?)message.SentAtUtc)
                    .FirstOrDefault(),
                UnreadCount = dbContext.Messages
                    .Count(message =>
                        message.ConversationId == item.Id &&
                        message.SenderId != userId &&
                        message.ReadAtUtc == null)
            })
            .OrderByDescending(item =>
                item.LastMessageAtUtc)
            .ThenByDescending(item => item.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        ConversationSummaryResult[] items = rows
            .Select(item =>
                new ConversationSummaryResult(
                    item.Id,
                    item.ListingId,
                    item.ListingTitle ?? string.Empty,
                    item.OtherUserId,
                    item.LastMessage,
                    item.LastMessageAtUtc,
                    item.UnreadCount))
            .ToArray();

        return new PagedResult<ConversationSummaryResult>(
            items,
            page,
            pageSize,
            totalCount,
            (int)Math.Ceiling(
                totalCount / (double)pageSize));
    }

    public async Task<PagedResult<MessageResult>?>
        GetMessagesAsync(
            Guid userId,
            Guid conversationId,
            int page,
            int pageSize,
            CancellationToken cancellationToken = default)
    {
        RequireUser(userId);
        ValidatePage(page, pageSize);

        if (!await CanAccessAsync(
                userId,
                conversationId,
                cancellationToken))
        {
            return null;
        }

        var query = dbContext.Messages
            .AsNoTracking()
            .Where(item =>
                item.ConversationId == conversationId);

        int totalCount =
            await query.CountAsync(cancellationToken);

        MessageResult[] items = await query
            .OrderByDescending(item => item.SentAtUtc)
            .ThenByDescending(item => item.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(item => new MessageResult(
                item.Id,
                item.ConversationId,
                item.SenderId,
                item.Body,
                item.SentAtUtc,
                item.ReadAtUtc))
            .ToArrayAsync(cancellationToken);

        return new PagedResult<MessageResult>(
            items,
            page,
            pageSize,
            totalCount,
            (int)Math.Ceiling(
                totalCount / (double)pageSize));
    }

    public async Task<MessageResult?> SendAsync(
        Guid userId,
        Guid conversationId,
        string body,
        CancellationToken cancellationToken = default)
    {
        RequireUser(userId);

        if (!await CanAccessAsync(
                userId,
                conversationId,
                cancellationToken))
        {
            return null;
        }

        Message message = Message.Create(
            conversationId,
            userId,
            body,
            clock.UtcNow);

        dbContext.Messages.Add(message);
        await dbContext.SaveChangesAsync(
            cancellationToken);

        Conversation participants =
            await dbContext.Conversations
                .AsNoTracking()
                .SingleAsync(
                    item => item.Id == conversationId,
                    cancellationToken);

        await eventPublisher.MessageCreatedAsync(
            participants.BuyerId,
            participants.SellerId,
            MapMessage(message),
            CancellationToken.None);
        return MapMessage(message);
    }

    public async Task<bool> MarkReadAsync(
        Guid userId,
        Guid conversationId,
        CancellationToken cancellationToken = default)
    {
        RequireUser(userId);

        if (!await CanAccessAsync(
                userId,
                conversationId,
                cancellationToken))
        {
            return false;
        }

        DateTimeOffset readAtUtc = clock.UtcNow;

        int updated = await dbContext.Messages
            .Where(item =>
                item.ConversationId == conversationId &&
                item.SenderId != userId &&
                item.ReadAtUtc == null)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(
                    item => item.ReadAtUtc,
                    (DateTimeOffset?)readAtUtc),
                cancellationToken);

        if (updated > 0)
        {
            Conversation participants =
                await dbContext.Conversations
                    .AsNoTracking()
                    .SingleAsync(
                        item => item.Id == conversationId,
                        cancellationToken);

            await eventPublisher.MessagesReadAsync(
                participants.BuyerId,
                participants.SellerId,
                conversationId,
                userId,
                readAtUtc,
                CancellationToken.None);
        }
        return true;
    }

    public async Task<Guid?> GetOtherParticipantIdAsync(
        Guid userId,
        Guid conversationId,
        CancellationToken cancellationToken = default)
    {
        RequireUser(userId);

        if (conversationId == Guid.Empty)
        {
            return null;
        }

        var participants = await dbContext.Conversations
            .AsNoTracking()
            .Where(item =>
                item.Id == conversationId &&
                (item.BuyerId == userId ||
                 item.SellerId == userId))
            .Select(item => new
            {
                item.BuyerId,
                item.SellerId
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (participants is null)
        {
            return null;
        }

        return participants.BuyerId == userId
            ? participants.SellerId
            : participants.BuyerId;
    }

    private Task<bool> CanAccessAsync(
        Guid userId,
        Guid conversationId,
        CancellationToken cancellationToken)
    {
        return dbContext.Conversations
            .AsNoTracking()
            .AnyAsync(
                item => item.Id == conversationId &&
                    (item.BuyerId == userId ||
                     item.SellerId == userId),
                cancellationToken);
    }

    private static MessageResult MapMessage(
        Message message)
    {
        return new MessageResult(
            message.Id,
            message.ConversationId,
            message.SenderId,
            message.Body,
            message.SentAtUtc,
            message.ReadAtUtc);
    }

    private static void RequireUser(Guid userId)
    {
        if (userId == Guid.Empty)
        {
            throw new DomainException(
                "User id is required.");
        }
    }

    private static void ValidatePage(
        int page,
        int pageSize)
    {
        if (page < 1 || page > 1_000_000 ||
            pageSize < 1 || pageSize > 100)
        {
            throw new DomainException(
                "Page must be 1..1000000 and pageSize 1..100.");
        }
    }
}