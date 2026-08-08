namespace RentoX.Application.Authentication;

public interface ILoginAccountService
{
    Task<Guid?> FindUserIdAsync(
        string phoneNumber,
        CancellationToken cancellationToken = default);
}