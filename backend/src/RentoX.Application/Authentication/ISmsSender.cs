namespace RentoX.Application.Authentication;

public interface ISmsSender
{
    Task SendOtpAsync(
        string phoneNumber,
        string code,
        CancellationToken cancellationToken = default);
}