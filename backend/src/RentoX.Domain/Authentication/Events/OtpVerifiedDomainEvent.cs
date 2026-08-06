using RentoX.Domain.Authentication.Enums;
using RentoX.Domain.Common.Events;

namespace RentoX.Domain.Authentication.Events;

public sealed record OtpVerifiedDomainEvent(
    Guid ChallengeId,
    string PhoneNumber,
    OtpPurpose Purpose) : DomainEvent;