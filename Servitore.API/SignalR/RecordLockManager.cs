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
        var newLock = new RecordLockDto
        {
            RecordKey = recordKey,
            Username = username,
            ConnectionId = connectionId,
            LockedAt = DateTime.UtcNow
        };

        return _locks.GetOrAdd(recordKey, newLock);
    }

    public static bool ReleaseLock(string recordKey, string username)
    {
        if (_locks.TryGetValue(recordKey, out var currentLock))
        {
            if (currentLock.Username == username)
            {
                return _locks.TryRemove(recordKey, out _);
            }
        }
        return false;
    }

    public static void ReleaseLocksForConnection(string connectionId)
    {
        var keysToRemove = _locks.Where(l => l.Value.ConnectionId == connectionId).Select(l => l.Key).ToList();
        foreach (var key in keysToRemove)
        {
            _locks.TryRemove(key, out _);
        }
    }

    public static void ForceReleaseLock(string recordKey)
    {
        _locks.TryRemove(recordKey, out _);
    }

    public static RecordLockDto? GetLock(string recordKey)
    {
        return _locks.TryGetValue(recordKey, out var currentLock) ? currentLock : null;
    }
}
