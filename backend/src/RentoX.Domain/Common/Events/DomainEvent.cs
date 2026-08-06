namespace RentoX.Domain.Common.Events;

public abstract record DomainEvent : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();

    public DateTimeOffset OccurredAtUtc { get; } =
        DateTimeOffset.UtcNow;
}