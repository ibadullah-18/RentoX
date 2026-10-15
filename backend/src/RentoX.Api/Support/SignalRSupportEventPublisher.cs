using Microsoft.AspNetCore.SignalR;
using RentoX.Application.Support;

namespace RentoX.Api.Support;

public sealed class SignalRSupportEventPublisher(
    IHubContext<SupportHub> hubContext,
    ILogger<SignalRSupportEventPublisher> logger)
    : ISupportEventPublisher
{
    private static readonly Action<ILogger, string, Exception?> DeliveryFailed =
        LoggerMessage.Define<string>(
            LogLevel.Warning,
            new EventId(1401, "SupportDeliveryFailed"),
            "Live support delivery failed ({Event})");

    public Task MessageAddedAsync(
        Guid ticketId, Guid ownerId, SupportTicketMessageResult message, CancellationToken cancellationToken = default) =>
        SendAsync(
            "SupportMessageCreated",
            new { TicketId = ticketId, Message = message },
            ownerId,
            SupportHub.SupportGroup,
            cancellationToken);

    public Task TicketCreatedAsync(
        Guid ticketId, Guid ownerId, string subject, string? userName, CancellationToken cancellationToken = default) =>
        SendAsync(
            "SupportTicketCreated",
            new { TicketId = ticketId, Subject = subject, UserName = userName },
            ownerId: null,
            SupportHub.SupportGroup,
            cancellationToken);

    public Task TicketChangedAsync(
        Guid ticketId, Guid ownerId, int status, int priority, CancellationToken cancellationToken = default) =>
        SendAsync(
            "SupportTicketChanged",
            new { TicketId = ticketId, Status = status, Priority = priority },
            ownerId,
            SupportHub.SupportGroup,
            cancellationToken);

    public Task ReportCreatedAsync(string kind, Guid reportId, CancellationToken cancellationToken = default) =>
        SendAsync(
            "ReportCreated",
            new { Kind = kind, ReportId = reportId },
            ownerId: null,
            SupportHub.ReportsGroup,
            cancellationToken);

    private async Task SendAsync(
        string method, object payload, Guid? ownerId, string group, CancellationToken cancellationToken)
    {
        try
        {
            await hubContext.Clients.Group(group).SendAsync(method, payload, cancellationToken);

            if (ownerId.HasValue)
            {
                await hubContext.Clients.User(ownerId.Value.ToString("D"))
                    .SendAsync(method, payload, cancellationToken);
            }
        }
        catch (Exception exception)
        {
            DeliveryFailed(logger, method, exception);
        }
    }
}
