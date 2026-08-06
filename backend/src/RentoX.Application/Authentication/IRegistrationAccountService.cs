using RentoX.Domain.Users.Enums;

namespace RentoX.Application.Authentication;

public interface IRegistrationAccountService
{
    Task<Guid> CreateAsync(
        string phoneNumber,
        string fullName,
        PreferredLanguage preferredLanguage,
        CancellationToken cancellationToken = default);
}