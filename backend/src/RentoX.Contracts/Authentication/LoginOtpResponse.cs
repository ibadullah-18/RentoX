namespace RentoX.Contracts.Authentication;

public sealed record LoginOtpResponse(
    Guid ChallengeId,
    DateTimeOffset ExpiresAtUtc,
    DateTimeOffset ResendAvailableAtUtc);