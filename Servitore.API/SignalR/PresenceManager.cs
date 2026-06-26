using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Servitore.Shared.Models;

namespace Servitore.API.SignalR;

public static class PresenceManager
{
    private static readonly ConcurrentDictionary<string, ConnectedUserDto> _users = new();

    public static List<ConnectedUserDto> GetConnectedUsers() => _users.Values.ToList();

    public static void AddUser(string connectionId, ConnectedUserDto user)
    {
        _users[connectionId] = user;
    }

    public static void RemoveUser(string connectionId)
    {
        _users.TryRemove(connectionId, out _);
    }

    public static void UpdateActivity(string connectionId, string currentModule, string status)
    {
        if (_users.TryGetValue(connectionId, out var user))
        {
            user.CurrentModule = currentModule;
            user.Status = status;
            user.LastActivity = System.DateTime.UtcNow;
        }
    }

    public static void UpdateEditingRecord(string connectionId, string? recordKey)
    {
        if (_users.TryGetValue(connectionId, out var user))
        {
            user.EditingRecord = recordKey;
            user.LastActivity = System.DateTime.UtcNow;
        }
    }
}
