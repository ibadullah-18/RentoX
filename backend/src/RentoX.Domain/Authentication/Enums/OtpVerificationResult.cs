namespace RentoX.Domain.Authentication.Enums;

public enum OtpVerificationResult
{
    Verified = 1,
    InvalidCode = 2,
    Expired = 3,
    TooManyAttempts = 4,
    AlreadyUsed = 5
}