using Microsoft.AspNetCore.SignalR;
using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Servitore.Shared.Models;

namespace Servitore.API.SignalR;

public class CollaborationHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        var httpContext = Context.GetHttpContext();
        var computerName = httpContext?.Request.Query["computerName"].ToString() ?? "Unknown";
        var appVersion = httpContext?.Request.Query["appVersion"].ToString() ?? "1.0.0";
        var ip = httpContext?.Connection?.RemoteIpAddress?.ToString() ?? "Unknown";
        
        var username = Context.User?.Identity?.Name ?? "Anonymous";
        var role = Context.User?.FindFirst(ClaimTypes.Role)?.Value ?? "Operator";

        var user = new ConnectedUserDto
        {
            ConnectionId = Context.ConnectionId,
            Username = username,
            Role = role,
            LoginTime = DateTime.UtcNow,
            LastActivity = DateTime.UtcNow,
            CurrentModule = "Dashboard",
            ComputerName = computerName,
            IpAddress = ip,
            Status = "Online",
            AppVersion = appVersion
        };

        PresenceManager.AddUser(Context.ConnectionId, user);
        
        // Broadcast presence list update
        await Clients.All.SendAsync("UserPresenceListUpdated", PresenceManager.GetConnectedUsers());
        
        // Broadcast a user logged in system notification
        await Clients.All.SendAsync("ReceiveNotification", new NotificationModel
        {
            Message = $"{username} logged in from {computerName}.",
            Type = Servitore.Shared.Enums.NotificationType.Info,
            CreatedBy = "System",
            CreatedDate = DateTime.UtcNow
        });

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var username = Context.User?.Identity?.Name ?? "Anonymous";
        
        PresenceManager.RemoveUser(Context.ConnectionId);
        
        // Clean up locks held by this connection
        RecordLockManager.ReleaseLocksForConnection(Context.ConnectionId);

        // Broadcast presence list update
        await Clients.All.SendAsync("UserPresenceListUpdated", PresenceManager.GetConnectedUsers());
        
        // Broadcast lock release updates
        await Clients.All.SendAsync("LocksUpdated");

        // Broadcast a user logged out system notification
        await Clients.All.SendAsync("ReceiveNotification", new NotificationModel
        {
            Message = $"{username} logged out.",
            Type = Servitore.Shared.Enums.NotificationType.Info,
            CreatedBy = "System",
            CreatedDate = DateTime.UtcNow
        });

        await base.OnDisconnectedAsync(exception);
    }

    public async Task UpdatePresence(string currentModule, string status)
    {
        PresenceManager.UpdateActivity(Context.ConnectionId, currentModule, status);
        await Clients.All.SendAsync("UserPresenceListUpdated", PresenceManager.GetConnectedUsers());
    }

    public async Task SendNotification(NotificationModel notification)
    {
        await Clients.All.SendAsync("ReceiveNotification", notification);
    }

    public async Task BroadcastDataChange(DataEventModel dataEvent)
    {
        await Clients.All.SendAsync("DataChanged", dataEvent);
    }

    public async Task ForceLogout(string connectionId)
    {
        if (Context.User?.IsInRole("Admin") == true || Context.User?.IsInRole("Manager") == true)
        {
            await Clients.Client(connectionId).SendAsync("OnForceLogout");
        }
    }

    public async Task SendBroadcast(string message)
    {
        if (Context.User?.IsInRole("Admin") == true || Context.User?.IsInRole("Manager") == true)
        {
            var senderName = Context.User?.Identity?.Name ?? "Admin";
            await Clients.All.SendAsync("ReceiveNotification", new NotificationModel
            {
                Message = $"[Broadcast] {message}",
                Type = Servitore.Shared.Enums.NotificationType.Info,
                CreatedBy = senderName,
                CreatedDate = DateTime.UtcNow
            });
        }
    }
}
