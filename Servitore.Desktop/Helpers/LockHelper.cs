using System;
using System.Threading.Tasks;
using Servitore.Shared.Models;

namespace Servitore.Desktop.Helpers;

public class LockResult
{
    public bool Success { get; set; }
    public RecordLockDto? Lock { get; set; }
}

public static class LockHelper
{
    public static async Task<LockResult> AcquireLockAsync(string recordKey)
    {
        try
        {
            var request = new { RecordKey = recordKey, ConnectionId = App.SignalRService.ConnectionId ?? string.Empty };
            var response = await App.ApiService.PostAsync<object, LockResult>("api/recordlock/acquire", request);
            return response ?? new LockResult { Success = false };
        }
        catch (Exception ex)
        {
            ClientLogger.Log($"Failed to acquire lock for {recordKey}", ex);
            return new LockResult { Success = true }; // Default to true on network error so editing is not blocked
        }
    }

    public static async Task ReleaseLockAsync(string recordKey)
    {
        try
        {
            var request = new { RecordKey = recordKey, ConnectionId = App.SignalRService.ConnectionId ?? string.Empty };
            await App.ApiService.PostAsync<object, object>("api/recordlock/release", request);
        }
        catch (Exception ex)
        {
            ClientLogger.Log($"Failed to release lock for {recordKey}", ex);
        }
    }

    public static async Task<LockResult> TakeOverLockAsync(string recordKey)
    {
        try
        {
            var request = new { RecordKey = recordKey, ConnectionId = App.SignalRService.ConnectionId ?? string.Empty };
            var response = await App.ApiService.PostAsync<object, LockResult>("api/recordlock/takeover", request);
            return response ?? new LockResult { Success = false };
        }
        catch (Exception ex)
        {
            ClientLogger.Log($"Failed to take over lock for {recordKey}", ex);
            return new LockResult { Success = false };
        }
    }
}
