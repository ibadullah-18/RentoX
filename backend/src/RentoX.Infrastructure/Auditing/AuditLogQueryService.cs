using Microsoft.EntityFrameworkCore;
using RentoX.Application.Abstractions.Time;
using RentoX.Application.Auditing;
using RentoX.Domain.Auditing;
using RentoX.Domain.Common.Exceptions;
using RentoX.Infrastructure.Persistence;

namespace RentoX.Infrastructure.Auditing;

public sealed class AuditLogQueryService(
    RentoXDbContext dbContext,
    IClock clock)
    : IAuditLogQueryService
{
    public async Task<AuditLogPageResult> SearchAsync(
        AuditLogSearchQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        if (query.Page is < 1 or > 1000)
        {
            throw new DomainException(
                "Page must be between 1 and 1000. Narrow the filters for older records.");
        }

        if (query.PageSize is < 1 or > 100)
        {
            throw new DomainException(
                "Page size must be between 1 and 100.");
        }

        ValidateOptionalId(query.ActorUserId);
        ValidateOptionalId(query.TargetId);
        ValidateOptionalId(query.OperationId);

        if (query.Action.HasValue &&
            !Enum.IsDefined((AuditAction)query.Action.Value))
        {
            throw new DomainException(
                "Audit action is invalid.");
        }

        if (query.FromUtc.HasValue != query.ToUtc.HasValue)
        {
            throw new DomainException(
                "Provide both fromUtc and toUtc, or omit both.");
        }

        DateTimeOffset toUtc =
            (query.ToUtc ?? clock.UtcNow).ToUniversalTime();

        DateTimeOffset fromUtc =
            query.FromUtc.HasValue
                ? query.FromUtc.Value.ToUniversalTime()
                : toUtc.AddDays(-30);

        if (fromUtc >= toUtc)
        {
            throw new DomainException(
                "fromUtc must be earlier than toUtc.");
        }

        if (toUtc - fromUtc > TimeSpan.FromDays(90))
        {
            throw new DomainException(
                "The audit search interval cannot exceed 90 days.");
        }

        IQueryable<AuditLogEntry> entries =
            dbContext.Set<AuditLogEntry>()
                .AsNoTracking()
                .Where(entry =>
                    entry.OccurredAtUtc >= fromUtc &&
                    entry.OccurredAtUtc < toUtc);

        if (query.ActorUserId.HasValue)
        {
            Guid actorUserId = query.ActorUserId.Value;

            entries = entries.Where(entry =>
                entry.ActorUserId == actorUserId);
        }

        if (query.TargetId.HasValue)
        {
            Guid targetId = query.TargetId.Value;

            entries = entries.Where(entry =>
                entry.TargetId == targetId);
        }

        if (query.OperationId.HasValue)
        {
            Guid operationId = query.OperationId.Value;

            entries = entries.Where(entry =>
                entry.OperationId == operationId);
        }

        if (query.Action.HasValue)
        {
            AuditAction action = (AuditAction)query.Action.Value;

            entries = entries.Where(entry =>
                entry.Action == action);
        }

        int totalCount =
            await entries.CountAsync(cancellationToken);

        int offset = (query.Page - 1) * query.PageSize;

        AuditLogItemResult[] items =
            await entries
                .OrderByDescending(entry => entry.OccurredAtUtc)
                .ThenByDescending(entry => entry.Id)
                .Skip(offset)
                .Take(query.PageSize)
                .Select(entry => new AuditLogItemResult(
                    entry.Id,
                    entry.OperationId,
                    entry.ActorUserId,
                    entry.TargetId,
                    (int)entry.Action,
                    entry.PreviousValue,
                    entry.CurrentValue,
                    entry.RelatedEntityId,
                    entry.OccurredAtUtc))
                .ToArrayAsync(cancellationToken);

        int totalPages = totalCount == 0
            ? 0
            : (int)Math.Ceiling(totalCount / (double)query.PageSize);

        return new AuditLogPageResult(
            items,
            query.Page,
            query.PageSize,
            totalCount,
            totalPages,
            fromUtc,
            toUtc);
    }

    private static void ValidateOptionalId(Guid? value)
    {
        if (value.HasValue && value.Value == Guid.Empty)
        {
            throw new DomainException(
                "An audit filter id cannot be empty.");
        }
    }
}