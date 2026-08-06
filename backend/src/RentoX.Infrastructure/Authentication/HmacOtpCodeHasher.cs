using System.Security.Cryptography;
using System.Text;
using RentoX.Application.Authentication;

namespace RentoX.Infrastructure.Authentication;

public sealed class HmacOtpCodeHasher(
    OtpOptions options) : IOtpCodeHasher
{
    public string Hash(
        string phoneNumber,
        string code)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            phoneNumber);

        ArgumentException.ThrowIfNullOrWhiteSpace(code);

        if (options.HashingKey.Length < 32)
        {
            throw new InvalidOperationException(
                "OTP hashing key must contain at least 32 characters.");
        }

        byte[] key = Encoding.UTF8.GetBytes(
            options.HashingKey);

        byte[] content = Encoding.UTF8.GetBytes(
            $"{phoneNumber}:{code}");

        using HMACSHA256 hmac = new(key);

        return Convert.ToHexString(
            hmac.ComputeHash(content));
    }
}