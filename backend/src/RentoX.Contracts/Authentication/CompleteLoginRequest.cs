namespace RentoX.Contracts.Authentication;

public sealed record CompleteLoginRequest(
    Guid ChallengeId,
    string Code);