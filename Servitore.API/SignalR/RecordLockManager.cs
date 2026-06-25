using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Servitore.Shared.Models;

namespace Servitore.API.SignalR;

public static class RecordLockManager
{
    private static readonly ConcurrentDictionary<string, RecordLockDto> _locks = new();

    public static RecordLockDto? AcquireLock(string recordKey, string username, string connectionId)
    {
        var computerName = "Unknown";
        var connectedUser = PresenceManager.GetConnectedUsers().FirstOrDefault(u => u.ConnectionId == connectionId);
        if (connectedUser != null)
        {
            computerName = connectedUser.ComputerName;
        }

        var newLock = new RecordLockDto
        {
            RecordKey = recordKey,
            Username = username,
            ConnectionId = connectionId,
            LockedAt = DateTime.UtcNow,
            ComputerName = computerName
        };

        return _locks.GetOrAdd(recordKey, newLock);
    }

    public static RecordLockDto? ReleaseLock(string recordKey, string username)
    {
        if (_locks.TryGetValue(recordKey, out var currentLock))
        {
            if (currentLock.Username == username)
            {
                if (_locks.TryRemove(recordKey, out var removedLock))
                {
                    return removedLock;
                }
            }
        }
        return null;
    }

    public static void ReleaseLocksForConnection(string connectionId)
    {
        var keysToRemove = _locks.Where(l => l.Value.ConnectionId == connectionId).Select(l => l.Key).ToList();
        foreach (var key in keysToRemove)
        {
            _locks.TryRemove(key, out _);
        }
    }

    public static RecordLockDto? ForceReleaseLock(string recordKey)
    {
        _locks.TryRemove(recordKey, out var removedLock);
        return removedLock;
    }

    public static RecordLockDto? GetLock(string recordKey)
    {
        return _locks.TryGetValue(recordKey, out var currentLock) ? currentLock : null;
    }

    public static void CleanStaleLocks(TimeSpan maxAge)
    {
        var now = DateTime.UtcNow;
        var keysToRemove = _locks.Where(l => now - l.Value.LockedAt > maxAge).Select(l => l.Key).ToList();
        foreach (var key in keysToRemove)
        {
            _locks.TryRemove(key, out _);
        }
    }
}
