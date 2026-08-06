using RentoX.Domain.Common;
using RentoX.Domain.Common.Exceptions;

namespace RentoX.Domain.Authentication;

public sealed class RefreshToken : Entity
{
    private RefreshToken()
    {
    }

    private RefreshToken(
        Guid id,
        Guid userId,
        string tokenHash,
        DateTimeOffset createdAtUtc,
        DateTimeOffset expiresAtUtc)
        : base(id)
    {
        UserId = userId;
        TokenHash = tokenHash;
        CreatedAtUtc = createdAtUtc;
        ExpiresAtUtc = expiresAtUtc;
    }

    public Guid UserId { get; private set; }

    public string TokenHash { get; private set; } = string.Empty;

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset ExpiresAtUtc { get; private set; }

    public DateTimeOffset? RevokedAtUtc { get; private set; }

    public Guid? ReplacedByTokenId { get; private set; }

    public bool IsActive =>
        RevokedAtUtc is null
        && DateTimeOffset.UtcNow < ExpiresAtUtc;

    public static RefreshToken Create(
        Guid userId,
        string tokenHash,
        DateTimeOffset createdAtUtc,
        TimeSpan lifetime)
    {
        if (userId == Guid.Empty)
        {
            throw new DomainException(
                "User ID cannot be empty.");
        }

        if (string.IsNullOrWhiteSpace(tokenHash))
        {
            throw new DomainException(
                "Refresh token hash is required.");
        }

        if (lifetime <= TimeSpan.Zero)
        {
            throw new DomainException(
                "Refresh token lifetime must be positive.");
        }

        return new RefreshToken(
            Guid.NewGuid(),
            userId,
            tokenHash,
            createdAtUtc,
            createdAtUtc.Add(lifetime));
    }

    public void Revoke(
        DateTimeOffset occurredAtUtc,
        Guid? replacedByTokenId = null)
    {
        if (RevokedAtUtc.HasValue)
        {
            return;
        }

        RevokedAtUtc = occurredAtUtc;
        ReplacedByTokenId = replacedByTokenId;
    }
} 