using RentoX.Domain.Common.Events;

namespace RentoX.Domain.Users.Events;

public sealed record UserProfileUpdatedDomainEvent(
    Guid UserId) : DomainEvent;