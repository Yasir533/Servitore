using Servitore.Shared.Enums;

namespace Servitore.Database.Entities;

public class Notification
{
    public int NotificationId { get; set; }
    public string Message { get; set; } = string.Empty;
    public NotificationType Type { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
}
