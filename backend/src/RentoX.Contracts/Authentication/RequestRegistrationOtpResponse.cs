namespace RentoX.Contracts.Authentication;

public sealed record RequestRegistrationOtpResponse(
    Guid ChallengeId,
    DateTimeOffset ExpiresAtUtc,
    DateTimeOffset ResendAvailableAtUtc);