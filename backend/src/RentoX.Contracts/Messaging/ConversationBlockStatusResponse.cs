namespace RentoX.Contracts.Messaging;

public sealed record ConversationBlockStatusResponse(
    Guid ConversationId,
    bool IsBlockedByMe,
    bool CanSendMessages);
