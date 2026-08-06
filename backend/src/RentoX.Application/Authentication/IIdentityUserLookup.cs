namespace RentoX.Application.Authentication;

public interface IIdentityUserLookup
{
    Task<bool> PhoneExistsAsync(
        string phoneNumber,
        CancellationToken cancellationToken = default);
}