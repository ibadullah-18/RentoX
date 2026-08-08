namespace RentoX.Application.Authentication;

public sealed record LoginOtpResult(
    Guid ChallengeId,
    DateTimeOffset ExpiresAtUtc,
    DateTimeOffset ResendAvailableAtUtc);