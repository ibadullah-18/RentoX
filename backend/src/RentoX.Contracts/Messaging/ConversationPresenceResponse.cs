namespace RentoX.Contracts.Messaging;

public sealed record ConversationPresenceResponse(
    Guid UserId,
    bool IsOnline);
