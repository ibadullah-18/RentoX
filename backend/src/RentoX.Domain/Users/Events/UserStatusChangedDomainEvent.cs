using RentoX.Domain.Common.Events;
using RentoX.Domain.Users.Enums;

namespace RentoX.Domain.Users.Events;

public sealed record UserStatusChangedDomainEvent(
    Guid UserId,
    UserStatus Status) : DomainEvent;