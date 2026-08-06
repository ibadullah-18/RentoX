namespace RentoX.Application.Authentication;

public interface ITokenService
{
    Task<AuthTokenResult> CreateAsync(
        Guid userId,
        string phoneNumber,
        CancellationToken cancellationToken = default);
}