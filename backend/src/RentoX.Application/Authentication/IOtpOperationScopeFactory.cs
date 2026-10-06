namespace RentoX.Application.Authentication;

public interface IOtpOperationScopeFactory
{
    Task<IOtpOperationScope> BeginForPhoneAsync(
        string phoneNumber,
        CancellationToken cancellationToken = default);

    Task<IOtpOperationScope> BeginForChallengeAsync(
        Guid challengeId,
        CancellationToken cancellationToken = default);
}
