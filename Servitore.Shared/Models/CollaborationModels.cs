using System;

namespace Servitore.Shared.Models;

public class ConnectedUserDto
{
    public string ConnectionId { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public DateTime LoginTime { get; set; }
    public DateTime LastActivity { get; set; }
    public string CurrentModule { get; set; } = string.Empty;
    public string ComputerName { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
    public string Status { get; set; } = "Online"; // Online, Away, Offline
}

public class RecordLockDto
{
    public string RecordKey { get; set; } = string.Empty; // e.g. "Customer-12", "Asset-5"
    public string Username { get; set; } = string.Empty;
    public string ConnectionId { get; set; } = string.Empty;
    public DateTime LockedAt { get; set; }
}

public class DataEventModel
{
    public string EntityType { get; set; } = string.Empty; // e.g. "Customer", "Asset", "Ticket"
    public string Action { get; set; } = string.Empty; // "Created", "Updated", "Deleted"
    public string RecordId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty; // e.g. "Customer ABC"
    public string Username { get; set; } = string.Empty;
}
