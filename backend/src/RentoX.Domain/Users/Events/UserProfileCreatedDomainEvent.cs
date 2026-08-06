using RentoX.Domain.Common.Events;

namespace RentoX.Domain.Users.Events;

public sealed record UserProfileCreatedDomainEvent(
    Guid UserId) : DomainEvent;