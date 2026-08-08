namespace RentoX.Application.Authentication;

public interface IAuthSessionService
{
    Task<bool> RevokeAsync(
        string refreshToken,
        CancellationToken cancellationToken = default);

    Task RevokeAllAsync(
        Guid userId,
        CancellationToken cancellationToken = default);
}