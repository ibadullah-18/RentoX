namespace RentoX.Application.Authentication;

public sealed record RegistrationOtpResult(
    Guid ChallengeId,
    DateTimeOffset ExpiresAtUtc,
    DateTimeOffset ResendAvailableAtUtc);