namespace RentoX.Contracts.Authentication;

public sealed record CompleteRegistrationRequest(
    Guid ChallengeId,
    string Code,
    string FullName,
    int PreferredLanguage);