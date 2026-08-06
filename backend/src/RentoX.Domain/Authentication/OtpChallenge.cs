using System.Security.Cryptography;
using System.Text;
using RentoX.Domain.Authentication.Enums;
using RentoX.Domain.Authentication.Events;
using RentoX.Domain.Common;
using RentoX.Domain.Common.Exceptions;

namespace RentoX.Domain.Authentication;

public sealed class OtpChallenge : AggregateRoot
{
    private OtpChallenge()
    {
    }

    private OtpChallenge(
        Guid id,
        string phoneNumber,
        string codeHash,
        OtpPurpose purpose,
        DateTimeOffset createdAtUtc,
        DateTimeOffset expiresAtUtc,
        int maximumAttempts)
        : base(id)
    {
        PhoneNumber = phoneNumber;
        CodeHash = codeHash;
        Purpose = purpose;
        CreatedAtUtc = createdAtUtc;
        ExpiresAtUtc = expiresAtUtc;
        MaximumAttempts = maximumAttempts;

        RaiseDomainEvent(new OtpRequestedDomainEvent(
            id,
            phoneNumber,
            purpose));
    }

    public string PhoneNumber { get; private set; } = string.Empty;

    public string CodeHash { get; private set; } = string.Empty;

    public OtpPurpose Purpose { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset ExpiresAtUtc { get; private set; }

    public DateTimeOffset? VerifiedAtUtc { get; private set; }

    public int FailedAttemptCount { get; private set; }

    public int MaximumAttempts { get; private set; }

    public static OtpChallenge Create(
        PhoneNumber phoneNumber,
        string codeHash,
        OtpPurpose purpose,
        DateTimeOffset createdAtUtc,
        TimeSpan lifetime,
        int maximumAttempts)
    {
        ArgumentNullException.ThrowIfNull(phoneNumber);

        if (string.IsNullOrWhiteSpace(codeHash))
        {
            throw new DomainException(
                "OTP code hash is required.");
        }

        if (lifetime <= TimeSpan.Zero)
        {
            throw new DomainException(
                "OTP lifetime must be positive.");
        }

        if (maximumAttempts <= 0)
        {
            throw new DomainException(
                "Maximum attempts must be positive.");
        }

        return new OtpChallenge(
            Guid.NewGuid(),
            phoneNumber.Value,
            codeHash,
            purpose,
            createdAtUtc,
            createdAtUtc.Add(lifetime),
            maximumAttempts);
    }

    public OtpVerificationResult Verify(
        string candidateHash,
        DateTimeOffset occurredAtUtc)
    {
        if (VerifiedAtUtc.HasValue)
        {
            return OtpVerificationResult.AlreadyUsed;
        }

        if (occurredAtUtc >= ExpiresAtUtc)
        {
            return OtpVerificationResult.Expired;
        }

        if (FailedAttemptCount >= MaximumAttempts)
        {
            return OtpVerificationResult.TooManyAttempts;
        }

        if (!HashesMatch(CodeHash, candidateHash))
        {
            FailedAttemptCount++;

            return FailedAttemptCount >= MaximumAttempts
                ? OtpVerificationResult.TooManyAttempts
                : OtpVerificationResult.InvalidCode;
        }

        VerifiedAtUtc = occurredAtUtc;

        RaiseDomainEvent(new OtpVerifiedDomainEvent(
            Id,
            PhoneNumber,
            Purpose));

        return OtpVerificationResult.Verified;
    }

    private static bool HashesMatch(
        string expectedHash,
        string candidateHash)
    {
        byte[] expectedBytes =
            Encoding.UTF8.GetBytes(expectedHash);

        byte[] candidateBytes =
            Encoding.UTF8.GetBytes(candidateHash);

        return CryptographicOperations.FixedTimeEquals(
            expectedBytes,
            candidateBytes);
    }
}