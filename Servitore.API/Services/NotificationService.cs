using Microsoft.AspNetCore.SignalR;
using Servitore.API.SignalR;
using Servitore.Database.Context;
using Servitore.Database.Entities;
using Servitore.Shared.Enums;
using Servitore.Shared.Models;

namespace Servitore.API.Services;

public interface INotificationService
{
    Task BroadcastAsync(NotificationType type, string message, string createdBy);
}

// Persists the notification, then pushes it live to every connected desktop
// (Admin 1..7+) via SignalR so screens update instantly without polling.
public class NotificationService : INotificationService
{
    private readonly AppDbContext _context;
    private readonly IHubContext<CollaborationHub> _hubContext;

    public NotificationService(AppDbContext context, IHubContext<CollaborationHub> _hubContextVal)
    {
        _context = context;
        _hubContext = _hubContextVal;
    }

    public async Task BroadcastAsync(NotificationType type, string message, string createdBy)
    {
        var entity = new Notification
        {
            Message = message,
            Type = type,
            CreatedBy = createdBy,
            CreatedDate = DateTime.UtcNow
        };

        _context.Notifications.Add(entity);
        await _context.SaveChangesAsync();

        var model = new NotificationModel
        {
            NotificationId = entity.NotificationId,
            Message = entity.Message,
            Type = entity.Type,
            CreatedBy = entity.CreatedBy,
            CreatedDate = entity.CreatedDate
        };

        await _hubContext.Clients.All.SendAsync("ReceiveNotification", model);
    }
}
