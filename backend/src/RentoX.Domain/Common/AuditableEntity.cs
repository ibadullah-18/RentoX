namespace RentoX.Domain.Common;

public abstract class AuditableEntity : AggregateRoot
{
    protected AuditableEntity()
    {
    }

    protected AuditableEntity(Guid id)
        : base(id)
    {
    }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset? UpdatedAtUtc { get; private set; }

    public DateTimeOffset? DeletedAtUtc { get; private set; }

    public Guid? CreatedByUserId { get; private set; }

    public Guid? UpdatedByUserId { get; private set; }

    public bool IsDeleted => DeletedAtUtc.HasValue;

    public void MarkAsCreated(
        DateTimeOffset occurredAtUtc,
        Guid? userId)
    {
        CreatedAtUtc = occurredAtUtc;
        CreatedByUserId = userId;
    }

    public void MarkAsUpdated(
        DateTimeOffset occurredAtUtc,
        Guid? userId)
    {
        UpdatedAtUtc = occurredAtUtc;
        UpdatedByUserId = userId;
    }

    public void MarkAsDeleted(DateTimeOffset occurredAtUtc)
    {
        DeletedAtUtc = occurredAtUtc;
    }

    public void Restore()
    {
        DeletedAtUtc = null;
    }
}