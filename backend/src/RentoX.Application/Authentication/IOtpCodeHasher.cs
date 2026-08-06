namespace RentoX.Application.Authentication;

public interface IOtpCodeHasher
{
    string Hash(
        string phoneNumber,
        string code);
}