namespace RentoX.Application.Authentication;

public interface IRefreshTokenService
{
    Task<AuthTokenResult?> RefreshAsync(
        string refreshToken,
        CancellationToken cancellationToken = default);
}