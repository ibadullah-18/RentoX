namespace RentoX.Application.Support;

/// <summary>
/// Tells the people who are looking at the screen right now: the owner of a request and the staff who answer
/// support or review reports. Nothing here is stored; a failed delivery never fails the action itself.
/// </summary>
public interface ISupportEventPublisher
{
    /// <summary>A message was written: to the owner of the request and to the support staff.</summary>
    Task MessageAddedAsync(
        Guid ticketId,
        Guid ownerId,
        SupportTicketMessageResult message,
        CancellationToken cancellationToken = default);

    /// <summary>A new request was opened: to the support staff.</summary>
    Task TicketCreatedAsync(
        Guid ticketId,
        Guid ownerId,
        string subject,
        string? userName,
        CancellationToken cancellationToken = default);

    /// <summary>The status or the priority changed: to the owner and to the support staff.</summary>
    Task TicketChangedAsync(
        Guid ticketId,
        Guid ownerId,
        int status,
        int priority,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// A listing (kind "listing") or a store (kind "store") now waits for a review: to the staff who review them.
    /// </summary>
    Task ReviewWaitingAsync(
        string kind,
        Guid id,
        string title,
        CancellationToken cancellationToken = default);

    /// <summary>A new report (kind: "content" or "chat") came in: to the staff who review reports.</summary>
    Task ReportCreatedAsync(
        string kind,
        Guid reportId,
        CancellationToken cancellationToken = default);
}
