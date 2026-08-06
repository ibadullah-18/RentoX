using System.Globalization;
using System.Security.Cryptography;
using RentoX.Application.Authentication;

namespace RentoX.Infrastructure.Authentication;

public sealed class SecureOtpCodeGenerator(
    OtpOptions options) : IOtpCodeGenerator
{
    public string Generate()
    {
        if (options.CodeLength is < 4 or > 8)
        {
            throw new InvalidOperationException(
                "OTP code length must be between 4 and 8.");
        }

        int maximum = (int)Math.Pow(
            10,
            options.CodeLength);

        int code = RandomNumberGenerator.GetInt32(maximum);

        return code.ToString(
            $"D{options.CodeLength}",
            CultureInfo.InvariantCulture);
    }
}