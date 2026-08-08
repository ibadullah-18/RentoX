namespace RentoX.Contracts.Authentication;

public sealed record LogoutRequest(
    string RefreshToken);