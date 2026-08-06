using RentoX.Domain.Common.Exceptions;

namespace RentoX.Domain.Authentication;

public sealed record PhoneNumber
{
    private PhoneNumber(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public static PhoneNumber Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainException(
                "Phone number is required.");
        }

        string digits = string.Concat(
            value.Where(char.IsDigit));

        if (digits.StartsWith("994", StringComparison.Ordinal))
        {
            digits = digits[3..];
        }
        else if (digits.StartsWith('0'))
        {
            digits = digits[1..];
        }

        if (digits.Length != 9)
        {
            throw new DomainException(
                "Azerbaijan phone number must contain 9 digits.");
        }

        return new PhoneNumber($"+994{digits}");
    }

    public override string ToString()
    {
        return Value;
    }
}