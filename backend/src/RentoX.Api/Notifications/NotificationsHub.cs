using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace RentoX.Api.Notifications;

[Authorize]
public sealed class NotificationsHub : Hub
{
}
