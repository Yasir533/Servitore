using Microsoft.AspNetCore.SignalR;
using Servitore.Shared.Models;

namespace Servitore.API.SignalR;

// Every connected desktop client joins this hub. When any admin/engineer creates
// or updates a ticket, the API broadcasts here and every other open desktop
// updates its screen instantly, without polling.
public class NotificationHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        await Clients.Caller.SendAsync("Connected", Context.ConnectionId);
        await base.OnConnectedAsync();
    }

    // Called by NotificationService.BroadcastAsync(...) from the server side;
    // clients only listen for "ReceiveNotification" — they don't call this directly.
    public async Task SendNotification(NotificationModel notification)
    {
        await Clients.All.SendAsync("ReceiveNotification", notification);
    }
}
