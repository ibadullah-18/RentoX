using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using RentoX.Application.Authorization;
using RentoX.Application.Support;

namespace RentoX.Api.Support;

/// <summary>
/// The live line for support and reports. The staff who answer support or review reports join a group for it;
/// the owner of a request is reached by their own id. Typing goes only between the owner and the support staff.
/// </summary>
[Authorize]
public sealed class SupportHub(ISupportTicketService tickets) : Hub
{
    public const string SupportGroup = "staff:support";
    public const string ReportsGroup = "staff:reports";
    public const string ListingsGroup = "staff:listings";
    public const string StoresGroup = "staff:stores";

    public override async Task OnConnectedAsync()
    {
        if (!Guid.TryParse(Context.UserIdentifier, out Guid userId) || userId == Guid.Empty)
        {
            Context.Abort();
            return;
        }

        if (Context.User is { } principal)
        {
            if (AdminSections.Allows(principal, AdminSections.Support))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, SupportGroup);
            }

            if (AdminSections.Allows(principal, AdminSections.Listings))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, ListingsGroup);
            }

            if (AdminSections.Allows(principal, AdminSections.Stores))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, StoresGroup);
            }

            if (AdminSections.Allows(principal, AdminSections.Reports))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, ReportsGroup);
            }
        }

        await base.OnConnectedAsync();
    }

    public Task StartTyping(Guid ticketId) => PublishTypingAsync(ticketId, isTyping: true);

    public Task StopTyping(Guid ticketId) => PublishTypingAsync(ticketId, isTyping: false);

    private async Task PublishTypingAsync(Guid ticketId, bool isTyping)
    {
        if (!Guid.TryParse(Context.UserIdentifier, out Guid userId) || userId == Guid.Empty)
        {
            throw new HubException("Authentication is required.");
        }

        Guid? ownerId = await tickets.GetOwnerIdAsync(ticketId, Context.ConnectionAborted);
        if (!ownerId.HasValue)
        {
            throw new HubException("Request was not found.");
        }

        bool isStaff = Context.User is { } principal && AdminSections.Allows(principal, AdminSections.Support);
        string method = isTyping ? "TypingStarted" : "TypingStopped";
        var payload = new SupportTypingEvent(ticketId, userId, isStaff);

        if (isStaff)
        {
            // The staff member writes: the owner of the request sees it (other staff do not need to).
            await Clients.User(ownerId.Value.ToString("D"))
                .SendAsync(method, payload, Context.ConnectionAborted);
        }
        else if (ownerId.Value == userId)
        {
            await Clients.Group(SupportGroup)
                .SendAsync(method, payload, Context.ConnectionAborted);
        }
        else
        {
            throw new HubException("Request was not found.");
        }
    }
}

public sealed record SupportTypingEvent(Guid TicketId, Guid UserId, bool IsStaff);
